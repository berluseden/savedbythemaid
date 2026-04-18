namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for email sending.
/// Implemented by the Api layer's EmailService via a thin adapter or directly.
/// </summary>
public interface IEmailServiceAdapter
{
    /// <summary>
    /// Sends a contact form inquiry to the admin email.
    /// </summary>
    Task SendContactFormAsync(string adminEmail, ContactFormData data);
}

/// <summary>
/// Application-layer model for contact form email data.
/// </summary>
public record ContactFormData(string Name, string Email, string Message);
