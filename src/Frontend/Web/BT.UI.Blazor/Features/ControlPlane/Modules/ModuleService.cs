using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

namespace BT.UI.Blazor.Features.ControlPlane.Modules;

internal sealed class ModuleService(IBackendApiClient client)
{
    private const string Path = "api/v1/control-plane/modules";
    public Task<AppResponse<List<ModuleDefinitionResponse>>> GetAsync(CancellationToken ct = default)
        => client.SendAsync<List<ModuleDefinitionResponse>>(HttpMethod.Get, Path, requiresAuthentication: true, cancellationToken: ct);
    public Task<AppResponse<ModuleDefinitionResponse>> SaveAsync(Guid? id, ModuleDefinitionRequest request, CancellationToken ct = default)
        => client.SendAsync<ModuleDefinitionResponse>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"{Path}/{id}" : Path, request: request, requiresAuthentication: true, cancellationToken: ct);
    public Task<AppResponse<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
        => client.SendAsync<bool>(HttpMethod.Delete, $"{Path}/{id}", requiresAuthentication: true, cancellationToken: ct);
}
