using System.ComponentModel.DataAnnotations;

namespace SavedByTheMaid.Application.DTOs.Business;

public record BusinessInfoDto
{
    public string Phone { get; init; } = "";
    public string Email { get; init; } = "";
    public string AddressLine1 { get; init; } = "";
    public string City { get; init; } = "";
    public string State { get; init; } = "";
    public string ZipCode { get; init; } = "";
    public string WeekdayHours { get; init; } = "";
    public string SaturdayHours { get; init; } = "";
    public string SundayHours { get; init; } = "";
    public string ResponseTime { get; init; } = "";
}

public record UpdateBusinessInfoRequest
{
    [StringLength(30)]
    public string Phone { get; init; } = "";

    [EmailAddress]
    [StringLength(100)]
    public string Email { get; init; } = "";

    [StringLength(150)]
    public string AddressLine1 { get; init; } = "";

    [StringLength(100)]
    public string City { get; init; } = "";

    [StringLength(50)]
    public string State { get; init; } = "";

    [StringLength(10)]
    public string ZipCode { get; init; } = "";

    [StringLength(50)]
    public string WeekdayHours { get; init; } = "";

    [StringLength(50)]
    public string SaturdayHours { get; init; } = "";

    [StringLength(50)]
    public string SundayHours { get; init; } = "";

    [StringLength(100)]
    public string ResponseTime { get; init; } = "";
}
