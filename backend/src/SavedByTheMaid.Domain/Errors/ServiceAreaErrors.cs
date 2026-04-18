using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Errors;

public static class ServiceAreaErrors
{
    public static readonly Error NotFound = new(
        "ServiceArea.NotFound",
        "The service area was not found.");

    public static readonly Error ZipCodeAlreadyAssigned = new(
        "ServiceArea.ZipCodeAlreadyAssigned",
        "The ZIP code is already assigned to a service area.");

    public static readonly Error ZipCodeNotCovered = new(
        "ServiceArea.ZipCodeNotCovered",
        "The ZIP code is not covered by any service area.");

    public static Error ZipCodeAssignedToArea(string zipCode, string areaName) => new(
        "ServiceArea.ZipCodeAssignedToArea",
        $"ZIP code {zipCode} is already assigned to service area '{areaName}'.");

    public static Error AreaInactive(int serviceAreaId) => new(
        "ServiceArea.AreaInactive",
        $"Service area {serviceAreaId} is inactive and not accepting bookings.");
}
