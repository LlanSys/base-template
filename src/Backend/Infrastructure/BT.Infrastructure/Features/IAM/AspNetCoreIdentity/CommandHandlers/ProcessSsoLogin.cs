using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Features.ControlPlane.Tenants.Services;
using BT.Infrastructure.Security;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ProcessSsoLogin(UserManager<AppUser> users, IamDBContext iam, IIamUnitOfWork unitOfWork,
    ICurrentTenantProvider tenantScope, AdministratorInvitationAcceptance invitations, TimeProvider timeProvider) : IRequestHandler<ProcessSsoLoginCommand, AppResponse<string>>
{
    public async Task<AppResponse<string>> Handle(ProcessSsoLoginCommand command, CancellationToken cancellationToken)
    {
        if (command.TenantId != tenantScope.TenantId || command.Provider is not ("EntraId" or "Google"))
            return AppResponses.Failure<string>("External sign-in could not be verified for this organization.");
        AppUser? user;

        if (command.InvitationId.HasValue)
        {
            var accepted = await invitations.AcceptExternalAsync(command.InvitationId.Value, command.InvitationToken ?? string.Empty,
                command.Provider, command.ProviderKey, command.Email, cancellationToken).ConfigureAwait(false);

            if (!accepted.IsSuccess) return AppResponses.Failure<string>(accepted.Message ?? "An error occurred while accepting the invitation.");
            user = await users.FindByIdAsync(accepted.Data ?? string.Empty).ConfigureAwait(false);
        }
        else
        {
            // Email is display/contact data, never sufficient authority to link an existing account.
            user = await users.FindByLoginAsync(command.Provider, command.ProviderKey).ConfigureAwait(false);
        }

        if (user is null || user.IsDeleted || !user.IsActive || user.TenantId != command.TenantId
            || await users.IsLockedOutAsync(user).ConfigureAwait(false))
            return AppResponses.Failure<string>("This external identity is not linked to an available account in this organization. Use your existing sign-in method or invitation.");
        var code = SingleUseToken.Create();
        var now = timeProvider.GetUtcNow();
        await iam.Set<SsoAuthorizationCode>().Where(item => item.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        iam.Set<SsoAuthorizationCode>().Add(new SsoAuthorizationCode
        {
            Hash = SingleUseToken.Hash(code),
            UserId = user.Id,
            TenantId = command.TenantId,
            ExpiresAt = now.AddMinutes(2)
        });

        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("External identity verified.", code);
    }
}
