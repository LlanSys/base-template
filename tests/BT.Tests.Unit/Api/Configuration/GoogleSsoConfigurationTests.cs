using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace BT.Tests.Unit.Api.Configuration;

public sealed class GoogleSsoConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Google_is_optional_and_uses_a_distinct_callback_with_code_flow_and_pkce(bool enabled)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = new string('x', 64),
            ["GoogleSso:Enabled"] = enabled.ToString(),
            ["GoogleSso:ClientId"] = "test-client",
            ["GoogleSso:ClientSecret"] = "test-secret",
            ["EntraId:Enabled"] = "true",
            ["EntraId:TenantId"] = "11111111-1111-1111-1111-111111111111",
            ["EntraId:ClientId"] = "microsoft-test-client",
            ["EntraId:ClientSecret"] = "microsoft-test-secret"
        }).Build();

        var services = new ServiceCollection().AddLogging();
        BT.Infrastructure.Extensions.DependencyInjection.ConfigureAuthentication(services, configuration);
        await using var provider = services.BuildServiceProvider();
        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();
        Assert.Contains(schemes, scheme => scheme.Name == "EntraId");
        Assert.Equal(enabled, schemes.Any(scheme => scheme.Name == "Google"));

        if (!enabled) return;
        var options = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        var google = options.Get("Google");
        var microsoft = options.Get("EntraId");
        Assert.Equal("/signin-google", google.CallbackPath.Value);
        Assert.NotEqual(microsoft.CallbackPath, google.CallbackPath);
        Assert.Equal("code", google.ResponseType);
        Assert.True(google.UsePkce);
        Assert.False(google.SaveTokens);
        Assert.False(google.MapInboundClaims);
        Assert.Contains("email", google.Scope);
    }
}
