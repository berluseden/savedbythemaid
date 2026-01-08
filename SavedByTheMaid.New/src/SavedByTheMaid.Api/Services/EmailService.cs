using System.Net;
using System.Net.Mail;

namespace SavedByTheMaid.Api.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendBookingConfirmationAsync(string email, BookingConfirmationEmail data)
    {
        var subject = $"Booking Confirmed - {data.ServiceType}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Booking Confirmed!</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {data.CustomerName},</p>
                    <p>Your cleaning service has been confirmed. Here are the details:</p>
                    
                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Service:</strong> {data.ServiceType}</p>
                        <p><strong>Date:</strong> {data.ScheduledDate:dddd, MMMM d, yyyy}</p>
                        <p><strong>Time:</strong> {data.ScheduledTime}</p>
                        <p><strong>Duration:</strong> ~{data.EstimatedDuration / 60} hours</p>
                        <p><strong>Address:</strong> {data.Address}</p>
                        <p><strong>Cleaner:</strong> {data.EmployeeName}</p>
                        <hr style='margin: 15px 0; border: none; border-top: 1px solid #e5e7eb;' />
                        <p style='font-size: 1.2em;'><strong>Total:</strong> ${data.TotalAmount:F2}</p>
                    </div>
                    
                    <p>If you need to make any changes, please contact us at least 24 hours before your appointment.</p>
                    
                    <p>Thank you for choosing SavedByTheMaid!</p>
                </div>
                <div style='background-color: #1e293b; padding: 15px; text-align: center; color: #94a3b8; font-size: 12px;'>
                    <p>SavedByTheMaid - Professional Cleaning Services</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingCancelledAsync(string email, BookingCancelledEmail data)
    {
        var subject = "Booking Cancelled";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #ef4444; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Booking Cancelled</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {data.CustomerName},</p>
                    <p>Your booking for {data.ServiceType} on {data.ScheduledDate:MMMM d, yyyy} has been cancelled.</p>
                    {(string.IsNullOrEmpty(data.Reason) ? "" : $"<p><strong>Reason:</strong> {data.Reason}</p>")}
                    <p>If you'd like to reschedule, you can book a new appointment on our website.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingReminderAsync(string email, BookingReminderEmail data)
    {
        var subject = $"Reminder: Cleaning Tomorrow - {data.ServiceType}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Reminder: Your Cleaning is Tomorrow!</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {data.CustomerName},</p>
                    <p>This is a friendly reminder that your cleaning service is scheduled for tomorrow.</p>
                    
                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Service:</strong> {data.ServiceType}</p>
                        <p><strong>Date:</strong> {data.ScheduledDate:dddd, MMMM d, yyyy}</p>
                        <p><strong>Time:</strong> {data.ScheduledTime}</p>
                        <p><strong>Address:</strong> {data.Address}</p>
                        <p><strong>Cleaner:</strong> {data.EmployeeName}</p>
                    </div>
                    
                    <p>Please ensure someone is available to let our cleaner in at the scheduled time.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "Welcome to SavedByTheMaid!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Welcome to SavedByTheMaid!</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {firstName},</p>
                    <p>Thank you for joining SavedByTheMaid! We're excited to help keep your space sparkling clean.</p>
                    <p>With your new account, you can:</p>
                    <ul>
                        <li>Book cleaning services quickly and easily</li>
                        <li>Manage your upcoming appointments</li>
                        <li>View your booking history</li>
                        <li>Save your favorite addresses</li>
                    </ul>
                    <p><a href='#' style='display: inline-block; background-color: #0ea5e9; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;'>Book Your First Cleaning</a></p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.TryParse(_config["Email:SmtpPort"], out var port) ? port : 587;
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPassword = _config["Email:SmtpPassword"];
        var fromEmail = _config["Email:FromEmail"] ?? "noreply@savedbytemaid.com";
        var fromName = _config["Email:FromName"] ?? "SavedByTheMaid";

        // Si no hay configuración SMTP, solo log (desarrollo)
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
        {
            _logger.LogInformation(
                "Email would be sent (SMTP not configured): To={To}, Subject={Subject}", 
                toEmail, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            // No re-throw - emails shouldn't break the flow
        }
    }
}
