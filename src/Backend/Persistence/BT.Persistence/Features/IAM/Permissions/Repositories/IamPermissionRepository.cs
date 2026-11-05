using BT.Domain.Features.IAM.Permissions.Contracts.Repositories;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.IAM.DataContext;

using Microsoft.EntityFrameworkCore;

namespace BT.Persistence.Features.IAM.Permissions.Repositories;

public sealed class IamPermissionRepository(IamDBContext context)
    : Repository<Permission>(context), IPermissionRepository
{
    public async Task<bool> HasReferencesAsync(string key, CancellationToken cancellationToken)
        => await context.RoleClaims.AnyAsync(claim => claim.ClaimType == "permission" && claim.ClaimValue == key, cancellationToken).ConfigureAwait(false)
            || await context.UserClaims.AnyAsync(claim => claim.ClaimType == "permission" && claim.ClaimValue == key, cancellationToken).ConfigureAwait(false)
            || await context.MenuItems.AnyAsync(menu => menu.RequiredPermissionKey == key, cancellationToken).ConfigureAwait(false);
}
