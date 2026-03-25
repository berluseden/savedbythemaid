using FluentValidation;
using SavedByTheMaid.Application.DTOs.Admin;

namespace SavedByTheMaid.Application.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(1, 100)
            .WithMessage("First name must be between 1 and 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(1, 100)
            .WithMessage("Last name must be between 1 and 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]+$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number contains invalid characters.")
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Address))
            .WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.PrimaryServiceAreaId)
            .GreaterThan(0)
            .When(x => x.PrimaryServiceAreaId.HasValue)
            .WithMessage("Primary service area ID must be greater than 0.");

        RuleFor(x => x.MaxDailyHours)
            .InclusiveBetween(1, 24)
            .When(x => x.MaxDailyHours.HasValue)
            .WithMessage("Max daily hours must be between 1 and 24.");

        RuleFor(x => x.MaxDailyServices)
            .InclusiveBetween(1, 20)
            .When(x => x.MaxDailyServices.HasValue)
            .WithMessage("Max daily services must be between 1 and 20.");
    }
}
