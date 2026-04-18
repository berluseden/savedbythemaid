using FluentValidation;
using SavedByTheMaid.Application.DTOs.Orders;

namespace SavedByTheMaid.Application.Validators;

public class CustomerCancelRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CustomerCancelRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}

public class RescheduleOrderRequestValidator : AbstractValidator<RescheduleOrderRequest>
{
    public RescheduleOrderRequestValidator()
    {
        RuleFor(x => x.NewDate)
            .NotEmpty()
            .WithMessage("New date is required.")
            .Must(d => DateOnly.TryParse(d, out _))
            .WithMessage("New date must be a valid date in format YYYY-MM-DD.");

        RuleFor(x => x.NewTime)
            .NotEmpty()
            .WithMessage("New time is required.")
            .Must(t => TimeOnly.TryParse(t, out _))
            .WithMessage("New time must be a valid time (e.g. 09:00).");
    }
}
