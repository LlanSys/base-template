using FluentValidation;
using BT.SharedKernel.Features.ControlPlane.Modules;

namespace BT.Application.Features.ControlPlane.Modules;

public sealed class SaveModuleCommandValidator : AbstractValidator<SaveModuleCommand>
{
    public SaveModuleCommandValidator()
    {
        RuleFor(command => command.Request.Key).NotEmpty().MaximumLength(100).Must(ModuleCatalog.IsImplemented)
            .WithMessage("The module must be implemented and registered in this release.");

        RuleFor(command => command.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Request.Description).MaximumLength(1000);
        RuleFor(command => command.Request.Revision).NotEmpty().When(command => command.Id.HasValue);
    }
}
