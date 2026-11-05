using System.Security.Claims;
using Asp.Versioning;
using BT.Api.Common.Controllers;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Configuration;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BT.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/auth/sso")]
[ApiController]
public sealed class ExternalSignInController(ISender sender, IAuthenticationSchemeProvider schemes, IControlPlaneUnitOfWork control,
    IOptions<IamProvisioningSettings> settings, Application.Features.IAM.Users.Contracts.Interfaces.IExternalAccountLinkService links) : BaseController
{
    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> Providers()
    {
        var registered = await schemes.GetAllSchemesAsync().ConfigureAwait(false);
        var providers = registered.Where(scheme => scheme.Name is "EntraId" or "Google")
            .Select(scheme => new SsoProviderResponse(scheme.Name, scheme.Name == "EntraId" ? "Microsoft" : "Google")).ToArray();

        return HandleResponse(AppResponses.Success(providers));
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public Task<IActionResult> Login([FromQuery] string returnUrl, [FromQuery] string provider = "EntraId", [FromQuery] string? connectionCode = null, CancellationToken cancellationToken = default)
        => StartAsync(provider, returnUrl, null, null, connectionCode, cancellationToken);

    [HttpPost("connect")]
    [Authorize]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Connect(ConnectExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await schemes.GetSchemeAsync(request.Provider).ConfigureAwait(false) is null)
            return HandleResponse(AppResponses.Failure<string>("This sign-in method is unavailable."));
        return HandleResponse(await sender.Send(new ConnectExternalIdentityCommand(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public Task<IActionResult> AcceptWithProvider([FromForm] string provider, [FromForm] string returnUrl,
        [FromForm] Guid invitationId, [FromForm] string invitationToken, CancellationToken cancellationToken)
        => StartAsync(provider, returnUrl, invitationId, invitationToken, null, cancellationToken);

    private async Task<IActionResult> StartAsync(string provider, string returnUrl, Guid? invitationId, string? invitationToken, string? connectionCode, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";

        if (provider is not ("EntraId" or "Google") || await schemes.GetSchemeAsync(provider).ConfigureAwait(false) is null)
            return HandleResponse(AppResponses.Failure<bool>("This sign-in method is unavailable."));

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var target))
            return HandleResponse(AppResponses.Failure<bool>("The sign-in return address is unavailable."));
        var tenant = await control.Tenants.FirstOrDefaultAsync(item => item.HostName == target.Host, cancellationToken).ConfigureAwait(false);

        if (tenant is null || tenant.IsDeleted || tenant.Status != TenantStatus.Active
            || !SsoRedirectPolicy.IsAllowed(returnUrl, settings.Value.SignInUrl, tenant.HostName))
            return HandleResponse(AppResponses.Failure<bool>("Sign-in is unavailable for this organization or return address."));
        HttpContext.Items[ICurrentTenantProvider.ExternalSignInScopeKey] = tenant.Id;

        if (connectionCode is not null && !(await links.ValidateAsync(connectionCode, provider, cancellationToken).ConfigureAwait(false)).IsSuccess)
            return HandleResponse(AppResponses.Failure<bool>("This connection request is unavailable. Start again from account security."));

        if (invitationId.HasValue)
        {
            var preview = await sender.Send(new InspectAdministratorInvitationCommand(new InspectAdministratorInvitationRequest(invitationId.Value, invitationToken ?? string.Empty)), cancellationToken).ConfigureAwait(false);

            if (!preview.IsSuccess) return HandleResponse(AppResponses.Failure<bool>(preview.Message ?? "Failed to inspect invitation."));
        }
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(Callback), "ExternalSignIn"), IsPersistent = false };
        properties.Items["bt_provider"] = provider;
        properties.Items["bt_return"] = returnUrl;
        properties.Items["bt_tenant"] = tenant.Id.ToString();

        if (connectionCode is not null) properties.Items["bt_connection"] = connectionCode;

        if (invitationId.HasValue)
        {
            properties.Items["bt_invitation"] = invitationId.Value.ToString();
            properties.Items["bt_invitation_token"] = invitationToken;
        }
        return Challenge(properties, provider);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);

        try
        {
            if (!result.Succeeded || result.Principal is null || result.Properties is null)
                return HandleResponse(AppResponses.Failure<bool>("External sign-in could not be verified."));
            var items = result.Properties.Items;
            items.TryGetValue("bt_provider", out var provider);
            items.TryGetValue("bt_return", out var returnUrl);
            items.TryGetValue("bt_tenant", out var tenantText);

            if (provider is not ("EntraId" or "Google") || !Guid.TryParse(tenantText, out var tenantId))
                return HandleResponse(AppResponses.Failure<bool>("External sign-in could not be verified for this organization."));
            var tenant = await control.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

            if (tenant is null || tenant.IsDeleted || tenant.Status != TenantStatus.Active
                || !SsoRedirectPolicy.IsAllowed(returnUrl, settings.Value.SignInUrl, tenant.HostName))
                return HandleResponse(AppResponses.Failure<bool>("External sign-in is unavailable."));
            HttpContext.Items[ICurrentTenantProvider.ExternalSignInScopeKey] = tenant.Id;
            var principal = result.Principal;
            var subject = principal.FindFirstValue("sub");
            var issuer = principal.FindFirstValue("bt_issuer");
            var email = principal.FindFirstValue("email") ?? (provider == "EntraId" ? principal.FindFirstValue("preferred_username") : null);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(email)
                || (provider == "Google" && !string.Equals(principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase)))
                return HandleResponse(AppResponses.Failure<bool>("The provider did not supply the identity information required for sign-in."));
            Guid? invitationId = items.TryGetValue("bt_invitation", out var invitationText) && Guid.TryParse(invitationText, out var parsed) ? parsed : null;
            items.TryGetValue("bt_invitation_token", out var invitationToken);

            if (items.TryGetValue("bt_connection", out var connectionCode) && connectionCode is not null)
            {
                var connection = await links.ConnectAsync(connectionCode, provider, issuer + "|" + subject, cancellationToken).ConfigureAwait(false);

                if (!connection.IsSuccess) return HandleResponse(AppResponses.Failure<bool>(connection.Message ?? "Failed to connect."));
            }
            var response = await sender.Send(new ProcessSsoLoginCommand(email, principal.FindFirstValue("given_name") ?? string.Empty,
                principal.FindFirstValue("family_name") ?? string.Empty, provider, issuer + "|" + subject, tenantId, invitationId, invitationToken), cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess) return HandleResponse(AppResponses.Failure<bool>(response.Message ?? "Failed to process sign-in."));
            return Redirect(returnUrl! + "?code=" + Uri.EscapeDataString(response.Data ?? string.Empty));
        }
        finally { await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false); }
    }
}
