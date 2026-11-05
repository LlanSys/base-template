using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Exceptions;
using BT.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BT.Infrastructure.Contracts.Implementations.Common;

internal sealed class CurrentTenantProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<OrgSettings> options) : ICurrentTenantProvider
{
    public Guid TenantId
    {
        get
        {
            var settings = options.Value;
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.HasClaim("platform_staff", "true") == true
                && httpContextAccessor.HttpContext?.Items[ICurrentTenantProvider.PlatformIdentityScopeKey] is Guid platformScope)
                return platformScope;

            if (TryGetTenantId(user?.FindFirstValue("tenant_id"), out var claimTenantId))
            {
                return claimTenantId;
            }

            // Set only by the server after validating protected OIDC state and the registered tenant.
            if (httpContextAccessor.HttpContext?.Items[ICurrentTenantProvider.ExternalSignInScopeKey] is Guid externalTenant)
                return externalTenant;

            var headerValue = httpContextAccessor.HttpContext?.Request.Headers[settings.HeaderName].FirstOrDefault();

            if (TryGetTenantId(headerValue, out var headerTenantId))
            {
                return headerTenantId;
            }

            if (settings.DefaultTenantId != Guid.Empty)
            {
                return settings.DefaultTenantId;
            }

            throw new TenantNotResolvedException();
        }
    }

    private static bool TryGetTenantId(string? value, out Guid tenantId)
    {
        return Guid.TryParse(value, out tenantId) && tenantId != Guid.Empty;
    }
}
