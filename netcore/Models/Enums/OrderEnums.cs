namespace netcore.Models
{
    /// <summary>
    /// Estado de pago de una orden
    /// </summary>
    public enum PaymentStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Refunded = 3
    }

    /// <summary>
    /// Estado de una orden de servicio
    /// </summary>
    public enum OrderStatus
    {
        Draft = 0,
        Confirmed = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Tipo de recurrencia para servicios
    /// </summary>
    public enum RecurrenceType
    {
        Once = 0,
        Weekly = 1,
        Biweekly = 2,
        Monthly = 3
    }

    /// <summary>
    /// Origen de la orden
    /// </summary>
    public enum OrderSource
    {
        Web = 0,
        Admin = 1,
        Phone = 2
    }

    /// <summary>
    /// Estado de una cita de servicio
    /// </summary>
    public enum MeetStatus
    {
        Scheduled = 0,
        Assigned = 1,
        OnTheWay = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
        NoShow = 6
    }
}
