using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record InspectAdministratorInvitationCommand(InspectAdministratorInvitationRequest Request) : IRequest<AppResponse<AdministratorInvitationPreview>>;
