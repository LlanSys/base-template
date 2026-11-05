using System.Security.Claims;
namespace BT.Application.Features.IAM.Authorization;

public static class MfaSessionClaims
{
    public const string Verified = "mfa_verified";

    // Called only after this application successfully verifies an authenticator code.
    public static void MarkVerified(List<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        claims.RemoveAll(claim => claim.Type == Verified);
        claims.Add(new Claim(Verified, "true"));
    }

    // The original principal must already have passed access-token signature validation.
    public static void PreserveVerifiedSession(ClaimsPrincipal original, List<Claim> refreshed)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(refreshed);
        refreshed.RemoveAll(claim => claim.Type == Verified);

        if (original.HasClaim(Verified, "true")) MarkVerified(refreshed);
    }
}
