using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace BT.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed partial class TenantInvitationWorker(IServiceScopeFactory scopes, ILogger<TenantInvitationWorker> logger, TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                var scope = scopes.CreateAsyncScope();
                await using var scopeLifetime = scope.ConfigureAwait(false);
                await scope.ServiceProvider.GetRequiredService<TenantInvitationDispatcher>().DispatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException) { DispatchFailed(logger, exception); }
        }
    }
    [LoggerMessage(EventId = 2683, Level = LogLevel.Error, Message = "Administrator invitation dispatch failed; pending work remains durable.")]
    private static partial void DispatchFailed(ILogger logger, Exception exception);
}
