using System.Security.Claims;
using BT.Application.Features.IAM.Authorization;

namespace BT.Api.Middleware;

internal sealed class EffectiveAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IEffectiveAccessService accessService)
    {
        if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity
            && context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId)
        {
            var access = await accessService.GetAsync(userId, context.RequestAborted).ConfigureAwait(false);

            if (!access.IsPlatformStaff && (access.TenantId == Guid.Empty
                || !Guid.TryParse(identity.FindFirst("tenant_id")?.Value, out var tokenTenantId) || tokenTenantId != access.TenantId))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity());
                await next(context).ConfigureAwait(false);
                return;
            }

            foreach (var claim in identity.FindAll("permission").Concat(identity.FindAll("platform_staff")).ToArray())
                identity.RemoveClaim(claim);

            if (!identity.HasClaim("temp_auth", "true")) identity.AddClaims(access.Permissions.Select(key => new Claim("permission", key)));

            if (access.IsPlatformStaff) identity.AddClaim(new Claim("platform_staff", "true"));

            if (access.IsPlatformStaff)
            {
                context.Items[BT.Domain.Shared.Contracts.Common.ICurrentTenantProvider.PlatformIdentityScopeKey] = access.TenantId;

                foreach (var claim in identity.FindAll("tenant_id").ToArray()) identity.RemoveClaim(claim);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
