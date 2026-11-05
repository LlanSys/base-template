namespace BT.Domain.Shared.Contracts.Common;

public interface ICurrentTenantProvider
{
    const string ExternalSignInScopeKey = "BT.ExternalSignInTenant";
    // Server-owned storage scope for a platform identity; never accepted from request headers.
    public const string PlatformIdentityScopeKey = "BT.PlatformIdentityScope";
    Guid TenantId { get; }
}
