using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public sealed record RevokeAdministratorInvitationCommand(Guid TenantId, Guid InvitationId) : IRequest<AppResponse<bool>>;
