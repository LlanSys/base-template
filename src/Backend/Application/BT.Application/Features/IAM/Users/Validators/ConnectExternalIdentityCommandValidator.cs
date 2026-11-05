using BT.Application.Features.IAM.Users.Commands;

using FluentValidation;
namespace BT.Application.Features.IAM.Users.Validators;

public sealed class ConnectExternalIdentityCommandValidator : AbstractValidator<ConnectExternalIdentityCommand>
{
    public ConnectExternalIdentityCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Request.Provider).Must(provider => provider is "EntraId" or "Google");
        RuleFor(command => command.Request.CurrentPassword).NotEmpty().MaximumLength(1024);
    }
}
