using FluentValidation;
using SavedByTheMaid.Api.Controllers.Admin;

namespace SavedByTheMaid.Api.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .MustMeetPasswordPolicy();

        RuleFor(x => x.FirstName)
            .MaximumLength(100).When(x => x.FirstName != null)
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).When(x => x.LastName != null)
            .WithMessage("Last name cannot exceed 100 characters.");
    }
}
