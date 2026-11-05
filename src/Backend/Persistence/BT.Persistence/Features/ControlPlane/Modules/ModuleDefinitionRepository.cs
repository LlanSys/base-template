using BT.Domain.Features.ControlPlane.Modules.Contracts;
using BT.Domain.Features.ControlPlane.Modules.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.ControlPlane.DataContext;

using Microsoft.EntityFrameworkCore;
namespace BT.Persistence.Features.ControlPlane.Modules;

internal sealed class ModuleDefinitionRepository(ControlPlaneDBContext context) : Repository<ModuleDefinition>(context), IModuleDefinitionRepository
{
    public Task<bool> IsKeyReservedAsync(string key, CancellationToken cancellationToken)
        => context.Modules.IgnoreQueryFilters().AnyAsync(module => module.Key == key, cancellationToken);
}
