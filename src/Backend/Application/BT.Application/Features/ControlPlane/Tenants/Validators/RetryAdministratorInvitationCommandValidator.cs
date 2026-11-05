using BT.Application.Features.ControlPlane.Tenants.Commands;

using FluentValidation;
namespace BT.Application.Features.ControlPlane.Tenants.Validators;

public sealed class RetryAdministratorInvitationCommandValidator : AbstractValidator<RetryAdministratorInvitationCommand>
{
    public RetryAdministratorInvitationCommandValidator()
    {
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.InvitationId).NotEmpty();
    }
}
