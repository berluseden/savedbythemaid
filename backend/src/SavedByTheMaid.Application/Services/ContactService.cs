using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SavedByTheMaid.Application.DTOs.Contact;
using SavedByTheMaid.Application.Interfaces;
using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Errors;

namespace SavedByTheMaid.Application.Services;

/// <summary>
/// Application service for handling contact/inquiry requests.
/// Validates input and delegates to the email service adapter.
/// </summary>
public class ContactService : IContactService
{
    private readonly IEmailServiceAdapter _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        IEmailServiceAdapter emailService,
        IConfiguration configuration,
        ILogger<ContactService> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> SendContactRequestAsync(ContactRequest request)
    {
        try
        {
            // Validate basic input
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                return Result.Failure(ContactErrors.SendFailed);
            }

            var adminEmail = _configuration["Email:ContactRecipient"]
                ?? _configuration["AdminSeed:Email"]
                ?? "admin@savedbythemaid.com";

            await _emailService.SendContactFormAsync(adminEmail, new ContactFormData(
                Name: request.Name,
                Email: request.Email,
                Message: request.Message
            ));

            _logger.LogInformation(
                "Contact form submitted by {Name} ({Email}), subject: {Subject}",
                request.Name, request.Email, request.Subject);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process contact form from {Email}", request.Email);
            return Result.Failure(ContactErrors.SendFailed);
        }
    }
}
