using BT.Application.Features.IAM.Permissions.Queries;
using BT.Domain.Features.IAM.Users.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using BT.Application.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetRolePermissions(RoleManager<AppRole> roleManager,
    IEffectiveAccessService accessService)
    : IRequestHandler<GetRolePermissionsQuery, AppResponse<RolePermissionsResponse>>
{
    public async Task<AppResponse<RolePermissionsResponse>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await roleManager.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(role => role.Id == request.RoleId && !role.IsDeleted, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return AppResponses.Failure<RolePermissionsResponse>("Role not found.");
        }
        var access = await accessService.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (!access.IsPlatformStaff && (role.IsPlatformRole || !role.IsDefault && role.TenantId != access.TenantId))
            return AppResponses.NotFound<RolePermissionsResponse>("Role not found.");

        var permissionKeys = (await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
            .Where(static claim => claim.Type == "permission")
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return AppResponses.Success(
            "Role permissions loaded.",
            new RolePermissionsResponse(role.Id, role.Name ?? string.Empty, permissionKeys));
    }
}
