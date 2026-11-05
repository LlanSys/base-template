namespace BT.Application.Features.IAM.Authorization;

public sealed record EffectiveAccess(Guid TenantId, bool IsPlatformStaff, IReadOnlySet<string> Permissions, IReadOnlySet<string> Modules)
{
    public static EffectiveAccess Denied { get; } = new(Guid.Empty, false,
        new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
