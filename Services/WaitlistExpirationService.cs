using MVC_project.Data;
using Microsoft.EntityFrameworkCore;

namespace MVC_project.Services
{
    /// <summary>
    /// Background service that periodically checks for expired waitlist entries
    /// and automatically removes them from cart, freeing up rooms for other users.
    /// Runs every 5 minutes to ensure timely processing.
    /// </summary>
    public class WaitlistExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WaitlistExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public WaitlistExpirationService(
            IServiceProvider serviceProvider,
            ILogger<WaitlistExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Waitlist Expiration Service started. Checking every {Interval} minutes.", _checkInterval.TotalMinutes);

            // Wait 1 minute before first check to allow application to fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredWaitlistEntries();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired waitlist entries");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Waitlist Expiration Service stopped.");
        }

        private async Task ProcessExpiredWaitlistEntries()
        {
            // Create a new scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var waitlistRepo = scope.ServiceProvider.GetRequiredService<WaitlistRepository>();
            var userTripRepo = scope.ServiceProvider.GetRequiredService<UserTripRepository>();
            var tripRepo = scope.ServiceProvider.GetRequiredService<TripRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Process expired waitlist entries
            await ProcessExpiredWaitlistEntriesInternal(waitlistRepo, userTripRepo, tripRepo, emailService);

            // Process expired cart items (regular cart, not waitlist)
            await ProcessExpiredCartItems(context, tripRepo, waitlistRepo, userTripRepo, emailService);
        }

        private async Task ProcessExpiredWaitlistEntriesInternal(
            WaitlistRepository waitlistRepo,
            UserTripRepository userTripRepo,
            TripRepository tripRepo,
            EmailService emailService)
        {
            // Get all expired entries
            var expiredEntries = waitlistRepo.GetExpiredEntries();

            if (expiredEntries.Count == 0)
            {
                _logger.LogDebug("No expired waitlist entries found.");
                return;
            }

            _logger.LogInformation("Found {Count} expired waitlist entries to process.", expiredEntries.Count);

            foreach (var entry in expiredEntries)
            {
                try
                {
                    _logger.LogInformation(
                        "Processing expired entry: User {UserId} ({UserName}) for trip {TripId} ({Destination}). Expired at {ExpiresAt}",
                        entry.UserId,
                        entry.User?.first_name ?? "Unknown",
                        entry.TripId,
                        entry.Trip?.Destination ?? "Unknown",
                        entry.ExpiresAt
                    );

                    // Find and remove the trip from user's cart
                    var cartItem = userTripRepo.GetByUserIdAndTripId(entry.UserId, entry.TripId);
                    if (cartItem != null)
                    {
                        var removedItem = userTripRepo.RemoveByUserTripId(cartItem.UserTripID);
                        
                        if (removedItem != null && removedItem.Trip != null)
                        {
                            // Restore available rooms
                            removedItem.Trip.AvailableRooms += removedItem.Quantity;
                            tripRepo.Update(removedItem.Trip);

                            _logger.LogInformation(
                                "Removed {Quantity} room(s) from user {UserId}'s cart and restored to {Destination}. Available rooms now: {AvailableRooms}",
                                removedItem.Quantity,
                                entry.UserId,
                                removedItem.Trip.Destination,
                                removedItem.Trip.AvailableRooms
                            );

                            // Process waitlist for the freed rooms
                            await ProcessWaitlistForTrip(
                                entry.TripId, 
                                removedItem.Quantity, 
                                waitlistRepo, 
                                userTripRepo, 
                                tripRepo, 
                                emailService
                            );
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Cart item not found for expired waitlist entry. User {UserId} may have already removed or paid for trip {TripId}.",
                            entry.UserId,
                            entry.TripId
                        );
                    }

                    // Mark waitlist entry as expired
                    waitlistRepo.UpdateStatus(entry.WaitlistID, "Expired");
                    _logger.LogInformation("Marked waitlist entry {WaitlistID} as Expired.", entry.WaitlistID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing expired entry for User {UserId}, Trip {TripId}",
                        entry.UserId,
                        entry.TripId
                    );
                }
            }

            _logger.LogInformation("Completed processing {Count} expired waitlist entries.", expiredEntries.Count);
        }

