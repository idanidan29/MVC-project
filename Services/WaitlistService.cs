using MVC_project.Data;
using Microsoft.Extensions.Logging;

namespace MVC_project.Services
{
    public class WaitlistService
    {
        private readonly WaitlistRepository _waitlistRepo;
        private readonly EmailService _emailService;
        private readonly ILogger<WaitlistService> _logger;

        public WaitlistService(WaitlistRepository waitlistRepo, EmailService emailService, ILogger<WaitlistService> logger)
        {
            _waitlistRepo = waitlistRepo;
            _emailService = emailService;
            _logger = logger;
        }

        // Process and send all pending waitlist notifications
        public async Task ProcessPendingNotificationsAsync()
        {
            _logger.LogInformation("Processing pending waitlist notifications...");

            var pendingNotifications = _waitlistRepo.GetPendingNotifications();

            if (!pendingNotifications.Any())
            {
                _logger.LogInformation("No pending notifications to process.");
                return;
            }

            _logger.LogInformation($"Found {pendingNotifications.Count} pending notifications.");

            foreach (var waitlist in pendingNotifications)
            {
                try
                {
                    // Send email
                    var success = await _emailService.SendWaitlistNotificationAsync(
                        waitlist.User!.email,
                        waitlist.User.first_name,
                        waitlist.Trip!.Destination
                    );

                    if (success)
                    {
                        // Mark email as sent
                        _waitlistRepo.MarkEmailSent(waitlist.WaitlistID);
                        _logger.LogInformation($"Notification sent to {waitlist.User.email} for trip to {waitlist.Trip.Destination}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to send notification to {waitlist.User.email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing notification for WaitlistID {waitlist.WaitlistID}");
                }
            }

            _logger.LogInformation("Finished processing waitlist notifications.");
        }
    }
}
