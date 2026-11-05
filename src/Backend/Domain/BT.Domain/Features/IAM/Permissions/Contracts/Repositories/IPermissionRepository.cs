using BT.Domain.Features.IAM.Permissions.Entities;
using BT.Domain.Shared.Contracts.Repositories;

namespace BT.Domain.Features.IAM.Permissions.Contracts.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<bool> HasReferencesAsync(string key, CancellationToken cancellationToken);
}
