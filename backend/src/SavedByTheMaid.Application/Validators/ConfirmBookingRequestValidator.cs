using FluentValidation;
using SavedByTheMaid.Application.DTOs.Booking;

namespace SavedByTheMaid.Application.Validators;

public class ConfirmBookingRequestValidator : AbstractValidator<ConfirmBookingRequest>
{
    public ConfirmBookingRequestValidator()
    {
        RuleFor(x => x.SoftReserveId)
            .GreaterThan(0)
            .WithMessage("A valid soft reserve ID is required.");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.")
            .MinimumLength(10)
            .WithMessage("Session ID must be at least 10 characters.")
            .MaximumLength(100);

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .WithMessage("ZIP code is required.")
            .Matches(@"^\d{5}(-\d{4})?$")
            .WithMessage("ZIP code must be in the format 12345 or 12345-6789.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required.")
            .Length(5, 500)
            .WithMessage("Address must be between 5 and 500 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200)
            .When(x => x.AddressLine2 != null);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => x.City != null);

        RuleFor(x => x.State)
            .MaximumLength(50)
            .When(x => x.State != null);

        RuleFor(x => x.ServiceTypeId)
            .GreaterThan(0)
            .WithMessage("A valid service type is required.");

        RuleFor(x => x.Bedrooms)
            .InclusiveBetween(0, 20)
            .WithMessage("Bedrooms must be between 0 and 20.");

        RuleFor(x => x.Bathrooms)
            .InclusiveBetween(0, 20)
            .WithMessage("Bathrooms must be between 0 and 20.");

        RuleFor(x => x.SquareFootage)
            .InclusiveBetween(0, 50000)
            .When(x => x.SquareFootage.HasValue)
            .WithMessage("Square footage must be between 0 and 50,000.");

        RuleFor(x => x.FloorLevel)
            .InclusiveBetween(0, 100)
            .When(x => x.FloorLevel.HasValue)
            .WithMessage("Floor level must be between 0 and 100.");

        RuleFor(x => x.Total)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total must be a non-negative amount.")
            .LessThanOrEqualTo(100000)
            .WithMessage("Total cannot exceed $100,000.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(256);

        RuleFor(x => x.ContactName)
            .MaximumLength(100)
            .When(x => x.ContactName != null);

        RuleFor(x => x.ContactPhone)
            .Matches(@"^[\d\s\-\+\(\)]+$")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Phone number contains invalid characters.")
            .MaximumLength(20);

        RuleFor(x => x.Password)
            .MinimumLength(8)
            .When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100);

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(1000)
            .When(x => x.SpecialInstructions != null);

        RuleFor(x => x.RecurrenceType)
            .IsInEnum()
            .WithMessage("Invalid recurrence type.");

        RuleFor(x => x.RecurrenceEndDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.RecurrenceEndDate.HasValue)
            .WithMessage("Recurrence end date must be in the future.");

        RuleFor(x => x.DirtLevel)
            .IsInEnum()
            .WithMessage("Invalid dirt level.");

        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.RoomId).GreaterThan(0);
            room.RuleFor(r => r.Quantity).InclusiveBetween(1, 20);
        });

        RuleForEach(x => x.AdditionalServiceIds)
            .GreaterThan(0)
            .When(x => x.AdditionalServiceIds != null);
    }
}
