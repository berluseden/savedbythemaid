using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SavedByTheMaid.Api.Services;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;
    private readonly IConfiguration _configuration;

    public ContactController(
        IEmailService emailService,
        ILogger<ContactController> logger,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a contact form request
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("auth-sensitive")]
    public async Task<IActionResult> SendContactRequest([FromBody] ContactRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminEmail = _configuration["Email:ContactRecipient"]
            ?? _configuration["AdminSeed:Email"]
            ?? "admin@savedbythemaid.com";

        try
        {
            await _emailService.SendContactFormAsync(adminEmail, new ContactFormEmail(
                Name: request.Name,
                Email: request.Email,
                Message: request.Message
            ));

            _logger.LogInformation("Contact form submitted by {Name} ({Email})", request.Name, request.Email);

            return Ok(new { message = "Your message has been sent. We'll get back to you soon!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process contact form from {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred sending your message. Please try again later." });
        }
    }
}

public record ContactRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; init; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "Message is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 5000 characters")]
    public string Message { get; init; } = "";
}
