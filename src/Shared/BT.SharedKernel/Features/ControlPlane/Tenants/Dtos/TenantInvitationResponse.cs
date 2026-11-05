namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record TenantInvitationResponse(Guid Id, string Email, string Name, string Status, int Attempts, DateTimeOffset? NextAttemptAt, string? Message,
    string AcceptanceStatus = "Pending", DateTimeOffset? ExpiresAt = null, DateTimeOffset? AcceptedAt = null);
