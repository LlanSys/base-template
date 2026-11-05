using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record AcceptAdministratorInvitationCommand(AcceptAdministratorInvitationRequest Request) : IRequest<AppResponse<SharedKernel.Features.IAM.Users.Dtos.LoginResponse>>;
