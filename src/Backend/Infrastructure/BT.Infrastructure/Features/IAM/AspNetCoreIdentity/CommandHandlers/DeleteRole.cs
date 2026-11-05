using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DeleteRole(TimeProvider timeProvider, RoleManager<AppRole> roleManager, IamDBContext context, ILogger<DeleteRole> logger,
    IamAccessBoundary boundary)
    : IRequestHandler<DeleteRoleCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(role => role.Id == command.RoleId && !role.IsDeleted, cancellationToken).ConfigureAwait(false);

            if (role is null)
            {
                return AppResponses.Failure<bool>("Role not found.");
            }

            if (role.IsDefault || !await boundary.CanEditRoleAsync(command.DeletedBy, role, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<bool>(AppError.Forbidden("Default roles cannot be deleted. You can delete only roles in your authorized scope."));

            var assignedUserCount = await context.UserRoles
                .AsNoTracking()
                .CountAsync(userRole => userRole.RoleId == role.Id, cancellationToken)
                .ConfigureAwait(false);

            if (assignedUserCount > 0)
            {
                return AppResponses.Failure<bool>($"Role cannot be deleted because it is assigned to {assignedUserCount} user(s). Remove assignments first.");
            }

            var permissionCount = await context.RoleClaims
                .AsNoTracking()
                .CountAsync(roleClaim => roleClaim.RoleId == role.Id, cancellationToken)
                .ConfigureAwait(false);

            if (permissionCount > 0)
            {
                return AppResponses.Failure<bool>($"Role cannot be deleted because it has {permissionCount} permission claim(s). Remove permissions first.");
            }

            role.MarkAsDeleted(command.DeletedBy, timeProvider.GetUtcNow());
            var result = await roleManager.UpdateAsync(role).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("Role deleted.", true)
                : AppResponses.Failure<bool>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleDeleteError(logger, command.RoleId, ex);
            throw;
        }
    }
}
