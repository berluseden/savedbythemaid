using System.Text.Encodings.Web;
using SavedByTheMaid.Api.Services.Email;

namespace SavedByTheMaid.Api.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _config;
    private readonly IEmailDispatcher _dispatcher;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration config,
        IEmailDispatcher dispatcher)
    {
        _logger = logger;
        _config = config;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// HTML-encodes a user-supplied string to prevent XSS in email templates.
    /// </summary>
    private static string H(string? s) => HtmlEncoder.Default.Encode(s ?? string.Empty);

    public async Task SendBookingConfirmationAsync(string email, BookingConfirmationEmail data)
    {
        var subject = $"Booking Confirmed - {H(data.ServiceType)}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Booking Confirmed!</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {H(data.CustomerName)},</p>
                    <p>Your cleaning service has been confirmed. Here are the details:</p>

                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Service:</strong> {H(data.ServiceType)}</p>
                        <p><strong>Date:</strong> {data.ScheduledDate:dddd, MMMM d, yyyy}</p>
                        <p><strong>Time:</strong> {H(data.ScheduledTime)}</p>
                        <p><strong>Duration:</strong> ~{data.EstimatedDuration / 60} hours</p>
                        <p><strong>Address:</strong> {H(data.Address)}</p>
                        <p><strong>Cleaner:</strong> {H(data.EmployeeName)}</p>
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
                    <p>Hi {H(data.CustomerName)},</p>
                    <p>Your booking for {H(data.ServiceType)} on {data.ScheduledDate:MMMM d, yyyy} has been cancelled.</p>
                    {(string.IsNullOrEmpty(data.Reason) ? "" : $"<p><strong>Reason:</strong> {H(data.Reason)}</p>")}
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
                    <p>Hi {H(data.CustomerName)},</p>
                    <p>This is a friendly reminder that your cleaning service is scheduled for tomorrow.</p>

                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Service:</strong> {H(data.ServiceType)}</p>
                        <p><strong>Date:</strong> {data.ScheduledDate:dddd, MMMM d, yyyy}</p>
                        <p><strong>Time:</strong> {H(data.ScheduledTime)}</p>
                        <p><strong>Address:</strong> {H(data.Address)}</p>
                        <p><strong>Cleaner:</strong> {H(data.EmployeeName)}</p>
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
                    <p>Hi {H(firstName)},</p>
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

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password - SavedByTheMaid";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Password Reset Request</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi,</p>
                    <p>We received a request to reset your password. Click the link below to set a new password:</p>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='display: inline-block; background-color: #0ea5e9; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Reset Password</a>
                    </div>

                    <p>This link will expire in 1 hour.</p>
                    <p>If you did not request a password reset, you can safely ignore this email.</p>

                    <p>Thank you,<br/>SavedByTheMaid Team</p>
                </div>
                <div style='background-color: #1e293b; padding: 15px; text-align: center; color: #94a3b8; font-size: 12px;'>
                    <p>SavedByTheMaid - Professional Cleaning Services</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendContactFormAsync(string adminEmail, ContactFormEmail data)
    {
        var subject = $"Contact Form: {H(data.Subject)} (from {H(data.Name)})";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>New Contact Form Submission</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Name:</strong> {H(data.Name)}</p>
                        <p><strong>Email:</strong> {H(data.Email)}</p>
                        <p><strong>Subject:</strong> {H(data.Subject)}</p>
                        <hr style='margin: 15px 0; border: none; border-top: 1px solid #e5e7eb;' />
                        <p><strong>Message:</strong></p>
                        <p>{H(data.Message)}</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(adminEmail, subject, body);
    }

    public async Task SendContactAutoReplyAsync(string userEmail, string userName)
    {
        var subject = "We received your message - SavedByTheMaid";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #0ea5e9; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Message Received!</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {H(userName)},</p>
                    <p>Thank you for reaching out to us. We have received your message and will respond within 24 hours.</p>
                    <p>If your inquiry is urgent, please call us directly.</p>
                    <p>Thank you,<br/>SavedByTheMaid Team</p>
                </div>
                <div style='background-color: #1e293b; padding: 15px; text-align: center; color: #94a3b8; font-size: 12px;'>
                    <p>SavedByTheMaid - Professional Cleaning Services</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(userEmail, subject, body);
    }

    public async Task SendOrderCancelledToEmployeeAsync(string employeeEmail, OrderCancelledEmployeeEmail data)
    {
        var subject = $"Booking Cancelled - {H(data.ServiceType)} on {data.ScheduledDate:MMM d, yyyy}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #ef4444; padding: 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>Booking Cancelled</h1>
                </div>
                <div style='padding: 20px; background-color: #f8fafc;'>
                    <p>Hi {H(data.EmployeeName)},</p>
                    <p>The following booking has been cancelled:</p>
                    <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>Customer:</strong> {H(data.CustomerName)}</p>
                        <p><strong>Service:</strong> {H(data.ServiceType)}</p>
                        <p><strong>Date:</strong> {data.ScheduledDate:dddd, MMMM d, yyyy}</p>
                        <p><strong>Address:</strong> {H(data.Address)}</p>
                        {(string.IsNullOrEmpty(data.Reason) ? "" : $"<p><strong>Reason:</strong> {H(data.Reason)}</p>")}
                    </div>
                    <p>Your schedule has been updated accordingly.</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(employeeEmail, subject, body);
    }

    /// <summary>
    /// Hands the envelope to the background dispatcher. Returns once the
    /// envelope is queued (sub-millisecond) — the actual transport call
    /// runs in <see cref="Email.EmailQueueWorker"/> with HTTP-level
    /// retries managed by the resilience handler in the HttpClient pipeline.
    ///
    /// Errors during enqueue (e.g. cancelled host shutdown) are logged but
    /// not re-thrown so the caller flow (booking confirm, contact form,
    /// etc.) is never blocked by mail-transport problems.
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            await _dispatcher.EnqueueAsync(new EmailEnvelope(toEmail, subject, htmlBody));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue email to {To} ({Subject})", toEmail, subject);
            _ = _config; // reserved for future template/locale lookups
        }
    }
}
