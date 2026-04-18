using FluentValidation;
using SavedByTheMaid.Api.Controllers;

namespace SavedByTheMaid.Api.Validators;

public class CreatePriceMultiplierRequestValidator : AbstractValidator<CreatePriceMultiplierRequest>
{
    public CreatePriceMultiplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Factor)
            .GreaterThan(0).WithMessage("Factor must be greater than 0.");
    }
}

public class UpdatePriceMultiplierRequestValidator : AbstractValidator<UpdatePriceMultiplierRequest>
{
    public UpdatePriceMultiplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Factor)
            .GreaterThan(0).WithMessage("Factor must be greater than 0.");
    }
}

public class CreateRecurrenceDiscountRequestValidator : AbstractValidator<CreateRecurrenceDiscountRequest>
{
    public CreateRecurrenceDiscountRequestValidator()
    {
        RuleFor(x => x.DiscountPercent)
            .GreaterThanOrEqualTo(0).WithMessage("Discount must be between 0 and 1.")
            .LessThanOrEqualTo(1).WithMessage("Discount must be between 0 and 1.");
    }
}
