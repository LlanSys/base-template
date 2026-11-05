namespace BT.Application.Features.ControlPlane.Modules;

public interface IModuleReadinessService
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetIssuesAsync(CancellationToken cancellationToken);
    Task<bool> IsReadyAsync(string moduleKey, CancellationToken cancellationToken);
}
