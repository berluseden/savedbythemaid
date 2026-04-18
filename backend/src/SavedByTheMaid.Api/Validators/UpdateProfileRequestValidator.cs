using FluentValidation;
using SavedByTheMaid.Application.Validators;

namespace SavedByTheMaid.Api.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\+]?[\d\s\-\(\)]{7,20}$")
            .WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Address)
            .MaximumLength(100).WithMessage("Address cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .Length(2).WithMessage("State must be exactly 2 characters.")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.ZipCode)
            .IsValidZipCode()
            .When(x => !string.IsNullOrEmpty(x.ZipCode));
    }
}
