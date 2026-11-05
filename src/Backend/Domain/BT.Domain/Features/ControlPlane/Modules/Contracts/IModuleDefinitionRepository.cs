using BT.Domain.Features.ControlPlane.Modules.Entities;
using BT.Domain.Shared.Contracts.Repositories;
namespace BT.Domain.Features.ControlPlane.Modules.Contracts;

public interface IModuleDefinitionRepository : IRepository<ModuleDefinition>
{
    Task<bool> IsKeyReservedAsync(string key, CancellationToken cancellationToken);
}
