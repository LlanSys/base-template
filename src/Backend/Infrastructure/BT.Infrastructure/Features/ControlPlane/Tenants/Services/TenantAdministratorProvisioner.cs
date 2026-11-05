using System.Net;

using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Infrastructure.Configuration;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.Notifications.Dtos;

using Microsoft.Extensions.Options;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed class TenantAdministratorProvisioner(IEmailService email, IOptions<IamProvisioningSettings> options)
{
    public async Task<AppResponse<bool>> SendAsync(TenantAdministratorInvitation invitation, string token, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(options.Value.SignInUrl, UriKind.Absolute, out var configuredUri)
            || configuredUri.Scheme is not ("https" or "http")) return AppResponses.Failure<bool>("Invitation delivery is unavailable.");
        var link = new UriBuilder(configuredUri)
        {
            Host = invitation.Tenant.HostName,
            Path = "/iam/invitations/accept",
            Query = $"id={invitation.Id:D}&token={Uri.EscapeDataString(token)}",
            Fragment = string.Empty
        }.Uri;

        var sent = await email.SendEmailAsync(new SendEmailRequest
        {
            To = invitation.Email,
            Subject = $"You're invited to administer {invitation.Tenant.DisplayName}",
            Body = $"<p>Hello {WebUtility.HtmlEncode(invitation.FirstName)},</p><p>You have been invited to administer {WebUtility.HtmlEncode(invitation.Tenant.DisplayName)}.</p>"
                + $"<p><a href=\"{WebUtility.HtmlEncode(link.AbsoluteUri)}\">Review and accept your invitation</a> to set up your account and authenticator.</p>"
                + $"<p>This invitation expires at {invitation.ExpiresAt:dd MMM yyyy HH:mm} UTC.</p>"
                + "<p>If you did not expect it, you can ignore this email. Opening the link does not accept the invitation.</p>"
        }, cancellationToken).ConfigureAwait(false);

        return sent.IsSuccess ? AppResponses.Success("Invitation sent.", true) : AppResponses.Failure<bool>("Invitation delivery failed. Retry is scheduled.");
    }
}
