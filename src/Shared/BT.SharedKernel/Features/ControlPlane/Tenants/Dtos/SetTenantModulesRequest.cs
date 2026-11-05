namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record SetTenantModulesRequest(IReadOnlyList<string> ModuleKeys);
