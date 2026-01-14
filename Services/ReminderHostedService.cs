using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MVC_project.Services
{
    /// <summary>
    /// Background service that checks for due trip reminders on a fixed interval.
    /// </summary>
    public class ReminderHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public ReminderHostedService(IServiceProvider serviceProvider, ILogger<ReminderHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder hosted service started. Checking every {Minutes} minutes.", _interval.TotalMinutes);

            // Small delay to allow app to warm up
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var reminderService = scope.ServiceProvider.GetRequiredService<ReminderService>();
                    await reminderService.SendDueRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running reminder check.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Reminder hosted service stopped.");
        }
    }
}
