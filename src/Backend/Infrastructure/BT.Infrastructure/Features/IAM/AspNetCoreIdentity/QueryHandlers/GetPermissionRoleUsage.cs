using BT.Application.Features.IAM.Authorization;
using BT.Application.Features.IAM.Permissions.Queries;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetPermissionRoleUsage(IamDBContext iam, ControlPlaneDBContext controlPlane, IEffectiveAccessService accessService)
    : IRequestHandler<GetPermissionRoleUsageQuery, AppResponse<IReadOnlyList<RolePermissionUsageResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<RolePermissionUsageResponse>>> Handle(GetPermissionRoleUsageQuery request, CancellationToken cancellationToken)
    {
        var access = await accessService.GetAsync(request.ActorId, cancellationToken).ConfigureAwait(false);

        if (!access.IsPlatformStaff && !access.Permissions.Contains(request.Key)) return AppResponses.NotFound<IReadOnlyList<RolePermissionUsageResponse>>("Permission not found.");
        var roles = await iam.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(role => !role.IsDeleted && (access.IsPlatformStaff || !role.IsPlatformRole && (role.IsDefault || role.TenantId == access.TenantId)))
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.TenantId,
                role.IsDefault,
                role.IsPlatformRole,
                role.DepartmentId,
                Granted = iam.RoleClaims.Any(claim => claim.RoleId == role.Id && claim.ClaimType == "permission" && claim.ClaimValue == request.Key)
            })
            .OrderBy(role => role.Name).ToListAsync(cancellationToken).ConfigureAwait(false);

        var tenantIds = roles.Where(role => !role.IsDefault && !role.IsPlatformRole).Select(role => role.TenantId).Distinct().ToArray();
        var tenants = await controlPlane.Tenants.AsNoTracking().Where(tenant => tenantIds.Contains(tenant.Id))
            .ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.DisplayName, cancellationToken).ConfigureAwait(false);

        return AppResponses.Success<IReadOnlyList<RolePermissionUsageResponse>>(roles.Select(role =>
            new RolePermissionUsageResponse(role.Id, role.Name ?? string.Empty, role.IsPlatformRole ? "Platform"
                : role.IsDefault ? "Default" : tenants.GetValueOrDefault(role.TenantId, "Tenant"),
                role.IsDefault, role.IsPlatformRole, role.Granted, role.DepartmentId)).ToArray());
    }
}
