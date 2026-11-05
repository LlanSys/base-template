namespace BT.Api.Features.IAM.Users.Controllers;

internal static class SsoRedirectPolicy
{
    public static bool IsAllowed(string? returnUrl, string configuredSignInUrl, string tenantHost) =>
        Uri.TryCreate(returnUrl, UriKind.Absolute, out var target)
        && Uri.TryCreate(configuredSignInUrl, UriKind.Absolute, out var configured)
        && target.Scheme is "https" or "http"
        && string.Equals(target.Scheme, configured.Scheme, StringComparison.OrdinalIgnoreCase)
        && target.Port == configured.Port
        && string.Equals(target.Host, tenantHost, StringComparison.OrdinalIgnoreCase)
        && target.AbsolutePath == "/iam/sso-callback"
        && string.IsNullOrEmpty(target.UserInfo) && string.IsNullOrEmpty(target.Query) && string.IsNullOrEmpty(target.Fragment);
}
