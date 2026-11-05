using BT.Domain.Features.Banking.Customers.Contracts.Repositories;
using BT.Domain.Features.HR.Employees.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Contracts.Repositories;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace BT.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamTempTotpSecretRepository(IamDBContext context, ILogger<IamTempTotpSecretRepository> logger, TimeProvider timeProvider) : Repository<TempTotpSecret>(context), ITempTotpSecretRepository
{
    public async Task<TempTotpSecret?> GetValidTempSecretByUserIdAsync(string userId)
    {
        var now = timeProvider.GetUtcNow();
        return await FindByCondition(x => x.UserId == userId &&
                                         !x.IsDeleted &&
                                         x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<bool> DeleteExpiredSecretsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            var expiredSecrets = await FindByCondition(x => x.ExpiresAt <= now && !x.IsDeleted)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (var secret in expiredSecrets)
            {
                secret.MarkAsDeleted("System", now);
            }

            await UpdateRangeAsync(new Collection<TempTotpSecret>(expiredSecrets), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogDeleteExpiredTempTotpSecretsError(logger, ex);
            return false;
        }
    }

    public async Task<bool> DeleteUserTempSecretsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var tempSecret = await FindByCondition(x => x.UserId == userId && !x.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (tempSecret == null) return false;

            tempSecret.MarkAsDeleted(userId, timeProvider.GetUtcNow());

            await UpdateAsync(tempSecret, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogDeleteUserTempTotpSecretsError(logger, userId, ex);
            return false;
        }
    }
}
