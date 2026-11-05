using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.SharedKernel.Features.ControlPlane;
namespace BT.Tests.Unit.SaaS;

public sealed class AdministrationCompletionTests
{
    [Fact]
    public void Invitation_lease_prevents_concurrent_dispatch_and_retry_until_expiration()
    {
        var item = Invitation();
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.True(item.TryLease(now));
        Assert.False(item.TryLease(now.AddMinutes(1)));
        Assert.False(item.Retry(now.AddMinutes(1)));
        Assert.True(item.TryLease(now.AddMinutes(6)));
        Assert.Equal(2, item.Attempts);
    }
    [Fact]
    public void Failed_delivery_is_recoverable_and_completed_delivery_is_not_automatically_repeated()
    {
        var item = Invitation();
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        item.TryLease(now);
        item.Fail(now);
        Assert.Equal(AdministratorInvitationStatus.Failed, item.Status);
        Assert.False(item.TryLease(now));
        Assert.True(item.Retry(now));
        Assert.True(item.TryLease(now));
        item.Complete(now);
        Assert.Null(item.LastFailure);
        Assert.False(item.TryLease(now.AddDays(1)));
        Assert.True(item.Retry(now.AddDays(1)));
    }
    [Fact]
    public void Platform_shell_accepts_granular_read_permissions_without_a_legacy_admin_shortcut()
    {
        Assert.True(PlatformPermissions.OpensControlPanel(["controlplane.view_tenants"]));
        Assert.True(PlatformPermissions.OpensControlPanel(["modules.view"]));
        Assert.False(PlatformPermissions.OpensControlPanel(["controlplane.manage"]));
        Assert.False(PlatformPermissions.OpensControlPanel(["customers.view"]));
    }
    private static TenantAdministratorInvitation Invitation() => new(TimeProvider.System.GetUtcNow())
    {
        TenantId = Guid.CreateVersion7(),
        Email = "admin@example.test",
        NormalizedEmail = "ADMIN@EXAMPLE.TEST",
        FirstName = "Initial",
        LastName = "Administrator",
        CreatedBy = "test-actor"
    };
}
