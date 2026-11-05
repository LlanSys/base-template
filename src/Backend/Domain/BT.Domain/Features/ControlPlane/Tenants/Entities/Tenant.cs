using BT.Domain.Shared.Contracts.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using BT.Domain.Features.ControlPlane.Tenants.Enums;

namespace BT.Domain.Features.ControlPlane.Tenants.Entities;

public class Tenant : IAuditable, IHasDomainEvents, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public Guid Revision { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);
        IsDeleted = true;
        Status = TenantStatus.Suspended;
        DeletedBy = deletedBy;
        DeletedAt = now;
    }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Identifier { get; set; } // URL-safe slug
    public required string DisplayName { get; set; }
    public required string HostName { get; set; }
    public string? ContactEmail { get; set; }
    public int MaxUsers { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.PendingKYC;

    public void ApproveKYC(DateTimeOffset now, string actorId)
    {
        if (Status != TenantStatus.PendingKYC)
            throw new InvalidOperationException($"Cannot approve KYC from status {Status}");

        Status = TenantStatus.PendingProvisioning;
        UpdatedBy = actorId;
        UpdatedAt = now;
    }

    public void MarkAsProvisioning()
    {
        if (Status != TenantStatus.PendingProvisioning)
            throw new InvalidOperationException($"Cannot start provisioning from status {Status}");

        Status = TenantStatus.Provisioning;
    }

    public void MarkAsActive()
    {
        Status = TenantStatus.Active;
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
    }

    public void MarkAsProvisioningFailed()
    {
        Status = TenantStatus.ProvisioningFailed;
    }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public Guid DeploymentStampId { get; set; }
    public DeploymentStamp? DeploymentStamp { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }

    public ICollection<TenantAdministratorInvitation> AdministratorInvitations { get; set; } = [];
    public ICollection<TenantModule> Modules { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    [Timestamp]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Required by Entity Framework Core for optimistic concurrency control")]
    public byte[] RowVersion { get; set; } = [];
}
