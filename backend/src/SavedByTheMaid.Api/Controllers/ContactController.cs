using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Application.DTOs.Contact;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IValidator<ContactRequest> _contactValidator;

    public ContactController(
        IEmailService emailService,
        ILogger<ContactController> logger,
        IConfiguration configuration,
        IValidator<ContactRequest> contactValidator)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _contactValidator = contactValidator;
    }

    /// <summary>
    /// Send a contact form request
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("contact")]
    public async Task<IActionResult> SendContactRequest([FromBody] ContactRequest request)
    {
        var validationError = await _contactValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var adminEmail = _configuration["Email:ContactRecipient"]
            ?? _configuration["AdminSeed:Email"]
            ?? "admin@savedbythemaid.com";

        try
        {
            await _emailService.SendContactFormAsync(adminEmail, new ContactFormEmail(
                Name: request.Name,
                Email: request.Email,
                Subject: request.Subject,
                Message: request.Message
            ));

            _logger.LogInformation("Contact form submitted by {Name} ({Email})", request.Name, request.Email);

            // Fire-and-forget auto-reply to user — do not fail the request if this errors
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendContactAutoReplyAsync(request.Email, request.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send contact auto-reply to {Email}", request.Email);
                }
            });

            return Ok(new { message = "Your message has been sent. We'll get back to you soon!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process contact form from {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred sending your message. Please try again later." });
        }
    }
}
