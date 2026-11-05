using BT.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BT.Tests.Unit.Api.Configuration;

public sealed class MessagingConfigurationTests
{
    [Fact]
    public void Enabled_outbox_rejects_an_unknown_database_provider_before_registering_a_bus()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Enabled"] = "true",
            ["DatabaseSettings:Provider"] = "Unknown"
        }).Build();

        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.ConfigureOutBoxMessagingWithGlobalRetry(configuration));
    }
}
