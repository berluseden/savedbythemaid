using FluentValidation;

namespace SavedByTheMaid.Api.Validators;

public class CreateAdditionalServiceRequestValidator : AbstractValidator<CreateAdditionalServiceRequest>
{
    public CreateAdditionalServiceRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256).WithMessage("Title cannot exceed 256 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

        RuleFor(x => x.AdditionalMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Additional minutes must be greater than or equal to 0.");
    }
}

public class UpdateAdditionalServiceRequestValidator : AbstractValidator<UpdateAdditionalServiceRequest>
{
    public UpdateAdditionalServiceRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(256).WithMessage("Title cannot exceed 256 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

        RuleFor(x => x.AdditionalMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Additional minutes must be greater than or equal to 0.");
    }
}

public class CreateServiceTypeRequestValidator : AbstractValidator<CreateServiceTypeRequest>
{
    public CreateServiceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.PricePerBedroom)
            .GreaterThanOrEqualTo(0).WithMessage("Price per bedroom must be greater than or equal to 0.");

        RuleFor(x => x.PricePerBathroom)
            .GreaterThanOrEqualTo(0).WithMessage("Price per bathroom must be greater than or equal to 0.");

        RuleFor(x => x.EstimatedMinutes)
            .GreaterThan(0).WithMessage("Estimated minutes must be greater than 0.");

        RuleFor(x => x.MinutesPerBedroom)
            .GreaterThanOrEqualTo(0).WithMessage("Minutes per bedroom must be greater than or equal to 0.");

        RuleFor(x => x.MinutesPerBathroom)
            .GreaterThanOrEqualTo(0).WithMessage("Minutes per bathroom must be greater than or equal to 0.");
    }
}

public class UpdateServiceTypeRequestValidator : AbstractValidator<UpdateServiceTypeRequest>
{
    public UpdateServiceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.PricePerBedroom)
            .GreaterThanOrEqualTo(0).WithMessage("Price per bedroom must be greater than or equal to 0.");

        RuleFor(x => x.PricePerBathroom)
            .GreaterThanOrEqualTo(0).WithMessage("Price per bathroom must be greater than or equal to 0.");

        RuleFor(x => x.EstimatedMinutes)
            .GreaterThan(0).WithMessage("Estimated minutes must be greater than 0.");

        RuleFor(x => x.MinutesPerBedroom)
            .GreaterThanOrEqualTo(0).WithMessage("Minutes per bedroom must be greater than or equal to 0.");

        RuleFor(x => x.MinutesPerBathroom)
            .GreaterThanOrEqualTo(0).WithMessage("Minutes per bathroom must be greater than or equal to 0.");
    }
}

public class CreateCleaningPlaceRequestValidator : AbstractValidator<CreateCleaningPlaceRequest>
{
    public CreateCleaningPlaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");
    }
}

public class UpdateCleaningPlaceRequestValidator : AbstractValidator<UpdateCleaningPlaceRequest>
{
    public UpdateCleaningPlaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");
    }
}

public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.BaseMinutes)
            .GreaterThan(0).WithMessage("Base minutes must be greater than 0.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0.");
    }
}

public class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.BaseMinutes)
            .GreaterThan(0).WithMessage("Base minutes must be greater than 0.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0.");
    }
}

public class CreateEquipmentRequestValidator : AbstractValidator<CreateEquipmentRequest>
{
    public CreateEquipmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description != null)
            .WithMessage("Description cannot exceed 1000 characters.");
    }
}

public class UpdateEquipmentRequestValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description != null)
            .WithMessage("Description cannot exceed 1000 characters.");
    }
}

public class CreateServiceAreaRequestValidator : AbstractValidator<CreateServiceAreaRequest>
{
    public CreateServiceAreaRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");
    }
}

public class UpdateServiceAreaRequestValidator : AbstractValidator<UpdateServiceAreaRequest>
{
    public UpdateServiceAreaRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name cannot exceed 256 characters.");
    }
}

public class AddZipCodeRequestValidator : AbstractValidator<AddZipCodeRequest>
{
    public AddZipCodeRequestValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .MaximumLength(20).WithMessage("Zip code cannot exceed 20 characters.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Zip code must be a valid US zip code (e.g. 12345 or 12345-6789).");
    }
}
