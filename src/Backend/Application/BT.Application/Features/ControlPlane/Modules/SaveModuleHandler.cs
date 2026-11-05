using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Modules.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

internal sealed class SaveModuleHandler(IControlPlaneUnitOfWork unitOfWork, IModuleReadinessService readiness)
    : IRequestHandler<SaveModuleCommand, AppResponse<ModuleDefinitionResponse>>
{
    public async Task<AppResponse<ModuleDefinitionResponse>> Handle(SaveModuleCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var key = ModuleCatalog.Keys.FirstOrDefault(key => string.Equals(key, request.Key, StringComparison.OrdinalIgnoreCase));

        if (key is null) return AppResponses.Failure<ModuleDefinitionResponse>("The module is not implemented by this release.");
        var module = command.Id is { } id ? await unitOfWork.Modules.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) : null;

        if (command.Id.HasValue && module is null) return AppResponses.Failure<ModuleDefinitionResponse>("Module not found.");

        if (module is not null && (module.Revision != request.Revision || !string.Equals(module.Key, key, StringComparison.Ordinal)))
            return AppResponses.Failure<ModuleDefinitionResponse>("The module changed. Reload it before saving. Module keys cannot be renamed.");

        if (module is null && await unitOfWork.Modules.IsKeyReservedAsync(key, cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<ModuleDefinitionResponse>("This module is already registered.");
        var issues = (await readiness.GetIssuesAsync(cancellationToken).ConfigureAwait(false))[key];

        if (request.IsActive && issues.Count > 0)
            return AppResponses.Failure<ModuleDefinitionResponse>(string.Join(" ", issues));
        var isNew = module is null;
        module ??= new ModuleDefinition { Key = key, Name = request.Name.Trim(), CreatedBy = command.ActorId };
        module.Name = request.Name.Trim();
        module.Description = request.Description.Trim();
        module.IsActive = request.IsActive;
        module.Revision = Guid.CreateVersion7();

        if (isNew) await unitOfWork.Modules.CreateAsync(module, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new ModuleDefinitionResponse(module.Id, module.Key, module.Name, module.Description, module.IsActive, module.Revision));
    }
}
