using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class CompleteAdministratorOnboardingCommandHandler(AdministratorInvitationAcceptance acceptance) : IRequestHandler<CompleteAdministratorOnboardingCommand, AppResponse<bool>>
{
    public Task<AppResponse<bool>> Handle(CompleteAdministratorOnboardingCommand request, CancellationToken cancellationToken) => acceptance.CompleteAsync(request.UserId, cancellationToken, request.MfaVerified);
}
