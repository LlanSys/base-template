using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Authorization;

internal static class IdentityWriteScope
{
    public static async Task<AppResponse<T>> RunAsync<T>(IamDBContext context, Func<Task<AppResponse<T>>> write, CancellationToken cancellationToken,
        System.Data.IsolationLevel isolation = System.Data.IsolationLevel.ReadCommitted)
    {
        if (!context.Database.IsRelational()) return await write().ConfigureAwait(false);
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            var transaction = await context.Database.BeginTransactionAsync(isolation, cancellationToken).ConfigureAwait(false);
            await using var transactionLifetime = transaction.ConfigureAwait(false);
            var response = await write().ConfigureAwait(false);

            if (response.IsSuccess) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            else await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }).ConfigureAwait(false);
    }
}
