using BT.Application.Features.ControlPlane.Tenants.Commands;

using FluentValidation;
namespace BT.Application.Features.ControlPlane.Tenants.Validators;

public sealed class RevokeAdministratorInvitationCommandValidator : AbstractValidator<RevokeAdministratorInvitationCommand>
{
    public RevokeAdministratorInvitationCommandValidator() { RuleFor(command => command.TenantId).NotEmpty(); RuleFor(command => command.InvitationId).NotEmpty(); }
}
