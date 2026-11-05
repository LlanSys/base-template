using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BT.Infrastructure.Configuration;
using BT.Infrastructure.Security;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed partial class TenantInvitationDispatcher(ControlPlaneDBContext context, TenantAdministratorProvisioner provisioner, ILogger<TenantInvitationDispatcher> logger, IOptions<AdministratorInvitationSettings> settings, TimeProvider timeProvider)
{
    public async Task DispatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var pending = await context.AdministratorInvitations.Include(item => item.Tenant)
            .Where(item => item.AcceptanceStatus == InvitationAcceptanceStatus.Pending && item.Status != AdministratorInvitationStatus.Delivered && item.NextAttemptAt <= now
                && (item.LeaseUntil == null || item.LeaseUntil <= now) && !item.Tenant.IsDeleted && item.Tenant.Status == TenantStatus.Active)
            .OrderBy(item => item.NextAttemptAt).Take(10).ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var item in pending)
        {
            if (!item.TryLease(timeProvider.GetUtcNow())) continue;
            var token = SingleUseToken.Create();
            item.Issue(SingleUseToken.Hash(token), timeProvider.GetUtcNow().AddHours(settings.Value.ValidityHours));

            try { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
            catch (DbUpdateConcurrencyException exception) { LeaseLost(logger, exception); return; }

            try
            {
                var response = await provisioner.SendAsync(item, token, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccess) item.Complete(timeProvider.GetUtcNow());
                else { item.Fail(timeProvider.GetUtcNow()); DeliveryRejected(logger, item.Id); }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                DeliveryFailed(logger, item.Id, exception);
                item.Fail(timeProvider.GetUtcNow());
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    [LoggerMessage(EventId = 2680, Level = LogLevel.Debug, Message = "Invitation lease was claimed by another worker.")]
    private static partial void LeaseLost(ILogger logger, Exception exception);
    [LoggerMessage(EventId = 2681, Level = LogLevel.Warning, Message = "Administrator invitation {InvitationId} was rejected; retry scheduled.")]
    private static partial void DeliveryRejected(ILogger logger, Guid invitationId);
    [LoggerMessage(EventId = 2682, Level = LogLevel.Error, Message = "Administrator invitation {InvitationId} failed; retry scheduled.")]
    private static partial void DeliveryFailed(ILogger logger, Guid invitationId, Exception exception);
}
