using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

/// <summary>
/// Servicio de programación que maneja la validación de conflictos y gestión de SlotOccupancy.
/// Implementa el modelo anti-colisión usando la tabla SlotOccupancy con UNIQUE(EmployeeId, SlotStart).
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Verifica si existe conflicto para un empleado en un rango horario.
    /// </summary>
    /// <param name="employeeId">ID del empleado a verificar</param>
    /// <param name="start">Inicio del rango horario</param>
    /// <param name="end">Fin del rango horario</param>
    /// <param name="excludeMeetingId">ID del meeting a excluir (para reschedule del mismo meeting)</param>
    /// <returns>null si no hay conflicto, SchedulingConflict con detalles si existe</returns>
    Task<SchedulingConflict?> CheckConflictsAsync(int employeeId, DateTime start, DateTime end, int? excludeMeetingId = null);

    /// <summary>
    /// Adquiere slots en SlotOccupancy para un empleado en un rango horario.
    /// Los slots se crean con granularidad de 30 minutos.
    /// </summary>
    /// <param name="employeeId">ID del empleado</param>
    /// <param name="start">Inicio del rango</param>
    /// <param name="end">Fin del rango</param>
    /// <param name="type">Tipo de ocupación (SoftReserve o Meeting)</param>
    /// <param name="referenceId">ID de referencia (SoftReserve o ServiceMeet)</param>
    /// <param name="expiresAt">Fecha de expiración (solo para SoftReserve)</param>
    Task AcquireSlotsAsync(int employeeId, DateTime start, DateTime end, OccupancyType type, int referenceId, DateTime? expiresAt = null);

    /// <summary>
    /// Libera slots de SlotOccupancy por referenceId y tipo.
    /// </summary>
    /// <param name="referenceId">ID de referencia</param>
    /// <param name="type">Tipo de ocupación</param>
    Task ReleaseSlotsAsync(int referenceId, OccupancyType type);

    /// <summary>
    /// Transfiere slots de un empleado a otro (para reasignación).
    /// </summary>
    /// <param name="referenceId">ID de referencia del meeting</param>
    /// <param name="type">Tipo de ocupación</param>
    /// <param name="newEmployeeId">Nuevo empleado al que transferir</param>
    Task TransferSlotsAsync(int referenceId, OccupancyType type, int newEmployeeId);
}

/// <summary>
/// Resultado de verificación de conflicto con detalles del mismo.
/// </summary>
public record SchedulingConflict
{
    /// <summary>
    /// Tipo de conflicto detectado
    /// </summary>
    public ConflictType Type { get; init; }

    /// <summary>
    /// Mensaje descriptivo del conflicto
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// ID de la entidad en conflicto (Meeting, TimeOff, etc.)
    /// </summary>
    public int? ConflictingEntityId { get; init; }

    /// <summary>
    /// Inicio del rango en conflicto
    /// </summary>
    public DateTime? ConflictStart { get; init; }

    /// <summary>
    /// Fin del rango en conflicto
    /// </summary>
    public DateTime? ConflictEnd { get; init; }

    /// <summary>
    /// ID del empleado afectado
    /// </summary>
    public int EmployeeId { get; init; }

    /// <summary>
    /// Nombre del empleado (si está disponible)
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Detalles adicionales del conflicto
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Tipos de conflicto posibles
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Conflicto con otro meeting/reserva existente
    /// </summary>
    ExistingBooking = 0,

    /// <summary>
    /// Conflicto con tiempo libre aprobado del empleado
    /// </summary>
    TimeOff = 1,

    /// <summary>
    /// Empleado no disponible en el área de servicio
    /// </summary>
    AreaMismatch = 2,

    /// <summary>
    /// Empleado inactivo o no válido
    /// </summary>
    EmployeeUnavailable = 3
}

