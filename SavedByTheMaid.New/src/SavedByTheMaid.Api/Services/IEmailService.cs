namespace SavedByTheMaid.Api.Services;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(string email, BookingConfirmationEmail data);
    Task SendBookingCancelledAsync(string email, BookingCancelledEmail data);
    Task SendBookingReminderAsync(string email, BookingReminderEmail data);
    Task SendWelcomeEmailAsync(string email, string firstName);
    Task SendPasswordResetAsync(string email, string resetLink);
    Task SendContactFormAsync(string adminEmail, ContactFormEmail data);
}

public record ContactFormEmail(
    string Name,
    string Email,
    string Message
);

public record BookingConfirmationEmail(
    string CustomerName,
    string ServiceType,
    DateTime ScheduledDate,
    string ScheduledTime,
    string Address,
    decimal TotalAmount,
    string EmployeeName,
    int EstimatedDuration
);

public record BookingCancelledEmail(
    string CustomerName,
    string ServiceType,
    DateTime ScheduledDate,
    string Reason
);

public record BookingReminderEmail(
    string CustomerName,
    string ServiceType,
    DateTime ScheduledDate,
    string ScheduledTime,
    string Address,
    string EmployeeName
);
