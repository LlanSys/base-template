using BT.Application.Features.ControlPlane.Tenants.Commands;

using FluentValidation;
namespace BT.Application.Features.ControlPlane.Tenants.Validators;

public sealed class InviteTenantAdministratorCommandValidator : AbstractValidator<InviteTenantAdministratorCommand>
{
    public InviteTenantAdministratorCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Request.LastName).NotEmpty().MaximumLength(100);
    }
}
