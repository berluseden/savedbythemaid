namespace SavedByTheMaid.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
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
/// Tipo de bloqueo/tiempo libre de empleada
/// </summary>
public enum TimeOffType
{
    TimeOff = 0,        // Permiso general
    Vacation = 1,       // Vacaciones
    Sick = 2,           // Enfermedad
    Personal = 3,       // Personal
    ManualBlock = 4     // Bloqueo manual por admin
}

/// <summary>
/// Estado de aprobación de tiempo libre
/// </summary>
public enum TimeOffStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Estado de ajuste por incidencia en ServiceMeet
/// </summary>
public enum AdjustmentStatus
{
    None = 0,
    PendingReview = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// Tipo de condición para multiplicadores de precio
/// </summary>
public enum MultiplierConditionType
{
    SquareFootage = 0,      // Por m²/sq ft
    DirtLevel = 1,          // Nivel de suciedad (ligero/normal/alto)
    HasPets = 2,            // Tiene mascotas
    FirstTime = 3,          // Primera vez (vs recurrente)
    FloorLevel = 4,         // Piso del inmueble
    NoElevator = 5,         // Sin ascensor
    ExtraRooms = 6          // Habitaciones adicionales
}

/// <summary>
/// Nivel de suciedad del lugar
/// </summary>
public enum DirtLevel
{
    Light = 0,
    Normal = 1,
    Heavy = 2
}
