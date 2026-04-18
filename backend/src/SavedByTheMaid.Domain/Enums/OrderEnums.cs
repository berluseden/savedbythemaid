namespace SavedByTheMaid.Domain.Enums;

public enum OrderStatus
{
    /// <summary>
    /// Order pending admin review (initial state from web)
    /// </summary>
    PendingReview = 0,
    
    /// <summary>
    /// Draft - Order created but not finalized (deprecated, use PendingReview)
    /// </summary>
    [Obsolete("Use PendingReview for new orders")]
    Draft = 1,
    
    /// <summary>
    /// Order confirmed and ready to be executed
    /// </summary>
    Confirmed = 2,
    
    /// <summary>
    /// Service in progress
    /// </summary>
    InProgress = 3,
    
    /// <summary>
    /// Service completed successfully
    /// </summary>
    Completed = 4,
    
    /// <summary>
    /// Order cancelled
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Customer did not show up or did not allow access
    /// </summary>
    NoShow = 6
}

public enum RecurrenceType
{
    None = 0,
    Weekly = 1,
    BiWeekly = 2,
    Monthly = 3
}

public enum OrderSource
{
    Website = 0,
    Phone = 1,
    App = 2,
    Referral = 3,
    Admin = 4
}

public enum MeetStatus
{
    Scheduled = 0,
    Assigned = 1,
    OnTheWay = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Rescheduled = 6,
    NoShow = 7
}

public enum SoftReserveStatus
{
    Active = 0,
    Converted = 1,
    Expired = 2,
    Cancelled = 3
}

public enum DayOfWeekFlag
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64
}

/// <summary>
/// Type of employee block/time off
/// </summary>
public enum TimeOffType
{
    TimeOff = 0,        // General leave
    Vacation = 1,       // Vacation
    Sick = 2,           // Sick leave
    Personal = 3,       // Personal
    ManualBlock = 4     // Manual block by admin
}

/// <summary>
/// Time off approval status
/// </summary>
public enum TimeOffStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Incident adjustment status in ServiceMeet
/// </summary>
public enum AdjustmentStatus
{
    None = 0,
    PendingReview = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Condition type for price multipliers
/// </summary>
public enum MultiplierConditionType
{
    SquareFootage = 0,      // By sq ft/m²
    DirtLevel = 1,          // Dirt level (light/normal/heavy)
    HasPets = 2,            // Has pets
    FirstTime = 3,          // First time (vs recurring)
    FloorLevel = 4,         // Building floor level
    NoElevator = 5,         // No elevator
    ExtraRooms = 6          // Extra rooms
}

/// <summary>
/// Dirt level of the place
/// </summary>
public enum DirtLevel
{
    Light = 0,
    Normal = 1,
    Heavy = 2
}

/// <summary>
/// Slot occupancy type for the anti-collision model.
/// SoftReserve is temporary and expires, Meeting is permanent.
/// </summary>
public enum OccupancyType
{
    /// <summary>
    /// Temporary reservation that expires after a certain time
    /// </summary>
    SoftReserve = 0,
    
    /// <summary>
    /// Confirmed appointment (ServiceMeet)
    /// </summary>
    Meeting = 1
}
