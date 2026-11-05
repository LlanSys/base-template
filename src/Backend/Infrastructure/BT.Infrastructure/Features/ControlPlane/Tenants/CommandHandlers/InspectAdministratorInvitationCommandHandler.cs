using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class InspectAdministratorInvitationCommandHandler(AdministratorInvitationAcceptance acceptance) : IRequestHandler<InspectAdministratorInvitationCommand, AppResponse<AdministratorInvitationPreview>>
{
    public Task<AppResponse<AdministratorInvitationPreview>> Handle(InspectAdministratorInvitationCommand request, CancellationToken cancellationToken) => acceptance.InspectAsync(request.Request, cancellationToken);
}
