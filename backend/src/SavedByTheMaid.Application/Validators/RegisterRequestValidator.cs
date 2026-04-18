using FluentValidation;
using SavedByTheMaid.Application.DTOs.Auth;

namespace SavedByTheMaid.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .IsValidEmail();

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100)
            .WithMessage("Password cannot exceed 100 characters.")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]+$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number contains invalid characters.")
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.FirstName))
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.LastName))
            .WithMessage("Last name cannot exceed 100 characters.");
    }
}
