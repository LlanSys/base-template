using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;

namespace BT.Application.Features.ControlPlane.Modules;

internal sealed class GetModulesHandler(IControlPlaneUnitOfWork unitOfWork, IModuleReadinessService readiness)
    : IRequestHandler<GetModulesQuery, AppResponse<IReadOnlyList<ModuleDefinitionResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<ModuleDefinitionResponse>>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
    {
        var modules = await unitOfWork.Modules.ListAsync(query => query.OrderBy(module => module.Name)
            .Select(module => new ModuleDefinitionResponse(module.Id, module.Key, module.Name, module.Description, module.IsActive, module.Revision)),
            cancellationToken).ConfigureAwait(false);

        var issues = await readiness.GetIssuesAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<ModuleDefinitionResponse>>(modules.Select(module => module with { ReadinessIssues = issues[module.Key] }).ToArray());
    }
}
