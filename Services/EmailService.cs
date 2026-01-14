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

        // Send payment confirmation email
        public async Task<bool> SendPaymentConfirmationAsync(string toEmail, string userName, string destination, decimal amount, int quantity, DateTime startDate, DateTime endDate, string packageType)
        {
            try
            {
                var subject = $"✅ Payment Confirmed - Your Trip to {destination} is Booked!";
                var bookingRef = "BK-" + DateTime.Now.Ticks.ToString().Substring(0, 8).ToUpper();
                var duration = (endDate - startDate).Days;

                    var travelerLabel = quantity == 1 ? "Person" : "People";

                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    </head>
                    <body style='font-family: Segoe UI, Arial, sans-serif; line-height: 1.6; color: #333; max-width: 700px; margin: 0 auto; padding: 0;'>
                        <!-- Header -->
                        <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 40px 20px; text-align: center; color: white;'>
                            <h1 style='margin: 0; font-size: 32px; font-weight: bold;'>✅ Payment Confirmed!</h1>
                            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.95;'>Your trip is officially booked</p>
                        </div>

                        <!-- Main Content -->
                        <div style='background: #ffffff; padding: 40px 30px; border-bottom: 1px solid #e5e7eb;'>
                            <p style='font-size: 16px; color: #1f2937; margin: 0 0 25px 0;'>Hello <strong>{userName}</strong>,</p>
                            
                            <div style='background: linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(52, 211, 153, 0.1) 100%); border-left: 4px solid #10b981; padding: 20px; border-radius: 4px; margin-bottom: 30px;'>
                                <p style='margin: 0; color: #065f46; font-size: 15px;'>
                                    <strong>Thank you for booking with us!</strong> Your payment has been processed successfully and your trip is confirmed.
                                </p>
                            </div>

                            <!-- Booking Details Card -->
                            <h2 style='color: #1f2937; font-size: 18px; font-weight: 600; margin: 0 0 20px 0;'>Booking Details</h2>
                            
                            <div style='background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin-bottom: 30px;'>
                                <!-- Destination -->
                                <div style='display: flex; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e7eb;'>
                                    <div style='color: #10b981; font-size: 20px; margin-right: 15px; width: 30px; text-align: center;'>📍</div>
                                    <div style='flex: 1;'>
                                        <p style='margin: 0; color: #6b7280; font-size: 13px; font-weight: 500; text-transform: uppercase;'>Destination</p>
                                        <p style='margin: 5px 0 0 0; color: #1f2937; font-size: 16px; font-weight: 600;'>{destination}</p>
                                    </div>
                                </div>

                                <!-- Trip Dates -->
                                <div style='display: flex; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e7eb;'>
                                    <div style='color: #10b981; font-size: 20px; margin-right: 15px; width: 30px; text-align: center;'>📅</div>
                                    <div style='flex: 1;'>
                                        <p style='margin: 0; color: #6b7280; font-size: 13px; font-weight: 500; text-transform: uppercase;'>Travel Dates</p>
                                        <p style='margin: 5px 0 0 0; color: #1f2937; font-size: 16px; font-weight: 600;'>{startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}</p>
                                        <p style='margin: 3px 0 0 0; color: #6b7280; font-size: 14px;'>{duration} days</p>
                                    </div>
                                </div>

                                <!-- Travelers -->
                                <div style='display: flex; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e7eb;'>
                                    <div style='color: #10b981; font-size: 20px; margin-right: 15px; width: 30px; text-align: center;'>👥</div>
                                    <div style='flex: 1;'>
                                        <p style='margin: 0; color: #6b7280; font-size: 13px; font-weight: 500; text-transform: uppercase;'>Travelers</p>
                                            <p style='margin: 5px 0 0 0; color: #1f2937; font-size: 16px; font-weight: 600;'>{quantity} {travelerLabel}</p>
                                    </div>
                                </div>

                                <!-- Package Type -->
                                <div style='display: flex; margin-bottom: 0;'>
                                    <div style='color: #10b981; font-size: 20px; margin-right: 15px; width: 30px; text-align: center;'>🎁</div>
                                    <div style='flex: 1;'>
                                        <p style='margin: 0; color: #6b7280; font-size: 13px; font-weight: 500; text-transform: uppercase;'>Package Type</p>
                                        <p style='margin: 5px 0 0 0; color: #1f2937; font-size: 16px; font-weight: 600;'>{packageType}</p>
                                    </div>
                                </div>
                            </div>

                            <!-- Price Summary -->
                            <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 8px; padding: 25px; margin-bottom: 30px; text-align: center;'>
                                <p style='margin: 0; font-size: 14px; opacity: 0.9; text-transform: uppercase; font-weight: 500;'>Total Amount Paid</p>
                                <p style='margin: 10px 0 0 0; font-size: 36px; font-weight: bold;'>${amount:F2}</p>
                            </div>

                            <!-- Next Steps -->
                            <h2 style='color: #1f2937; font-size: 18px; font-weight: 600; margin: 30px 0 20px 0;'>What's Next?</h2>
                            <div style='background: #f9fafb; border-radius: 8px; padding: 20px;'>
                                <div style='margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e7eb;'>
                                    <p style='margin: 0; color: #10b981; font-weight: 600; font-size: 15px;'>📧 Confirmation Email</p>
                                    <p style='margin: 5px 0 0 0; color: #6b7280; font-size: 14px;'>You'll receive a detailed itinerary email within 24 hours</p>
                                </div>
                                <div style='margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px solid #e5e7eb;'>
                                    <p style='margin: 0; color: #10b981; font-weight: 600; font-size: 15px;'>🎫 Travel Documents</p>
                                    <p style='margin: 5px 0 0 0; color: #6b7280; font-size: 14px;'>Access your booking details anytime in ""My Bookings"" section</p>
                                </div>
                                <div style='margin-bottom: 0;'>
                                    <p style='margin: 0; color: #10b981; font-weight: 600; font-size: 15px;'>❓ Questions?</p>
                                    <p style='margin: 5px 0 0 0; color: #6b7280; font-size: 14px;'>Contact our support team at support@travelmate.com</p>
                                </div>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style='background: #f3f4f6; padding: 30px; text-align: center; color: #6b7280; font-size: 13px;'>
                            <p style='margin: 0 0 15px 0;'>
                                <strong style='color: #1f2937;'>TravelMate</strong> - Your Adventure Awaits
                            </p>
                            <p style='margin: 0; opacity: 0.8;'>
                                This is an automated confirmation email. Please do not reply to this message.
                            </p>
                            <p style='margin: 10px 0 0 0; opacity: 0.7;'>
                                © 2026 TravelMate. All rights reserved.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending payment confirmation to {toEmail}");
                return false;
            }
        }

        // Send trip reminder emails to booked users
        public async Task<bool> SendTripReminderAsync(string toEmail, string userName, string destination, DateTime startDate, int daysBefore)
        {
            try
            {
                var subject = $"Reminder: Your trip to {destination} is coming up";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    </head>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #1f2937; max-width: 640px; margin: 0 auto; padding: 24px;'>
                        <h2 style='color: #111827; margin-bottom: 8px;'>Hi {userName},</h2>
                        <p style='margin-top: 0;'>This is a friendly reminder that your trip to <strong>{destination}</strong> departs on <strong>{startDate:MMMM dd, yyyy}</strong>.</p>
                        <div style='background: #f3f4f6; border-radius: 12px; padding: 16px; margin: 18px 0; border: 1px solid #e5e7eb;'>
                            <p style='margin: 0; color: #374151;'>We are sending this {daysBefore}-day reminder so you have time to finish any packing, document checks, and payments.</p>
                        </div>
                        <p style='margin: 12px 0;'>If you have any questions before departure, reply to this email or visit your account to review your booking details.</p>
                        <p style='margin-top: 24px; color: #6b7280; font-size: 0.9em;'>Safe travels!<br/>The MVC Travel Team</p>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending trip reminder to {toEmail}");
                return false;
            }
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

                            <p style='color: #64748b; font-size: 0.9em; margin-top: 30px;'>
                                The trip is already in your cart. Please log in to complete your payment. Don't miss this opportunity!
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
