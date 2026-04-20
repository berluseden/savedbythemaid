using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Singleton record holding the business contact info displayed on the public Contact page.
/// Only one row should ever exist (Id = 1). The DataSeeder creates it on first boot.
/// </summary>
public class BusinessInfo : BaseEntity
{
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    /// <summary>e.g. "8:00 AM – 8:00 PM"</summary>
    public string WeekdayHours { get; set; } = "";
    /// <summary>Empty string = closed</summary>
    public string SaturdayHours { get; set; } = "";
    /// <summary>Empty string = closed</summary>
    public string SundayHours { get; set; } = "";
    public string ResponseTime { get; set; } = "We reply within 24 hours";
}
