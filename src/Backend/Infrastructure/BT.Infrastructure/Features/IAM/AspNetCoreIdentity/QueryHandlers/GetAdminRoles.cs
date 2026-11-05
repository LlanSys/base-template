using BT.Application.Features.IAM.Users.Queries;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.HR.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using BT.Application.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetAdminRoles(RoleManager<AppRole> roleManager, IamDBContext context, HrDBContext hrContext,
    Domain.Shared.Contracts.Common.ICurrentActorProvider actorProvider,
    IEffectiveAccessService accessService)
    : IRequestHandler<GetAdminRolesQuery, AppResponse<IReadOnlyList<AdminRoleListResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<AdminRoleListResponse>>> Handle(GetAdminRolesQuery request, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(actorProvider.ActorId, cancellationToken).ConfigureAwait(false);
        var userIds = context.Users.IgnoreQueryFilters().Where(user => !user.IsDeleted
            && (actor.IsPlatformStaff || user.TenantId == actor.TenantId)).Select(user => user.Id);

        var userCounts = await context.UserRoles
            .AsNoTracking()
            .Where(assignment => userIds.Contains(assignment.UserId))
            .GroupBy(static userRole => userRole.RoleId)
            .Select(static group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(static item => item.RoleId, static item => item.Count, cancellationToken)
            .ConfigureAwait(false);

        var roles = await roleManager.Roles.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(role => !role.IsDeleted && (actor.IsPlatformStaff
                || (!role.IsPlatformRole && (role.IsDefault || role.TenantId == actor.TenantId))))
            .OrderBy(static role => role.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var departmentIds = roles
            .Where(static role => role.DepartmentId.HasValue)
            .Select(static role => role.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await hrContext.Departments
            .AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .ToDictionaryAsync(static department => department.Id, static department => department.Name, cancellationToken)
            .ConfigureAwait(false);

        var result = roles
            .Select(role => new AdminRoleListResponse(
                role.Id,
                role.Name ?? string.Empty,
                role.NormalizedName ?? string.Empty,
                role.DepartmentId,
                role.DepartmentId.HasValue ? departments.GetValueOrDefault(role.DepartmentId.Value, "Unknown")
                    : role.IsPlatformRole ? "Platform" : role.IsDefault ? "Default" : "Tenant",
                userCounts.GetValueOrDefault(role.Id), role.IsDefault, role.IsActive, role.IsPlatformRole, role.ModuleKey))
            .ToList();

        return AppResponses.Success("Roles loaded.", (IReadOnlyList<AdminRoleListResponse>)result);
    }
}
