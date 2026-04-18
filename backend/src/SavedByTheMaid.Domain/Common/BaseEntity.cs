namespace SavedByTheMaid.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    // TODO: tighten setters with private set + Mark*() methods (deferred — breaks current call sites)
    public bool IsDeleted { get; set; } = false;
}

public abstract class BaseAuditableEntity : BaseEntity
{
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
