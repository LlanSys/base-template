using Microsoft.EntityFrameworkCore;
using BT.Application.Features.IAM.Users.Commands;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.IAM.DataContext;
using System.Data;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateRole(RoleManager<AppRole> roleManager, ILogger<UpdateRole> logger,
    IamAccessBoundary boundary,
    IamDBContext context)
    : IRequestHandler<UpdateRoleCommand, AppResponse<AdminRoleListResponse>>
{
    public Task<AppResponse<AdminRoleListResponse>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
        => IdentityWriteScope.RunAsync(context,
            () => HandleCoreAsync(command, cancellationToken), cancellationToken, IsolationLevel.Serializable);

    private async Task<AppResponse<AdminRoleListResponse>> HandleCoreAsync(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(role => role.Id == command.RoleId && !role.IsDeleted, cancellationToken).ConfigureAwait(false);

            if (role is null)
            {
                return AppResponses.Failure<AdminRoleListResponse>("Role not found.");
            }

            var name = command.Request.Name.Trim();

            if (role.IsDefault && !string.Equals(name, role.Name, StringComparison.Ordinal))
                return AppResponses.Failure<AdminRoleListResponse>("Default role names are stable identifiers and cannot be renamed.");

            if (!await boundary.CanEditRoleAsync(command.UpdatedBy, role, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<AdminRoleListResponse>(AppError.Forbidden("Default roles are vendor-managed. You can edit only your tenant's custom roles."));

            if (string.IsNullOrWhiteSpace(name))
            {
                return AppResponses.Failure<AdminRoleListResponse>("Role name is required.");
            }

            role.Name = name;
            role.DepartmentId = command.Request.DepartmentId;

            if (role.IsDefault && !command.Request.IsActive)
                return AppResponses.Failure<AdminRoleListResponse>("Default roles cannot be disabled. Change module availability or user assignments instead.");
            role.IsActive = command.Request.IsActive;
            var result = await roleManager.UpdateAsync(role).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("Role updated.", new AdminRoleListResponse(role.Id, role.Name ?? name, role.NormalizedName ?? name.ToUpperInvariant(), role.DepartmentId, role.DepartmentId.HasValue ? "Department-scoped" : "Global", 0))
                : AppResponses.Failure<AdminRoleListResponse>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleUpdateError(logger, command.RoleId, ex);
            throw;
        }
    }
}
