using FluentValidation;
using SavedByTheMaid.Application.DTOs.Booking;

namespace SavedByTheMaid.Application.Validators;

public class AvailabilityRequestValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityRequestValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP code is required.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid ZIP code format.");

        RuleFor(x => x.EstimatedMinutes)
            .InclusiveBetween(30, 480).WithMessage("Duration must be between 30 and 480 minutes.");

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Date must be today or in the future.");
    }
}

public class CreateSoftReserveRequestValidator : AbstractValidator<CreateSoftReserveRequest>
{
    public CreateSoftReserveRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Invalid employee ID.")
            .When(x => x.EmployeeId.HasValue);

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP code is required.");

        RuleFor(x => x.EstimatedMinutes)
            .InclusiveBetween(30, 480).WithMessage("Duration must be between 30 and 480 minutes.");

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Date must be today or in the future.");
    }
}

public class EstimateRequestValidator : AbstractValidator<EstimateRequest>
{
    public EstimateRequestValidator()
    {
        RuleFor(x => x.ServiceTypeId)
            .GreaterThan(0)
            .WithMessage("Service type ID must be greater than 0.");

        RuleFor(x => x.Bedrooms)
            .InclusiveBetween(1, 20)
            .WithMessage("Bedrooms must be between 1 and 20.");

        RuleFor(x => x.Bathrooms)
            .InclusiveBetween(1, 20)
            .WithMessage("Bathrooms must be between 1 and 20.");

        RuleFor(x => x.SquareFootage)
            .InclusiveBetween(100, 50000)
            .When(x => x.SquareFootage.HasValue)
            .WithMessage("Square footage must be between 100 and 50,000.");

        RuleFor(x => x.DirtLevel)
            .IsInEnum()
            .WithMessage("Invalid dirt level specified.");

        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.RoomId)
                .GreaterThan(0)
                .WithMessage("Room ID must be greater than 0.");

            room.RuleFor(r => r.Quantity)
                .InclusiveBetween(1, 20)
                .WithMessage("Room quantity must be between 1 and 20.");
        });

        RuleForEach(x => x.AdditionalServiceIds)
            .GreaterThan(0)
            .WithMessage("Additional service ID must be greater than 0.");
    }
}
