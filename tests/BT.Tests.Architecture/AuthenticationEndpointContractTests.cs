using System.Reflection;
using System.Text.Json;
using BT.Api.Features.IAM.Users.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace BT.Tests.Architecture;

public sealed class AuthenticationEndpointContractTests
{
    [Theory]
    [InlineData("AcceptInvitation", nameof(AuthController.AcceptInvitation))]
    [InlineData("ExchangeSsoCode", nameof(AuthController.ExchangeSsoCode))]
    public void Frontend_authentication_endpoint_configuration_matches_the_versioned_api_route(string setting, string action)
    {
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(AssemblyReferences.RepoRoot, "src/Frontend/Web/BT.UI.Blazor/appsettings.json")));
        var endpoint = configuration.RootElement.GetProperty("BackendApi").GetProperty("Endpoints").GetProperty("Iam").GetProperty("Auth").GetProperty(setting).GetString();
        var controller = typeof(AuthController);
        var prefix = controller.GetCustomAttribute<RouteAttribute>()!.Template;
        var suffix = controller.GetMethod(action)!.GetCustomAttribute<HttpPostAttribute>()!.Template;
        Assert.Equal((prefix + "/" + suffix).Replace("{version:apiVersion}", "{version}", StringComparison.Ordinal), endpoint);
    }
}
