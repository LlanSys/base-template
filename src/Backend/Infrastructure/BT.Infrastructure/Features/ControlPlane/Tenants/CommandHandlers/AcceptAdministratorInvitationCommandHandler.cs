using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class AcceptAdministratorInvitationCommandHandler(AdministratorInvitationAcceptance acceptance) : IRequestHandler<AcceptAdministratorInvitationCommand, AppResponse<SharedKernel.Features.IAM.Users.Dtos.LoginResponse>>
{
    public Task<AppResponse<SharedKernel.Features.IAM.Users.Dtos.LoginResponse>> Handle(AcceptAdministratorInvitationCommand request, CancellationToken cancellationToken) => acceptance.AcceptPasswordAsync(request.Request, cancellationToken);
}
