using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

/// <summary>
/// Servicio para registrar cambios de estado en el historial de auditoría
/// </summary>
public interface IStatusHistoryService
{
    /// <summary>
    /// Registra un cambio de estado de una orden
    /// </summary>
    Task RecordOrderStatusChangeAsync(
        int serviceOrderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null);

    /// <summary>
    /// Registra un cambio de estado de una cita
    /// </summary>
    Task RecordMeetStatusChangeAsync(
        int serviceMeetId,
        MeetStatus? fromStatus,
        MeetStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null);

    /// <summary>
    /// Obtiene el historial de estados de una orden
    /// </summary>
    Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int serviceOrderId);

    /// <summary>
    /// Obtiene el historial de estados de una cita
    /// </summary>
    Task<List<MeetStatusHistory>> GetMeetStatusHistoryAsync(int serviceMeetId);
}

/// <summary>
/// Implementación del servicio de historial de estados
/// </summary>
public class StatusHistoryService : IStatusHistoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatusHistoryService> _logger;

    public StatusHistoryService(
        ApplicationDbContext context,
        ILogger<StatusHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordOrderStatusChangeAsync(
        int serviceOrderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null)
    {
        try
        {
            var history = new OrderStatusHistory
            {
                ServiceOrderId = serviceOrderId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedById = changedById,
                ChangedAt = DateTime.UtcNow,
                ReasonCode = reasonCode,
                Notes = notes
            };

            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Order status change recorded: OrderId={OrderId}, From={From}, To={To}, By={By}, Reason={Reason}",
                serviceOrderId, fromStatus?.ToString() ?? "null", toStatus, changedById ?? "SYSTEM", reasonCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording order status change for OrderId={OrderId}", serviceOrderId);
            // No lanzar excepción para no interrumpir el flujo principal
        }
    }

    public async Task RecordMeetStatusChangeAsync(
        int serviceMeetId,
        MeetStatus? fromStatus,
        MeetStatus toStatus,
        string? changedById = null,
        string? reasonCode = null,
        string? notes = null)
    {
        try
        {
            var history = new MeetStatusHistory
            {
                ServiceMeetId = serviceMeetId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedById = changedById,
                ChangedAt = DateTime.UtcNow,
                ReasonCode = reasonCode,
                Notes = notes
            };

            _context.MeetStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Meet status change recorded: MeetId={MeetId}, From={From}, To={To}, By={By}, Reason={Reason}",
                serviceMeetId, fromStatus?.ToString() ?? "null", toStatus, changedById ?? "SYSTEM", reasonCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording meet status change for MeetId={MeetId}", serviceMeetId);
            // No lanzar excepción para no interrumpir el flujo principal
        }
    }

    public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int serviceOrderId)
    {
        return await _context.OrderStatusHistories
            .Where(h => h.ServiceOrderId == serviceOrderId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<List<MeetStatusHistory>> GetMeetStatusHistoryAsync(int serviceMeetId)
    {
        return await _context.MeetStatusHistories
            .Where(h => h.ServiceMeetId == serviceMeetId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }
}
