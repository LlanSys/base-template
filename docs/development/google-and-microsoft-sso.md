# Google and Microsoft sign-in

BaseTemplate supports Microsoft Entra ID and Google through ASP.NET Core OpenID Connect authorization-code flow with PKCE. Providers are disabled until configured. The UI obtains the enabled provider list from the API; it does not display a button for an unavailable scheme. Both flows preserve application MFA requirements. Provider access/refresh tokens are not persisted.

## Local callback registration

Use a Google OAuth **Web application** client. This server-side flow does not use Google's JavaScript SDK, so Authorized JavaScript origins can remain empty. Register the callback on the API, not the Blazor callback page:

| Mode | Google redirect URI | Microsoft redirect URI | Blazor origin |
| --- | --- | --- | --- |
| Existing Docker applications | `http://localhost:8080/signin-google` | `http://localhost:8080/signin-oidc` | `http://localhost:8081` |
| Visual Studio HTTPS profiles | `https://localhost:7129/signin-google` | `https://localhost:7129/signin-oidc` | `https://localhost:7049` |
| Visual Studio HTTP profiles | `http://localhost:5157/signin-google` | `http://localhost:5157/signin-oidc` | `http://localhost:5106` |

Google consent screens in Testing mode require the intended accounts in the test-user list. Request only `openid`, `profile`, and `email`. Register the exact production HTTPS API callback separately when deploying.

## Configuration

API user-secrets own the local values. Use Manage User Secrets or `dotnet user-secrets` against `src/Backend/Api/BT.Api/BT.Api.csproj`; never commit credential values.

| Google | Microsoft |
| --- | --- |
| `GoogleSso:Enabled=true` | `EntraId:Enabled=true` |
| `GoogleSso:ClientId` | `EntraId:ClientId` |
| `GoogleSso:ClientSecret` | `EntraId:ClientSecret` |
| `GoogleSso:CallbackPath=/signin-google` | `EntraId:CallbackPath=/signin-oidc` |
| Fixed Google discovery authority | `EntraId:TenantId` selects the trusted authority |

Set `IamProvisioning:SignInUrl` to the selected Blazor origin plus `/iam/sign-in`. Set the Blazor `BackendApi:PublicBaseUrl` to the central API origin used by the browser. Keep `BackendApi:BaseUrl` as the server-to-server address, for example `http://api:8080/` within Compose. When switching HTTP/HTTPS launch profiles, align these origins and the registered callback together.

`scripts/setup-local-app-deployment.ps1` imports enabled providers' credentials from API user-secrets into the existing ignored `ops/local/.env` when the corresponding Compose values are missing. Explicit Compose settings take precedence. `APP_GOOGLE_SSO_*` and `APP_ENTRA_SSO_*` values are supplied at container runtime; `.env` is excluded from Docker build context. For an existing running app group, recreate only its API/Blazor services after configuration changes. Do not make another Compose group.

In production use environment/Key Vault configuration (`GoogleSso__ClientSecret` / `GoogleSso--ClientSecret`, and equivalent Microsoft names). Keep local credentials separate from production credentials. Preserve the API Data Protection ring across instances so protected OIDC state and external cookies remain readable.

## Identity and tenant binding

- The registered provider validates token signature, issuer, audience, nonce and protocol state. The application binds the external credential to issuer plus subject, not a mutable email address. Google invitation acceptance requires the verified email claim and the invited address.
- A central API origin receives provider callbacks for all approved organization hosts. The start endpoint validates the Blazor return origin/path against the tenant catalogue and configured scheme/port; the protected authentication properties carry tenant identity and intent. The callback revalidates the tenant before establishing a server-owned scope. Browser-supplied headers cannot set that scope item.
- The welcome page posts its invitation credential in the request body. The credential is carried through OIDC in protected state, rather than a browser-visible API query parameter. The underlying invitation is claimed only after provider authentication and explicit invitation intent.
- New invited identities are created only after validated acceptance. Existing accounts are never connected automatically by matching email. An already linked invited account can use its established provider; otherwise it accepts with its current password, or connects a provider through Account security after signing in.
- The connection UI requires current-password confirmation and external authentication. Its two-minute one-use ticket is stored hashed in the existing IAM token table and bound to the account security stamp and tenant. Replayed, changed-account, conflicting-identity, and cross-tenant requests fail.
- The callback returns a separate two-minute one-use database authorization code. API exchange consumes it atomically before issuing a session or an application MFA challenge. Redis never stores the bearer or refresh tokens from this flow. An enrolled authenticator is challenged; incomplete administrator setup remains restricted until MFA and final acceptance complete.
- Administrator completion checks signed application MFA evidence from the current session, as well as enrollment. The authenticator verifier issues that evidence. Refresh can preserve existing verified-session evidence, but cannot promote a stored enrollment flag or an external provider's untrusted claim into application MFA proof.

`EntraId:AutoLinkByVerifiedEmail` remains a recognized legacy setting for configuration compatibility; automatic email-only linking is no longer performed.

Previously stored Microsoft login mappings containing only a provider subject need explicit authenticated reconnection, or an operator-reviewed migration that establishes their original issuer. Do not guess that issuer or rebind accounts by email during an upgrade. Existing password credentials and permissions are not changed by the schema migrations.

## Verification

Automated coverage includes provider registration/PKCE options, redirect rejection, tenant-bound one-use exchange, external account connection, external invitation acceptance, and MFA enforcement. Live consent requires the account owner to complete provider authentication. A successful 302 to Google verifies challenge configuration, not the whole login flow.

On 2026-11-05, the account owner completed the local Google connection, Google sign-in, and application authenticator challenge. The vendor dashboard and existing permissions were observed. Microsoft remains supported and configurable; live Microsoft consent was not repeated in this verification run.

Primary references: [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect), [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html), and [OWASP token-handling guidance](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html).
