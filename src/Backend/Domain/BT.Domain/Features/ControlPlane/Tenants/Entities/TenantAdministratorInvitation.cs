using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
namespace BT.Domain.Features.ControlPlane.Tenants.Entities;

public sealed class TenantAdministratorInvitation : IAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public AdministratorInvitationStatus Status { get; private set; }
    public InvitationAcceptanceStatus AcceptanceStatus { get; private set; }
    public string? TokenHash { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? ClaimedUserId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }

    private TenantAdministratorInvitation() { }

    public TenantAdministratorInvitation(DateTimeOffset now)
    {
        NextAttemptAt = now;
        CreatedAt = now;
    }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? LastFailure { get; private set; }
    public Guid Revision { get; private set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool TryLease(DateTimeOffset now)
    {
        if (AcceptanceStatus != InvitationAcceptanceStatus.Pending || Status == AdministratorInvitationStatus.Delivered || NextAttemptAt > now || LeaseUntil > now) return false;
        LeaseUntil = now.AddMinutes(5);
        Attempts++;
        Revision = Guid.CreateVersion7();
        return true;
    }
    public void Complete(DateTimeOffset now)
    {
        Status = AdministratorInvitationStatus.Delivered;
        DeliveredAt = now;
        LeaseUntil = null;
        LastFailure = null;
        Revision = Guid.CreateVersion7();
    }
    public void Fail(DateTimeOffset now)
    {
        Status = AdministratorInvitationStatus.Failed;
        LastFailure = "Administrator setup or invitation delivery failed. Retry is scheduled.";
        LeaseUntil = null;
        NextAttemptAt = now.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(Attempts, 6))));
        Revision = Guid.CreateVersion7();
    }
    public bool Retry(DateTimeOffset now)
    {
        if (LeaseUntil > now || AcceptanceStatus != InvitationAcceptanceStatus.Pending) return false;
        TokenHash = null;
        ExpiresAt = null;
        Status = AdministratorInvitationStatus.Pending;
        NextAttemptAt = now;
        LastFailure = null;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public void Issue(string hash, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        if (AcceptanceStatus != InvitationAcceptanceStatus.Pending) throw new InvalidOperationException("This invitation cannot be issued.");
        TokenHash = hash;
        ExpiresAt = expiresAt;
        Revision = Guid.CreateVersion7();
    }

    public bool Reissue(DateTimeOffset now)
    {
        if (AcceptanceStatus is InvitationAcceptanceStatus.Accepted or InvitationAcceptanceStatus.SettingUp || LeaseUntil > now) return false;
        AcceptanceStatus = InvitationAcceptanceStatus.Pending;
        ClaimedUserId = null;
        RevokedAt = null;
        return Retry(now);
    }

    public bool Claim(string userId, DateTimeOffset now)
    {
        if (AcceptanceStatus != InvitationAcceptanceStatus.Pending || TokenHash is null || ExpiresAt <= now) return false;
        ClaimedUserId = userId;
        AcceptanceStatus = InvitationAcceptanceStatus.SettingUp;
        TokenHash = null;
        LeaseUntil = null;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public bool Accept(string userId, DateTimeOffset now)
    {
        if (AcceptanceStatus != InvitationAcceptanceStatus.SettingUp || !string.Equals(ClaimedUserId, userId, StringComparison.Ordinal)) return false;
        AcceptanceStatus = InvitationAcceptanceStatus.Accepted;
        AcceptedAt = now;
        Revision = Guid.CreateVersion7();
        return true;
    }

    public bool Revoke(DateTimeOffset now)
    {
        if (AcceptanceStatus is InvitationAcceptanceStatus.Accepted or InvitationAcceptanceStatus.Revoked) return false;
        AcceptanceStatus = InvitationAcceptanceStatus.Revoked;
        TokenHash = null;
        RevokedAt = now;
        LeaseUntil = null;
        Revision = Guid.CreateVersion7();
        return true;
    }
}
