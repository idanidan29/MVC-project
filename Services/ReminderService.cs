using MVC_project.Data;
using System.Linq;
using MVC_project.Models;
using Microsoft.Extensions.Logging;

namespace MVC_project.Services
{
    public class ReminderService
    {
        private readonly TripRepository _tripRepo;
        private readonly BookingRepository _bookingRepo;
        private readonly EmailService _emailService;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(TripRepository tripRepo, BookingRepository bookingRepo, EmailService emailService, ILogger<ReminderService> logger)
        {
            _tripRepo = tripRepo;
            _bookingRepo = bookingRepo;
            _emailService = emailService;
            _logger = logger;
        }

        public record ReminderResult(int SentCount, int SkippedCount);

        /// <summary>
        /// Sends reminder emails for trips whose reminder date is due. Optionally limits to a single trip.
        /// A reminder is due when: reminderDays is set, StartDate is in the future, and today is on/after StartDate - reminderDays.
        /// </summary>
        public async Task<ReminderResult> SendDueRemindersAsync(int? tripId = null, DateTime? todayOverride = null)
        {
            var today = (todayOverride ?? DateTime.UtcNow).Date;
            var sent = 0;
            var skipped = 0;

            var trips = tripId.HasValue
                ? new List<Trip?> { _tripRepo.GetById(tripId.Value) }
                : _tripRepo.GetReminderEnabledTrips().ToList();

            foreach (var trip in trips)
            {
                if (trip == null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    if (!trip.IsActive)
                    {
                        skipped++;
                        continue;
                    }

                    if (!trip.ReminderDaysBefore.HasValue)
                    {
                        skipped++;
                        continue;
                    }

                    if (trip.StartDate.Date < today)
                    {
                        skipped++;
                        continue;
                    }

                    var reminderDate = trip.StartDate.Date.AddDays(-trip.ReminderDaysBefore.Value);
                    if (reminderDate > today)
                    {
                        skipped++;
                        continue; // Not time yet
                    }

                    if (trip.LastReminderSentAt.HasValue && trip.LastReminderSentAt.Value.Date >= reminderDate)
                    {
                        skipped++;
                        continue; // Already sent for this window
                    }

                    var bookings = _bookingRepo.GetConfirmedByTripId(trip.TripID, today).ToList();
                    if (!bookings.Any())
                    {
                        skipped++;
                        continue; // Nothing to notify
                    }

                    var tripSends = 0;
                    foreach (var booking in bookings)
                    {
                        if (booking.User == null)
                        {
                            continue;
                        }

                        var ok = await _emailService.SendTripReminderAsync(
                            booking.User.email,
                            booking.User.first_name,
                            trip.Destination,
                            trip.StartDate,
                            trip.ReminderDaysBefore.Value);

                        if (ok)
                        {
                            tripSends++;
                        }
                    }

                    if (tripSends > 0)
                    {
                        sent += tripSends;
                        trip.LastReminderSentAt = DateTime.UtcNow;
                        _tripRepo.Update(trip);
                        _logger.LogInformation("Sent {Count} reminder(s) for trip {TripId} ({Destination})", tripSends, trip.TripID, trip.Destination);
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    _logger.LogError(ex, "Error while sending reminders for trip {TripId}", trip.TripID);
                }
            }

            return new ReminderResult(sent, skipped);
        }
    }
}
