using BT.Application.Features.ControlPlane.Tenants.Queries;
using BT.Application.Features.IAM.Authorization;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Features.IAM.Users.Dtos;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.QueryHandlers;

internal sealed class GetTenantAdministrationQueryHandler(TimeProvider timeProvider, ControlPlaneDBContext control, IamDBContext iam, IEffectiveAccessService accessService)
    : IRequestHandler<GetTenantAdministrationQuery, AppResponse<TenantAdministrationResponse>>
{
    public async Task<AppResponse<TenantAdministrationResponse>> Handle(GetTenantAdministrationQuery request, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(request.ActorId, cancellationToken).ConfigureAwait(false);

        if (!actor.IsPlatformStaff || !actor.Permissions.Contains("controlplane.view_tenants")) return AppResponses.Failure<TenantAdministrationResponse>(AppError.Forbidden("Tenant viewing permission is required."));

        if (!await control.Tenants.AnyAsync(tenant => tenant.Id == request.TenantId, cancellationToken).ConfigureAwait(false)) return AppResponses.NotFound<TenantAdministrationResponse>("Tenant not found.");
        var now = timeProvider.GetUtcNow();
        var modules = await (from grant in control.TenantModules.AsNoTracking()
                             join module in control.Modules.AsNoTracking() on grant.ModuleKey equals module.Key
                             where grant.TenantId == request.TenantId
                             select new TenantModuleStatusResponse(grant.ModuleKey, grant.IsActive, grant.IsActive && module.IsActive && (grant.ExpiresAt == null || grant.ExpiresAt > now), grant.ExpiresAt))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var invitations = new List<TenantInvitationResponse>();
        var administrators = new List<AssignedUserResponse>();

        if (actor.Permissions.Contains("controlplane.manage_tenant_administrators"))
        {
            invitations = await control.AdministratorInvitations.AsNoTracking().Where(item => item.TenantId == request.TenantId)
                .OrderByDescending(item => item.CreatedAt).Select(item => new TenantInvitationResponse(item.Id, item.Email, item.FirstName + " " + item.LastName,
                    item.Status.ToString(), item.Attempts, item.Status == BT.Domain.Features.ControlPlane.Tenants.Enums.AdministratorInvitationStatus.Delivered ? null : item.NextAttemptAt, item.LastFailure,
                    item.AcceptanceStatus.ToString(), item.ExpiresAt, item.AcceptedAt))
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var adminRoles = iam.Roles.IgnoreQueryFilters().Where(role => !role.IsDeleted && role.IsActive && role.IsDefault && role.DefinitionKey == "tenant-administrator").Select(role => role.Id);
            administrators = await (from assignment in iam.UserRoles.AsNoTracking()
                                    join user in iam.Users.IgnoreQueryFilters().AsNoTracking() on assignment.UserId equals user.Id
                                    where adminRoles.Contains(assignment.RoleId) && user.TenantId == request.TenantId && !user.IsDeleted && !user.IsPlatformStaff
                                    select new AssignedUserResponse(user.Id, user.FirstName + " " + user.LastName, user.Email ?? string.Empty, user.IsActive))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        return AppResponses.Success(new TenantAdministrationResponse(modules, invitations, administrators));
    }
}