public class SchedulingService : ISchedulingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulingService> _logger;
    private const int SlotGranularityMinutes = 30;

    public SchedulingService(ApplicationDbContext context, ILogger<SchedulingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SchedulingConflict?> CheckConflictsAsync(int employeeId, DateTime start, DateTime end, int? excludeMeetingId = null)
    {
        _logger.LogInformation(
            "Verificando conflictos para empleado {EmployeeId} en rango {Start} - {End}, excluyendo meeting {ExcludeMeetingId}",
            employeeId, start, end, excludeMeetingId);

        // Verificar que el empleado existe y está activo
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            return new SchedulingConflict
            {
                Type = ConflictType.EmployeeUnavailable,
                Message = "El empleado especificado no existe",
                EmployeeId = employeeId
            };
        }

        if (!employee.IsActive)
        {
            return new SchedulingConflict
            {
                Type = ConflictType.EmployeeUnavailable,
                Message = $"El empleado {employee.FirstName} {employee.LastName} está inactivo",
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}"
            };
        }

        // 1. Verificar conflictos en SlotOccupancy
        var slotConflictQuery = _context.SlotOccupancies
            .Where(s => s.EmployeeId == employeeId && !s.IsDeleted)
            .Where(s => s.SlotStart < end && s.SlotEnd > start);

        // Excluir el meeting actual si es un reschedule
        if (excludeMeetingId.HasValue)
        {
            slotConflictQuery = slotConflictQuery.Where(s => 
                !(s.OccupancyType == OccupancyType.Meeting && s.ReferenceId == excludeMeetingId.Value));
        }

        var existingSlot = await slotConflictQuery
            .OrderBy(s => s.SlotStart)
            .FirstOrDefaultAsync();

        if (existingSlot != null)
        {
            // Obtener detalles del meeting o reserve en conflicto
            string details = existingSlot.OccupancyType == OccupancyType.Meeting
                ? await GetMeetingDetailsAsync(existingSlot.ReferenceId)
                : $"Reserva temporal (expira: {existingSlot.ExpiresAt:HH:mm})";

            return new SchedulingConflict
            {
                Type = ConflictType.ExistingBooking,
                Message = $"El empleado ya tiene una {(existingSlot.OccupancyType == OccupancyType.Meeting ? "cita" : "reserva")} en ese horario",
                ConflictingEntityId = existingSlot.ReferenceId,
                ConflictStart = existingSlot.SlotStart,
                ConflictEnd = existingSlot.SlotEnd,
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Details = details
            };
        }

        // 2. Verificar conflictos con EmployeeTimeOff aprobados
        var timeOffConflict = await _context.EmployeeTimeOffs
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && !t.IsDeleted)
            .Where(t => t.Status == TimeOffStatus.Approved)
            .Where(t => t.StartDateTime < end && t.EndDateTime > start)
            .FirstOrDefaultAsync();

        if (timeOffConflict != null)
        {
            string timeOffType = timeOffConflict.Type switch
            {
                TimeOffType.Vacation => "vacaciones",
                TimeOffType.Sick => "licencia médica",
                TimeOffType.Personal => "permiso personal",
                TimeOffType.ManualBlock => "bloqueo manual",
                _ => "tiempo libre"
            };

            return new SchedulingConflict
            {
                Type = ConflictType.TimeOff,
                Message = $"El empleado tiene {timeOffType} aprobado en ese horario",
                ConflictingEntityId = timeOffConflict.Id,
                ConflictStart = timeOffConflict.StartDateTime,
                ConflictEnd = timeOffConflict.EndDateTime,
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Details = timeOffConflict.Reason
            };
        }

        _logger.LogInformation("No se encontraron conflictos para empleado {EmployeeId}", employeeId);
        return null; // Sin conflictos
    }

    public async Task AcquireSlotsAsync(int employeeId, DateTime start, DateTime end, OccupancyType type, int referenceId, DateTime? expiresAt = null)
    {
        _logger.LogInformation(
            "Adquiriendo slots para empleado {EmployeeId}, rango {Start} - {End}, tipo {Type}, ref {ReferenceId}",
            employeeId, start, end, type, referenceId);

        var slots = CalculateSlots(start, end);

        var slotOccupancies = slots.Select(slot => new SlotOccupancy
        {
            EmployeeId = employeeId,
            SlotStart = slot.Start,
            SlotEnd = slot.End,
            OccupancyType = type,
            ReferenceId = referenceId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.SlotOccupancies.AddRange(slotOccupancies);
        
        // No llamamos SaveChanges aquí - se debe llamar dentro de una transacción externa
        _logger.LogInformation("Se prepararon {Count} slots para inserción", slotOccupancies.Count);
    }

    public async Task ReleaseSlotsAsync(int referenceId, OccupancyType type)
    {
        _logger.LogInformation("Liberando slots para referencia {ReferenceId}, tipo {Type}", referenceId, type);

        var slotsToRemove = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == referenceId && s.OccupancyType == type && !s.IsDeleted)
            .ToListAsync();

        if (slotsToRemove.Any())
        {
            _context.SlotOccupancies.RemoveRange(slotsToRemove);
            _logger.LogInformation("Se marcaron {Count} slots para eliminación", slotsToRemove.Count);
        }
        else
        {
            _logger.LogWarning("No se encontraron slots para liberar con referencia {ReferenceId}", referenceId);
        }
    }

    public async Task TransferSlotsAsync(int referenceId, OccupancyType type, int newEmployeeId)
    {
        _logger.LogInformation(
            "Transfiriendo slots de referencia {ReferenceId} a empleado {NewEmployeeId}",
            referenceId, newEmployeeId);

        var slotsToTransfer = await _context.SlotOccupancies
            .Where(s => s.ReferenceId == referenceId && s.OccupancyType == type && !s.IsDeleted)
            .ToListAsync();

        foreach (var slot in slotsToTransfer)
        {
            slot.EmployeeId = newEmployeeId;
            slot.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Se transfirieron {Count} slots al empleado {NewEmployeeId}", 
            slotsToTransfer.Count, newEmployeeId);
    }

    /// <summary>
    /// Calcula los slots de 30 minutos para un rango de tiempo.
    /// </summary>
    private static List<(DateTime Start, DateTime End)> CalculateSlots(DateTime start, DateTime end)
    {
        var slots = new List<(DateTime Start, DateTime End)>();
        var currentSlot = NormalizeToSlotBoundary(start);

        while (currentSlot < end)
        {
            var slotEnd = currentSlot.AddMinutes(SlotGranularityMinutes);
            slots.Add((currentSlot, slotEnd));
            currentSlot = slotEnd;
        }

        return slots;
    }

    /// <summary>
    /// Normaliza una fecha/hora al límite de slot más cercano hacia abajo.
    /// </summary>
    private static DateTime NormalizeToSlotBoundary(DateTime dateTime)
    {
        var minutes = dateTime.Minute;
        var normalizedMinutes = (minutes / SlotGranularityMinutes) * SlotGranularityMinutes;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
            dateTime.Hour, normalizedMinutes, 0, dateTime.Kind);
    }

    private async Task<string> GetMeetingDetailsAsync(int meetingId)
    {
        var meeting = await _context.ServiceMeets
            .AsNoTracking()
            .Include(m => m.ServiceOrder)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
            return "Cita no encontrada";

        return $"Cita #{meeting.Id} ({meeting.ScheduledStart:HH:mm} - {meeting.ScheduledEnd:HH:mm}) - {meeting.ServiceOrder?.Address ?? "Sin dirección"}";
    }
}
