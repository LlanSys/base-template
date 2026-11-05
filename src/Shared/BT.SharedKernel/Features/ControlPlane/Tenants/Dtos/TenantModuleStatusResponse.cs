namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record TenantModuleStatusResponse(string Key, bool Assigned, bool Available, DateTimeOffset? ExpiresAt);
