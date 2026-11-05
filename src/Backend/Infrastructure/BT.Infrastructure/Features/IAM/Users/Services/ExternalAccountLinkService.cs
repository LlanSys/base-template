using System.Text.Json;

using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Security;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Users.Services;

internal sealed class ExternalAccountLinkService(IamDBContext iam, UserManager<AppUser> users, SignInManager<AppUser> signIn,
    IIamUnitOfWork unitOfWork, ICurrentTenantProvider tenantScope, TimeProvider timeProvider) : IExternalAccountLinkService
{
    private const string TicketProvider = "BT.ExternalIdentityConnection";
    private const string Unavailable = "This sign-in connection request is unavailable or has expired. Start again from account security.";
    public async Task<AppResponse<string>> CreateAsync(string userId, ConnectExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        if (request.Provider is not ("EntraId" or "Google")) return AppResponses.Failure<string>("This sign-in provider is unsupported.");
        var user = await users.FindByIdAsync(userId).ConfigureAwait(false);

        if (!CanConnect(user)) return AppResponses.Failure<string>("Complete account setup before connecting a sign-in method.");
        var verified = await signIn.CheckPasswordSignInAsync(user!, request.CurrentPassword, lockoutOnFailure: true).ConfigureAwait(false);

        if (!verified.Succeeded && !verified.RequiresTwoFactor) return AppResponses.Failure<string>("Your current password could not be verified.");
        var code = SingleUseToken.Create();
        await iam.Set<IdentityUserToken<string>>().Where(ticket => ticket.UserId == userId && ticket.LoginProvider == TicketProvider)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        iam.Set<IdentityUserToken<string>>().Add(new IdentityUserToken<string>
        {
            UserId = userId,
            LoginProvider = TicketProvider,
            Name = SingleUseToken.Hash(code),
            Value = JsonSerializer.Serialize(new ConnectionState(request.Provider, timeProvider.GetUtcNow().AddMinutes(2), user!.SecurityStamp ?? string.Empty))
        });

        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("Confirm the external account you want to connect.", code);
    }
    public async Task<AppResponse<string>> ValidateAsync(string code, string provider, CancellationToken cancellationToken)
    {
        var pending = await FindAsync(code, provider, cancellationToken).ConfigureAwait(false);
        return pending is null ? AppResponses.Failure<string>(Unavailable) : AppResponses.Success(pending.UserId);
    }
    private bool CanConnect(AppUser? user) => user is { IsActive: true, IsDeleted: false } && !user.RequiresOnboarding && user.TenantId == tenantScope.TenantId;
    private async Task<IdentityUserToken<string>?> FindAsync(string code, string provider, CancellationToken cancellationToken)
    {
        if (code is not { Length: 64 }) return null;
        var hash = SingleUseToken.Hash(code);
        var ticket = await iam.Set<IdentityUserToken<string>>().AsNoTracking().SingleOrDefaultAsync(ticket => ticket.Name == hash && ticket.LoginProvider == TicketProvider, cancellationToken).ConfigureAwait(false);

        if (ticket?.Value is null) return null;
        ConnectionState? state;

        try { state = JsonSerializer.Deserialize<ConnectionState>(ticket.Value); }
        catch (JsonException) { return null; }

        if (state is null || state.Provider != provider || state.ExpiresAt <= timeProvider.GetUtcNow()) return null;
        var user = await users.FindByIdAsync(ticket.UserId).ConfigureAwait(false);
        return CanConnect(user) && user!.SecurityStamp == state.SecurityStamp ? ticket : null;
    }
    public async Task<AppResponse<string>> ConnectAsync(string code, string provider, string providerKey, CancellationToken cancellationToken)
    {
        var ticket = await FindAsync(code, provider, cancellationToken).ConfigureAwait(false);

        if (ticket is null) return AppResponses.Failure<string>(Unavailable);

        if (await iam.UserLogins.AnyAsync(login => login.LoginProvider == provider && login.ProviderKey == providerKey && login.UserId != ticket.UserId, cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<string>("That external identity is already connected to another account.");
        return await iam.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            var transaction = await iam.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var transactionLifetime = transaction.ConfigureAwait(false);
            var consumed = await iam.Set<IdentityUserToken<string>>().Where(item => item.UserId == ticket.UserId && item.LoginProvider == TicketProvider && item.Name == ticket.Name)
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            if (consumed != 1) return AppResponses.Failure<string>(Unavailable);
            var user = await users.FindByIdAsync(ticket.UserId).ConfigureAwait(false);

            if (!CanConnect(user)) return AppResponses.Failure<string>(Unavailable);
            var existing = await users.FindByLoginAsync(provider, providerKey).ConfigureAwait(false);

            if (existing?.Id != user!.Id && !(await users.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider)).ConfigureAwait(false)).Succeeded)
                return AppResponses.Failure<string>("The external account could not be connected. Please try again.");
            await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success("External sign-in connected.", user.Id);
        }).ConfigureAwait(false);
    }
    private sealed record ConnectionState(string Provider, DateTimeOffset ExpiresAt, string SecurityStamp);
}
