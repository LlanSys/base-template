using BT.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using BT.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BT.Domain.Features.ControlPlane.Contracts;

public interface IControlPlaneUnitOfWork : IDisposable, IAsyncDisposable
{
    Domain.Shared.Contracts.Repositories.IRepository<Tenants.Entities.TenantAdministratorInvitation> AdministratorInvitations { get; }
    ITenantRepository Tenants { get; }
    Modules.Contracts.IModuleDefinitionRepository Modules { get; }
    IDeploymentStampRepository DeploymentStamps { get; }
    IImpersonationRecordRepository ImpersonationRecords { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
