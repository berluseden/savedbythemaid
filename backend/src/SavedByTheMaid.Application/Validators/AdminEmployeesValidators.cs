using FluentValidation;
using SavedByTheMaid.Application.DTOs.Admin;

namespace SavedByTheMaid.Application.Validators;

// CreateEmployeeRequest is already validated by CreateEmployeeRequestValidator.cs

public class UpdateEmployeeAdminRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeAdminRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\-\+\(\)]+$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number contains invalid characters.")
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address))
            .WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.MaxDailyHours)
            .InclusiveBetween(1, 24).When(x => x.MaxDailyHours.HasValue)
            .WithMessage("Max daily hours must be between 1 and 24.");

        RuleFor(x => x.MaxDailyServices)
            .InclusiveBetween(1, 20).When(x => x.MaxDailyServices.HasValue)
            .WithMessage("Max daily services must be between 1 and 20.");
    }
}

public class CreateEmployeeTimeOffRequestValidator : AbstractValidator<CreateEmployeeTimeOffRequest>
{
    public CreateEmployeeTimeOffRequestValidator()
    {
        RuleFor(x => x.EndDateTime)
            .GreaterThan(x => x.StartDateTime)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.StartDateTime)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Time off cannot be scheduled in the past.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class CreateEmployeeScheduleRequestValidator : AbstractValidator<CreateEmployeeScheduleRequest>
{
    public CreateEmployeeScheduleRequestValidator()
    {
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be greater than start time.");

        RuleFor(x => x.BufferMinutes)
            .InclusiveBetween(0, 120)
            .WithMessage("Buffer minutes must be between 0 and 120.");
    }
}
