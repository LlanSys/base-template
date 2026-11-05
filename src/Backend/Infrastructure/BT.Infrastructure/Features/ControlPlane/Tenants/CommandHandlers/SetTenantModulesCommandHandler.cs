using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Modules;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class SetTenantModulesCommandHandler(TimeProvider timeProvider, IControlPlaneUnitOfWork unitOfWork, ICurrentActorProvider actor)
    : IRequestHandler<SetTenantModulesCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(SetTenantModulesCommand request, CancellationToken cancellationToken)
    {
        var tenant = await unitOfWork.Tenants.FindAll().Include(tenant => tenant.Modules)
            .SingleOrDefaultAsync(tenant => tenant.Id == request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null) return AppResponses.NotFound<bool>("Tenant not found.");
        var keys = request.Request.ModuleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var catalog = await unitOfWork.Modules.ListAsync(query => query, cancellationToken).ConfigureAwait(false);
        var existingKeys = tenant.Modules.Where(grant => grant.IsActive).Select(grant => grant.ModuleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (keys.Any(key => !ModuleCatalog.IsImplemented(key) || !catalog.Any(module => string.Equals(module.Key, key, StringComparison.OrdinalIgnoreCase)
            && (module.IsActive || existingKeys.Contains(key)))))
            return AppResponses.Failure<bool>("Select only available business modules.");

        foreach (var grant in tenant.Modules)
        {
            grant.IsActive = keys.Contains(grant.ModuleKey);
            grant.UpdatedBy = actor.ActorId;
            grant.UpdatedAt = timeProvider.GetUtcNow();
        }

        foreach (var key in keys.Where(key => !tenant.Modules.Any(grant => string.Equals(grant.ModuleKey, key, StringComparison.OrdinalIgnoreCase))))
            tenant.Modules.Add(new TenantModule { TenantId = tenant.Id, ModuleKey = key, CreatedBy = actor.ActorId });
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
