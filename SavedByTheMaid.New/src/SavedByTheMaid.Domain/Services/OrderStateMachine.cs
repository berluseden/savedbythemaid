using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Services;

/// <summary>
/// Máquina de estados para OrderStatus.
/// Define las transiciones válidas entre estados de una orden.
/// </summary>
public static class OrderStateMachine
{
    /// <summary>
    /// Diccionario de transiciones válidas para cada estado de orden.
    /// Los estados terminales (Completed, Cancelled, NoShow) no tienen transiciones.
    /// </summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        // PendingReview: Orden nueva pendiente de revisión por admin
        [OrderStatus.PendingReview] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        
        // Draft (deprecated): Se comporta igual que PendingReview para compatibilidad
        #pragma warning disable CS0618 // Type or member is obsolete
        [OrderStatus.Draft] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        #pragma warning restore CS0618
        
        // Confirmed: Orden confirmada, puede iniciar, cancelarse o marcar NoShow
        [OrderStatus.Confirmed] = new[] { OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow },
        
        // InProgress: Servicio en ejecución, puede completarse o cancelarse
        [OrderStatus.InProgress] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
        
        // Estados terminales - no tienen transiciones permitidas
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.NoShow] = Array.Empty<OrderStatus>()
    };

    /// <summary>
    /// Estados considerados como finales (no permiten más transiciones)
    /// </summary>
    public static readonly OrderStatus[] FinalStates = new[]
    {
        OrderStatus.Completed,
        OrderStatus.Cancelled,
        OrderStatus.NoShow
    };

    /// <summary>
    /// Estados considerados como activos (orden en proceso)
    /// </summary>
    public static readonly OrderStatus[] ActiveStates = new[]
    {
        OrderStatus.PendingReview,
        OrderStatus.Confirmed,
        OrderStatus.InProgress
    };

    /// <summary>
    /// Verifica si una transición de estado es válida.
    /// </summary>
    /// <param name="from">Estado actual de la orden</param>
    /// <param name="to">Estado destino propuesto</param>
    /// <returns>True si la transición es válida, false en caso contrario</returns>
    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }

    /// <summary>
    /// Obtiene todos los estados a los que se puede transicionar desde el estado actual.
    /// </summary>
    /// <param name="current">Estado actual de la orden</param>
    /// <returns>Array de estados permitidos, o vacío si es estado terminal</returns>
    public static OrderStatus[] GetAllowedTransitions(OrderStatus current)
    {
        return ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : Array.Empty<OrderStatus>();
    }

    /// <summary>
    /// Verifica si un estado es terminal (no permite más transiciones).
    /// </summary>
    /// <param name="status">Estado a verificar</param>
    /// <returns>True si es estado terminal</returns>
    public static bool IsFinalState(OrderStatus status)
    {
        return FinalStates.Contains(status);
    }

    /// <summary>
    /// Verifica si un estado es activo (orden en proceso, no terminal).
    /// </summary>
    /// <param name="status">Estado a verificar</param>
    /// <returns>True si es estado activo</returns>
    public static bool IsActiveState(OrderStatus status)
    {
        return ActiveStates.Contains(status);
    }

    /// <summary>
    /// Valida una transición y lanza excepción si no es válida.
    /// </summary>
    /// <param name="from">Estado actual</param>
    /// <param name="to">Estado destino</param>
    /// <exception cref="InvalidOperationException">Si la transición no es válida</exception>
    public static void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Transición de estado inválida: {from} -> {to}. " +
                $"Transiciones permitidas desde {from}: [{string.Join(", ", GetAllowedTransitions(from))}]");
        }
    }
}

/// <summary>
/// Máquina de estados para MeetStatus.
/// Define las transiciones válidas entre estados de una cita de servicio.
/// </summary>
public static class MeetStateMachine
{
    /// <summary>
    /// Diccionario de transiciones válidas para cada estado de cita.
    /// </summary>
    private static readonly Dictionary<MeetStatus, MeetStatus[]> ValidTransitions = new()
    {
        // Scheduled: Cita programada, puede asignarse, cancelarse o reprogramarse
        [MeetStatus.Scheduled] = new[] { MeetStatus.Assigned, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        
        // Assigned: Empleada asignada, puede iniciar camino, cancelarse o reprogramarse
        [MeetStatus.Assigned] = new[] { MeetStatus.OnTheWay, MeetStatus.Cancelled, MeetStatus.Rescheduled },
        
        // OnTheWay: Empleada en camino, puede iniciar servicio o cancelarse
        [MeetStatus.OnTheWay] = new[] { MeetStatus.InProgress, MeetStatus.Cancelled, MeetStatus.NoShow },
        
        // InProgress: Servicio en ejecución, puede completarse o cancelarse
        [MeetStatus.InProgress] = new[] { MeetStatus.Completed, MeetStatus.Cancelled },
        
        // Rescheduled: Reprogramada, vuelve a estado Scheduled cuando se confirma nueva fecha
        [MeetStatus.Rescheduled] = new[] { MeetStatus.Scheduled, MeetStatus.Assigned, MeetStatus.Cancelled },
        
        // Estados terminales
        [MeetStatus.Completed] = Array.Empty<MeetStatus>(),
        [MeetStatus.Cancelled] = Array.Empty<MeetStatus>(),
        [MeetStatus.NoShow] = Array.Empty<MeetStatus>()
    };

    /// <summary>
    /// Estados considerados como finales
    /// </summary>
    public static readonly MeetStatus[] FinalStates = new[]
    {
        MeetStatus.Completed,
        MeetStatus.Cancelled,
        MeetStatus.NoShow
    };

    /// <summary>
    /// Estados considerados como activos
    /// </summary>
    public static readonly MeetStatus[] ActiveStates = new[]
    {
        MeetStatus.Scheduled,
        MeetStatus.Assigned,
        MeetStatus.OnTheWay,
        MeetStatus.InProgress,
        MeetStatus.Rescheduled
    };

    /// <summary>
    /// Verifica si una transición de estado es válida.
    /// </summary>
    public static bool CanTransition(MeetStatus from, MeetStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }

    /// <summary>
    /// Obtiene todos los estados a los que se puede transicionar desde el estado actual.
    /// </summary>
    public static MeetStatus[] GetAllowedTransitions(MeetStatus current)
    {
        return ValidTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : Array.Empty<MeetStatus>();
    }

    /// <summary>
    /// Verifica si un estado es terminal.
    /// </summary>
    public static bool IsFinalState(MeetStatus status)
    {
        return FinalStates.Contains(status);
    }

    /// <summary>
    /// Verifica si un estado es activo.
    /// </summary>
    public static bool IsActiveState(MeetStatus status)
    {
        return ActiveStates.Contains(status);
    }

    /// <summary>
    /// Valida una transición y lanza excepción si no es válida.
    /// </summary>
    public static void ValidateTransition(MeetStatus from, MeetStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Transición de estado inválida: {from} -> {to}. " +
                $"Transiciones permitidas desde {from}: [{string.Join(", ", GetAllowedTransitions(from))}]");
        }
    }
}
