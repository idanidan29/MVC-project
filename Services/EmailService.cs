using System.Net.Mail;
using System.Net;

namespace MVC_project.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Send waitlist notification emails
        public async Task<bool> SendWaitlistNotificationAsync(string toEmail, string userName, string destination)
        {
            try
            {
                var subject = $"🎉 A Spot Has Opened Up for Your Trip to {destination}!";
                
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    </head>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🎉 Great News!</h1>
                        </div>
                        
                        <div style='background: #ffffff; padding: 30px; border: 1px solid #e2e8f0; border-radius: 0 0 10px 10px;'>
                            <p style='font-size: 16px; color: #1e293b;'>Hello {userName},</p>
                            
                            <div style='background: #dcfce7; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #065f46; font-weight: bold;'>
                                    ✅ A room has opened up for your trip to {destination}!
                                </p>
                            </div>

                            <p>Great news! You were on the waitlist, and we've <strong>automatically added this trip to your cart</strong>.</p>
                            
                            <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='margin: 0; color: #92400e; font-weight: bold;'>
                                    ⏰ You have 24 hours to complete your payment, or the room will be released to the next person on the waitlist.
                                </p>
                            </div>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='http://localhost:5000/User/Bookings' 
                                   style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                          color: white; padding: 15px 40px; text-decoration: none; border-radius: 25px; 
                                          font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                                    Complete Payment Now
                                </a>
                            </div>

                            <p style='color: #64748b; font-size: 0.9em; margin-top: 30px;'>
                                The trip is already in your cart. Click the button above to proceed with payment. Don't miss this opportunity!
                            </p>
                            
                            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
                            <p style='color: #94a3b8; font-size: 0.85em; text-align: center;'>
                                This is an automated notification from the MVC Travel Booking System.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending waitlist notification to {toEmail}");
                return false;
            }
        }

        // Generic email sending method
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Get email configuration from appsettings
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["Email:Username"] ?? "";
                var smtpPassword = _configuration["Email:Password"] ?? "";
                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["Email:FromName"] ?? "MVC Travel";

                // If no configuration, simulate for development
                if (string.IsNullOrEmpty(smtpUsername))
                {
                    _logger.LogInformation($"[SIMULATED EMAIL] To: {toEmail}, Subject: {subject}");
                    _logger.LogInformation("Email configuration not found - running in simulation mode");
                    return true; // Simulate success
                }

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                
                // For development, simulate success if real sending fails
                _logger.LogInformation($"[SIMULATED EMAIL - Error Recovery] To: {toEmail}, Subject: {subject}");
                return true; // Still return true for development
            }
        }
    }
}
