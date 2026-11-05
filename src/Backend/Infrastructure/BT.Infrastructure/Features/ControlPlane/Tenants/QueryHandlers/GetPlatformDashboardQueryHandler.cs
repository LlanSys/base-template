using BT.Application.Features.ControlPlane.Tenants.Queries;
using BT.Application.Features.IAM.Authorization;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.QueryHandlers;

internal sealed class GetPlatformDashboardQueryHandler(ControlPlaneDBContext context, IEffectiveAccessService accessService)
    : IRequestHandler<GetPlatformDashboardQuery, AppResponse<PlatformDashboardResponse>>
{
    public async Task<AppResponse<PlatformDashboardResponse>> Handle(GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var access = await accessService.GetAsync(request.ActorId, cancellationToken).ConfigureAwait(false);

        if (!access.IsPlatformStaff || !access.Permissions.Contains("controlplane.view_tenants")) return AppResponses.Failure<PlatformDashboardResponse>(AppError.Forbidden("Tenant viewing permission is required."));
        var counts = await context.Tenants.AsNoTracking().GroupBy(tenant => tenant.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken).ConfigureAwait(false);

        int? pending = null, failed = null, activeStamps = null, disabledStamps = null;

        if (access.Permissions.Contains("controlplane.manage_tenant_administrators"))
        {
            var invitations = await context.AdministratorInvitations.Where(item => !item.Tenant.IsDeleted && item.AcceptanceStatus == InvitationAcceptanceStatus.Pending && item.Status != AdministratorInvitationStatus.Delivered)
                .GroupBy(item => item.Status).Select(group => new { Status = group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken).ConfigureAwait(false);

            pending = invitations.GetValueOrDefault(AdministratorInvitationStatus.Pending);
            failed = invitations.GetValueOrDefault(AdministratorInvitationStatus.Failed);
        }

        if (access.Permissions.Contains("controlplane.view_stamps"))
        {
            var stamps = await context.DeploymentStamps.GroupBy(stamp => stamp.IsActive).Select(group => new { Active = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Active, item => item.Count, cancellationToken).ConfigureAwait(false);

            activeStamps = stamps.GetValueOrDefault(true); disabledStamps = stamps.GetValueOrDefault(false);
        }
        return AppResponses.Success(new PlatformDashboardResponse(counts.Values.Sum(), counts.GetValueOrDefault(TenantStatus.Active), counts.GetValueOrDefault(TenantStatus.Suspended),
            counts.GetValueOrDefault(TenantStatus.PendingKYC), counts.GetValueOrDefault(TenantStatus.PendingProvisioning) + counts.GetValueOrDefault(TenantStatus.Provisioning),
            counts.GetValueOrDefault(TenantStatus.ProvisioningFailed), pending, failed, activeStamps, disabledStamps));
    }
}
