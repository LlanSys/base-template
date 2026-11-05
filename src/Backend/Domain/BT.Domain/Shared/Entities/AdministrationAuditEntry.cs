namespace BT.Domain.Shared.Entities;

public sealed class AdministrationAuditEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? TenantId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string ActorId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }
    public required string ChangedFields { get; set; }
}
