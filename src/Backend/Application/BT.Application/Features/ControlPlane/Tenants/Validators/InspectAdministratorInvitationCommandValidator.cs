using BT.Application.Features.ControlPlane.Tenants.Commands;

using FluentValidation;
namespace BT.Application.Features.ControlPlane.Tenants.Validators;

public sealed class InspectAdministratorInvitationCommandValidator : AbstractValidator<InspectAdministratorInvitationCommand>
{
    public InspectAdministratorInvitationCommandValidator()
    {
        RuleFor(command => command.Request.InvitationId).NotEmpty();
        RuleFor(command => command.Request.Token).NotEmpty().Length(64);
    }
}
