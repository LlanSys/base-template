using BT.Application.Features.ControlPlane.Tenants.Commands;

using FluentValidation;
namespace BT.Application.Features.ControlPlane.Tenants.Validators;

public sealed class AcceptAdministratorInvitationCommandValidator : AbstractValidator<AcceptAdministratorInvitationCommand>
{
    public AcceptAdministratorInvitationCommandValidator()
    {
        RuleFor(command => command.Request.InvitationId).NotEmpty();
        RuleFor(command => command.Request.Token).NotEmpty().Length(64);
        RuleFor(command => command.Request.Password).NotEmpty().MaximumLength(1024);
    }
}
