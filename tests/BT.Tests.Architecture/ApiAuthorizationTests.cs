using BT.Api.Common.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace BT.Tests.Architecture;

public sealed class ApiAuthorizationTests
{
    private static readonly string[] ControllersAllowedWithoutPermissionRequirement =
    [
        "AuthController",
        "TotpController"
    ];

    private static readonly string[] ActionsAllowedWithoutPermissionRequirement =
    [
        "IamAdminController.GetNavigationMenus",
        "PaymentsController.StripeWebhook",
        "PaymentsController.MpesaStkCallback",
        "PaymentsController.MpesaC2BValidation",
        "PaymentsController.MpesaC2BConfirmation",
        "ProfileController.GetProfilePicture",
        "ProfileController.UpdateProfilePicture",
        "ExternalSignInController.Providers",
        "ExternalSignInController.Login",
        "ExternalSignInController.AcceptWithProvider",
        "ExternalSignInController.Callback",
        "ExternalSignInController.Connect"
    ];

    private static readonly string[] AuthControllerAllowedActions =
    [
        "ForgotPassword",
        "GetCurrentUser",
        "GetOtpStatus",
        "Login",
        "Logout",
        "RefreshToken",
        "ResetPassword",
        "SendEmailOtp",
        "VerifyEmailOtp",
        "VerifyPasswordResetOtp",
        "VerifyPassword",
        "ChangePassword",
        "SsoLogin",
        "SsoCallback",
        "ExchangeSsoCode",
        "LoginWithPasskey",
        "RegisterPasskey",
        "RequestPasskeyLoginOptions",
        "RequestPasskeyRegistrationOptions",
        "GetPasskeys",
        "InspectInvitation",
        "AcceptInvitation",
        "CompleteOnboarding"
    ];

    [Fact]
    public void Api_Controller_Actions_Should_Declare_Authorization_Intent()
    {
        var unsecuredActions = GetControllerActions()
            .Where(action => !HasAuthorizationIntent(action.Controller, action.Method))
            .Select(action => $"{action.Controller.Name}.{action.Method.Name}")
            .Order(StringComparer.Ordinal)
            .ToList();

        unsecuredActions.Should().BeEmpty(
            because: "every API action must explicitly declare [Authorize], [AllowAnonymous], or a permission policy. Found: {0}",
            string.Join(", ", unsecuredActions));
    }

    [Fact]
    public void Feature_Controller_Actions_Should_Use_Permission_Requirements()
    {
        var actionsWithoutPermissions = GetControllerActions()
            .Where(action => !ControllersAllowedWithoutPermissionRequirement.Contains(action.Controller.Name, StringComparer.Ordinal))
            .Where(action => !ActionsAllowedWithoutPermissionRequirement.Contains($"{action.Controller.Name}.{action.Method.Name}", StringComparer.Ordinal))
            .Where(action => !HasPermissionRequirement(action.Controller, action.Method))
            .Select(action => $"{action.Controller.Name}.{action.Method.Name}")
            .Order(StringComparer.Ordinal)
            .ToList();

        actionsWithoutPermissions.Should().BeEmpty(
            because: "feature/admin API actions must use [RequirePermission] so authorization stays permission-driven. Found: {0}",
            string.Join(", ", actionsWithoutPermissions));
    }

    [Fact]
    public void AuthController_Should_Only_Expose_Authentication_Actions()
    {
        var unexpectedActions = GetControllerActions()
            .Where(static action => action.Controller.Name == "AuthController")
            .Select(static action => action.Method.Name)
            .Except(AuthControllerAllowedActions, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        unexpectedActions.Should().BeEmpty(
            because: "AuthController must stay limited to authentication/session endpoints. Admin or provisioning endpoints belong in IamAdminController. Found: {0}",
            string.Join(", ", unexpectedActions));
    }

    [Fact]
    public void Account_connection_and_onboarding_completion_require_authentication_without_an_anonymous_override()
    {
        foreach (var (controller, name) in new[]
        {
            (typeof(Api.Features.IAM.Users.Controllers.ExternalSignInController), "Connect"),
            (typeof(Api.Features.IAM.Users.Controllers.AuthController), "CompleteOnboarding")
        })
        {
            var method = controller.GetMethod(name)!;
            Assert.True(HasAttribute<AuthorizeAttribute>(controller, method));
            Assert.False(HasAttribute<AllowAnonymousAttribute>(controller, method));
        }
    }

    [Fact]
    public void Shipped_endpoint_permissions_must_be_registered_for_retirement_protection()
    {
        var unprotected = GetControllerActions().SelectMany(action => action.Controller.GetCustomAttributes<RequirePermissionAttribute>()
                .Concat(action.Method.GetCustomAttributes<RequirePermissionAttribute>()))
            .Select(attribute => attribute.Policy!["Permission:".Length..])
            .Where(key => !BT.SharedKernel.Features.IAM.Permissions.ImplementedPermissions.IsProtected(key)).Distinct().ToArray();

        Assert.Empty(unprotected);
    }

    private static IEnumerable<(Type Controller, MethodInfo Method)> GetControllerActions()
    {
        return AssemblyReferences.Api
            .GetTypes()
            .Where(static type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(static type => type.Namespace?.StartsWith("BT.Api.Features.", StringComparison.Ordinal) == true)
            .SelectMany(static controller => controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(HasHttpMethodAttribute)
                .Select(method => (controller, method)));
    }

    private static bool HasHttpMethodAttribute(MethodInfo method)
    {
        return method.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>().Any();
    }

    private static bool HasAuthorizationIntent(Type controller, MethodInfo method)
    {
        return HasAttribute<AllowAnonymousAttribute>(controller, method)
            || HasAttribute<AuthorizeAttribute>(controller, method);
    }

    private static bool HasPermissionRequirement(Type controller, MethodInfo method)
    {
        return HasAttribute<RequirePermissionAttribute>(controller, method);
    }

    private static bool HasAttribute<TAttribute>(Type controller, MethodInfo method)
        where TAttribute : Attribute
    {
        return controller.GetCustomAttributes(inherit: true).OfType<TAttribute>().Any()
            || method.GetCustomAttributes(inherit: true).OfType<TAttribute>().Any();
    }
}
