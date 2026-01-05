using System;

namespace JempSoft.Core.Exceptions
{
    /// <summary>
    /// Base exception for domain-specific errors
    /// </summary>
    public abstract class DomainException : Exception
    {
        public string Code { get; }

        protected DomainException(string code, string message) : base(message)
        {
            Code = code;
        }

        protected DomainException(string code, string message, Exception innerException) 
            : base(message, innerException)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Thrown when an entity is not found
    /// </summary>
    public class EntityNotFoundException : DomainException
    {
        public string EntityType { get; }
        public object? EntityId { get; }

        public EntityNotFoundException(string entityType, object? entityId = null)
            : base("ENTITY_NOT_FOUND", $"Entity '{entityType}'{(entityId != null ? $" with id '{entityId}'" : "")} was not found.")
        {
            EntityType = entityType;
            EntityId = entityId;
        }
    }

    /// <summary>
    /// Thrown when a business rule is violated
    /// </summary>
    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string code, string message) 
            : base(code, message)
        {
        }
    }

    /// <summary>
    /// Thrown when validation fails
    /// </summary>
    public class ValidationException : DomainException
    {
        public string[] ValidationErrors { get; }

        public ValidationException(string message) 
            : base("VALIDATION_ERROR", message)
        {
            ValidationErrors = new[] { message };
        }

        public ValidationException(string[] errors) 
            : base("VALIDATION_ERROR", string.Join("; ", errors))
        {
            ValidationErrors = errors;
        }
    }

    /// <summary>
    /// Thrown when there's a concurrency conflict
    /// </summary>
    public class ConcurrencyException : DomainException
    {
        public ConcurrencyException(string entityType)
            : base("CONCURRENCY_ERROR", $"A concurrency conflict occurred while updating '{entityType}'. The entity may have been modified by another user.")
        {
        }
    }

    /// <summary>
    /// Thrown when no maids are available for booking
    /// </summary>
    public class NoAvailableMaidsException : BusinessRuleException
    {
        public DateTime RequestedDate { get; }

        public NoAvailableMaidsException(DateTime date)
            : base("NO_AVAILABLE_MAIDS", $"No maids are available for {date:yyyy-MM-dd}.")
        {
            RequestedDate = date;
        }
    }

    /// <summary>
    /// Thrown when a service order operation fails
    /// </summary>
    public class ServiceOrderException : BusinessRuleException
    {
        public long? OrderId { get; }

        public ServiceOrderException(string message, long? orderId = null)
            : base("SERVICE_ORDER_ERROR", message)
        {
            OrderId = orderId;
        }
    }
}
