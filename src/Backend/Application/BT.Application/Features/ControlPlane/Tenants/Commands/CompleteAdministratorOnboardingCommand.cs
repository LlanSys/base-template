using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record CompleteAdministratorOnboardingCommand(string UserId, bool MfaVerified) : IRequest<AppResponse<bool>>;
