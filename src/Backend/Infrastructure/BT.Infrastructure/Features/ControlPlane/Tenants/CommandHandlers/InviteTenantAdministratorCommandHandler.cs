using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.SharedKernel.Dtos.Common;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class InviteTenantAdministratorCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork controlPlane)
    : IRequestHandler<InviteTenantAdministratorCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(InviteTenantAdministratorCommand command, CancellationToken cancellationToken)
    {
        var tenant = await controlPlane.Tenants.FindByIdAsync(command.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null || tenant.Status == TenantStatus.Suspended) return AppResponses.NotFound<bool>("An available tenant is required.");
        var normalized = command.Request.Email.Trim().ToUpperInvariant();
        var item = await controlPlane.AdministratorInvitations.FindByCondition(item => item.TenantId == tenant.Id && item.NormalizedEmail == normalized)
            .AsTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (item is null)
        {
            item = new TenantAdministratorInvitation(timeProvider.GetUtcNow())
            {
                TenantId = tenant.Id,
                Email = command.Request.Email.Trim(),
                NormalizedEmail = normalized,
                FirstName = command.Request.FirstName.Trim(),
                LastName = command.Request.LastName.Trim(),
                CreatedBy = command.ActorId
            };

            await controlPlane.AdministratorInvitations.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        }
        else if (!item.Reissue(timeProvider.GetUtcNow())) return AppResponses.Failure<bool>("This invitation is already being completed or has been accepted. Review the administrator's account instead.");
        await controlPlane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("Administrator invitation queued. Delivery starts after activation.", true);
    }
}
