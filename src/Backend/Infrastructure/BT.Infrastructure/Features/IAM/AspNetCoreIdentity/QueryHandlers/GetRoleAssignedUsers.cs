using BT.Application.Features.IAM.Authorization;
using BT.Application.Features.IAM.Users.Queries;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetRoleAssignedUsers(IamDBContext context, IEffectiveAccessService accessService)
    : IRequestHandler<GetRoleAssignedUsersQuery, AppResponse<AssignedUsersPage>>
{
    public async Task<AppResponse<AssignedUsersPage>> Handle(GetRoleAssignedUsersQuery request, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(request.ActorId, cancellationToken).ConfigureAwait(false);
        var role = await context.Roles.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(role => role.Id == request.RoleId && !role.IsDeleted
            && (actor.IsPlatformStaff || !role.IsPlatformRole && (role.IsDefault || role.TenantId == actor.TenantId)), cancellationToken).ConfigureAwait(false);

        if (role is null) return AppResponses.NotFound<AssignedUsersPage>("Role not found.");
        var users = from assignment in context.UserRoles.AsNoTracking()
                    join user in context.Users.IgnoreQueryFilters().AsNoTracking() on assignment.UserId equals user.Id
                    where assignment.RoleId == role.Id && !user.IsDeleted && user.IsPlatformStaff == role.IsPlatformRole
                        && (actor.IsPlatformStaff || user.TenantId == actor.TenantId)
                        && (request.Cursor == null || user.Id.CompareTo(request.Cursor) > 0)
                    orderby user.Id
                    select new AssignedUserResponse(user.Id, user.FirstName + " " + user.LastName, user.Email ?? string.Empty, user.IsActive);

        var page = await users.Take(51).ToListAsync(cancellationToken).ConfigureAwait(false);
        var next = page.Count > 50 ? page[49].Id : null;
        return AppResponses.Success(new AssignedUsersPage(page.Take(50).ToArray(), next));
    }
}
