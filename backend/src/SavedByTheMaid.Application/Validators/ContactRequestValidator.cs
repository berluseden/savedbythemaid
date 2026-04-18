using FluentValidation;
using SavedByTheMaid.Application.DTOs.Contact;

namespace SavedByTheMaid.Application.Validators;

public class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    public ContactRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Length(2, 100)
            .WithMessage("Name must be between 2 and 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .IsValidEmail();

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]+$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number contains invalid characters.")
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required.")
            .Length(3, 200)
            .WithMessage("Subject must be between 3 and 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required.")
            .Length(10, 5000)
            .WithMessage("Message must be between 10 and 5,000 characters.");
    }
}
