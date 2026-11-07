using BT.Application.Features.IAM.Users.Queries;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetUserRoles(
    UserManager<AppUser> userManager,
    IamAccessBoundary boundary,
    IamDBContext context)
    : IRequestHandler<GetUserRolesQuery, AppResponse<UserRolesResponse>>
{
    public async Task<AppResponse<UserRolesResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);

        if (user is null)
        {
            return AppResponses.Failure<UserRolesResponse>("User not found.");
        }

        if (!await boundary.CanAccessUserAsync(request.RequestedBy, user, cancellationToken).ConfigureAwait(false))
        {
            return AppResponses.Failure<UserRolesResponse>(AppError.Forbidden("User is outside your administrative scope."));
        }

        var actor = await boundary.GetActorAsync(request.RequestedBy, cancellationToken).ConfigureAwait(false);
        var assignments = context.UserRoles.Where(assignment => assignment.UserId == user.Id);
        var catalog = await context.Roles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(role => role.Name != null && (!role.IsDeleted && (role.IsDefault || role.TenantId == user.TenantId)
                || assignments.Any(assignment => assignment.RoleId == role.Id)))
            .Select(role => new
            {
                role.Id,
                Name = role.Name!,
                role.ModuleKey,
                role.IsActive,
                InScope = !role.IsDeleted && role.IsPlatformRole == user.IsPlatformStaff
                        && (role.IsDefault || role.TenantId == user.TenantId),
                IsAssigned = assignments.Any(assignment => assignment.RoleId == role.Id)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var assignedRoles = catalog.Where(static role => role.IsAssigned)
            .Select(static role => role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var options = catalog.GroupBy(static role => role.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var role = group.OrderByDescending(static role => role.IsAssigned)
                    .ThenByDescending(static role => role.InScope)
                    .First();

                var canAssign = role.InScope && role.IsActive
                    && group.Count(static candidate => candidate.InScope && candidate.IsActive) == 1
                    && (actor.IsPlatformStaff || role.ModuleKey is null || actor.Modules.Contains(role.ModuleKey));

                return new UserRoleOption(role.Id, role.Name, role.ModuleKey, role.IsActive, canAssign);
            })
            .OrderBy(static role => role.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return AppResponses.Success(
            "User roles loaded.",
            new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, assignedRoles, options));
    }
}
