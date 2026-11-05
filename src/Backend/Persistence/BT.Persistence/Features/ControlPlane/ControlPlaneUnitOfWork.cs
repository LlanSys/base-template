using BT.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Domain.Features.ControlPlane.Modules.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.ControlPlane.Modules;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BT.Persistence.Features.ControlPlane;

public class ControlPlaneUnitOfWork : IControlPlaneUnitOfWork
{
    private readonly ControlPlaneDBContext _context;

    public IRepository<TenantAdministratorInvitation> AdministratorInvitations { get; }
    public ITenantRepository Tenants { get; }
    public IModuleDefinitionRepository Modules { get; }
    public IDeploymentStampRepository DeploymentStamps { get; }
    public IImpersonationRecordRepository ImpersonationRecords { get; }

    public ControlPlaneUnitOfWork(
        ControlPlaneDBContext context,
        ITenantRepository tenants,
        IDeploymentStampRepository deploymentStamps,
        IImpersonationRecordRepository impersonationRecords)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        AdministratorInvitations = new Repository<TenantAdministratorInvitation>(context);
        Modules = new ModuleDefinitionRepository(context);
        Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        DeploymentStamps = deploymentStamps ?? throw new ArgumentNullException(nameof(deploymentStamps));
        ImpersonationRecords = impersonationRecords ?? throw new ArgumentNullException(nameof(impersonationRecords));
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