        /// <summary>
        /// Process waitlist when rooms become available, adding trips to waiting users' carts
        /// </summary>
        private async Task ProcessWaitlistForTrip(
            int tripId, 
            int roomsFreed, 
            WaitlistRepository waitlistRepo,
            UserTripRepository userTripRepo,
            TripRepository tripRepo,
            EmailService emailService)
        {
            var trip = tripRepo.GetById(tripId);
            if (trip == null)
            {
                _logger.LogWarning("Trip {TripId} not found when processing waitlist.", tripId);
                return;
            }

            int usersNotified = 0;

            // Process waitlist users one by one until no more rooms or no more waiting users
            while (roomsFreed > 0 && trip.AvailableRooms > 0)
            {
                var nextWaitlistUser = waitlistRepo.GetNextWaitingUser(tripId);
                if (nextWaitlistUser == null)
                {
                    _logger.LogDebug("No more users waiting for trip {TripId}.", tripId);
                    break;
                }

                try
                {
                    // Add trip to their cart (1 room per waitlist user)
                    userTripRepo.Add(nextWaitlistUser.UserId, tripId, 1);

                    // Decrease available rooms
                    trip.AvailableRooms -= 1;
                    tripRepo.Update(trip);

                    // Mark as notified and set expiration time
                    waitlistRepo.MarkEmailSent(nextWaitlistUser.WaitlistID);

                    // Send email notification
                    if (nextWaitlistUser.User != null)
                    {
                        await emailService.SendWaitlistNotificationAsync(
                            nextWaitlistUser.User.email,
                            nextWaitlistUser.User.first_name,
                            trip.Destination
                        );

                        _logger.LogInformation(
                            "Notified user {UserId} ({UserEmail}) about available room for {Destination}",
                            nextWaitlistUser.UserId,
                            nextWaitlistUser.User.email,
                            trip.Destination
                        );
                    }

                    roomsFreed--;
                    usersNotified++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error notifying waitlist user {UserId} for trip {TripId}",
                        nextWaitlistUser.UserId,
                        tripId
                    );
                }
            }

            if (usersNotified > 0)
            {
                _logger.LogInformation(
                    "Successfully notified {Count} user(s) from waitlist for {Destination}",
                    usersNotified,
                    trip.Destination
                );
            }
        }

        /// <summary>
        /// Process expired cart items that are not from waitlist
        /// </summary>
        private async Task ProcessExpiredCartItems(
            AppDbContext context,
            TripRepository tripRepo,
            WaitlistRepository waitlistRepo,
            UserTripRepository userTripRepo,
            EmailService emailService)
        {
            var now = DateTime.Now;
            var expiredCartItems = context.UserTrips
                .Include(ut => ut.Trip)
                .Include(ut => ut.User)
                .Where(ut => ut.ExpiresAt != null && ut.ExpiresAt < now)
                .ToList();

            if (expiredCartItems.Count == 0)
            {
                _logger.LogDebug("No expired cart items found.");
                return;
            }

            _logger.LogInformation("Found {Count} expired cart items to process.", expiredCartItems.Count);

            foreach (var cartItem in expiredCartItems)
            {
                try
                {
                    _logger.LogInformation(
                        "Processing expired cart item: User {UserId} ({UserName}) for trip {TripId} ({Destination}). Expired at {ExpiresAt}",
                        cartItem.UserId,
                        cartItem.User?.first_name ?? "Unknown",
                        cartItem.TripID,
                        cartItem.Trip?.Destination ?? "Unknown",
                        cartItem.ExpiresAt
                    );

                    // Remove from cart
                    var removedItem = userTripRepo.RemoveByUserTripId(cartItem.UserTripID);

                    if (removedItem != null && removedItem.Trip != null)
                    {
                        // Restore available rooms
                        removedItem.Trip.AvailableRooms += removedItem.Quantity;
                        tripRepo.Update(removedItem.Trip);

                        _logger.LogInformation(
                            "Removed {Quantity} room(s) from user {UserId}'s cart and restored to {Destination}. Available rooms now: {AvailableRooms}",
                            removedItem.Quantity,
                            cartItem.UserId,
                            removedItem.Trip.Destination,
                            removedItem.Trip.AvailableRooms
                        );

                        // Process waitlist for the freed rooms
                        await ProcessWaitlistForTrip(
                            cartItem.TripID,
                            removedItem.Quantity,
                            waitlistRepo,
                            userTripRepo,
                            tripRepo,
                            emailService
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing expired cart item for User {UserId}, Trip {TripId}",
                        cartItem.UserId,
                        cartItem.TripID
                    );
                }
            }

            _logger.LogInformation("Completed processing {Count} expired cart items.", expiredCartItems.Count);
        }
    }
}
