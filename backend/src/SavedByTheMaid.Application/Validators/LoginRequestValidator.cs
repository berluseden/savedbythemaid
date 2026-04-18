using FluentValidation;
using SavedByTheMaid.Application.DTOs.Auth;

namespace SavedByTheMaid.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .IsValidEmail();

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(100)
            .WithMessage("Password cannot exceed 100 characters.");
    }
}
