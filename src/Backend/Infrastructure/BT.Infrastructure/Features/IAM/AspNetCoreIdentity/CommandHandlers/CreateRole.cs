using BT.Application.Features.IAM.Users.Commands;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.IAM.DataContext;
using System.Data;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class CreateRole(RoleManager<AppRole> roleManager, ILogger<CreateRole> logger,
    IamAccessBoundary boundary,
    IamDBContext context)
    : IRequestHandler<CreateRoleCommand, AppResponse<AdminRoleListResponse>>
{
    public Task<AppResponse<AdminRoleListResponse>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
        => IdentityWriteScope.RunAsync(context,
            () => HandleCoreAsync(command, cancellationToken), cancellationToken, IsolationLevel.Serializable);

    private async Task<AppResponse<AdminRoleListResponse>> HandleCoreAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var roleName = command.Request.Name.Trim();

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return AppResponses.Failure<AdminRoleListResponse>("Role name is required.");
            }

            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                return AppResponses.Failure<AdminRoleListResponse>("A role with this name already exists.");
            }

            var actor = await boundary.GetActorAsync(command.CreatedBy, cancellationToken).ConfigureAwait(false);

            if (!actor.Permissions.Contains("roles.create")) return AppResponses.Failure<AdminRoleListResponse>(AppError.Forbidden("You cannot create roles in this scope."));

            if (command.Request.IsDefault && !actor.IsPlatformStaff)
                return AppResponses.Failure<AdminRoleListResponse>(AppError.Forbidden("Only the vendor can create default role definitions."));

            if (command.Request.ModuleKey is { } module && !ModuleCatalog.IsImplemented(module))
                return AppResponses.Failure<AdminRoleListResponse>("Select an implemented module.");
            var role = new AppRole
            {
                Name = roleName,
                DepartmentId = command.Request.DepartmentId,
                TenantId = command.Request.IsDefault ? Guid.Empty : actor.TenantId,
                IsDefault = command.Request.IsDefault,
                ModuleKey = command.Request.ModuleKey,
                IsPlatformRole = actor.IsPlatformStaff && command.Request.ModuleKey == null
            };

            var result = await roleManager.CreateAsync(role).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("Role created.", new AdminRoleListResponse(role.Id, role.Name ?? roleName, role.NormalizedName ?? roleName.ToUpperInvariant(), role.DepartmentId, role.DepartmentId.HasValue ? "Department-scoped" : "Global", 0))
                : AppResponses.Failure<AdminRoleListResponse>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleCreateError(logger, command.Request.Name, ex);
            throw;
        }
    }
}
