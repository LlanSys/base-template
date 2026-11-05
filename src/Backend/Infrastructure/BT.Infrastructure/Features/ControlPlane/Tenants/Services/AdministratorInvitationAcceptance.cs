using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Features.IAM.Users.Services;
using BT.Infrastructure.Security;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed class AdministratorInvitationAcceptance(ControlPlaneDBContext control, IamDBContext iam,
    IControlPlaneUnitOfWork controlUnitOfWork, UserManager<AppUser> users, SignInManager<AppUser> signIn,
    ICurrentTenantProvider tenantScope, SignInResponseFactory responses, TimeProvider timeProvider)
{
    private const string Unavailable = "This invitation is unavailable or has expired. If you already accepted it, sign in to resume setup. Otherwise, ask your organization administrator for a new invitation.";

    private Task<TenantAdministratorInvitation?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        control.AdministratorInvitations.Include(item => item.Tenant).SingleOrDefaultAsync(item => item.Id == id
            && item.TenantId == tenantScope.TenantId && !item.Tenant.IsDeleted && item.Tenant.Status == TenantStatus.Active, cancellationToken);

    private bool IsValid(TenantAdministratorInvitation? item, string token) => item is not null
        && item.AcceptanceStatus == InvitationAcceptanceStatus.Pending && item.ExpiresAt > timeProvider.GetUtcNow()
        && SingleUseToken.Matches(item.TokenHash, token);

    public async Task<AppResponse<AdministratorInvitationPreview>> InspectAsync(InspectAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);

        if (!IsValid(item, request.Token)) return AppResponses.Failure<AdministratorInvitationPreview>(Unavailable);
        var existing = await iam.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == item!.NormalizedEmail, cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new AdministratorInvitationPreview(item!.Tenant.DisplayName, item.Email, item.FirstName, existing, item.ExpiresAt!.Value));
    }

    public async Task<AppResponse<LoginResponse>> AcceptPasswordAsync(AcceptAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(request.InvitationId, cancellationToken).ConfigureAwait(false);

        if (!IsValid(item, request.Token)) return AppResponses.Failure<LoginResponse>(Unavailable);
        var user = await iam.Users.IgnoreQueryFilters().SingleOrDefaultAsync(user => user.NormalizedEmail == item!.NormalizedEmail, cancellationToken).ConfigureAwait(false);

        if (user is not null)
        {
            if (!CanUse(user, item!)) return AppResponses.Failure<LoginResponse>("Sign in with the account invited to this organization.");
            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true).ConfigureAwait(false);

            if (!result.Succeeded && !result.RequiresTwoFactor) return AppResponses.Failure<LoginResponse>("Your current password could not be verified. Use your existing sign-in method.");
        }
        else
        {
            if (item!.Tenant.MaxUsers > 0 && await iam.Users.IgnoreQueryFilters().CountAsync(user => user.TenantId == item.TenantId && !user.IsDeleted, cancellationToken).ConfigureAwait(false) >= item.Tenant.MaxUsers)
                return AppResponses.Failure<LoginResponse>("This organization has reached its account limit. Contact its administrator.");
            user = NewUser(item);
            var created = await users.CreateAsync(user, request.Password).ConfigureAwait(false);

            if (!created.Succeeded) return AppResponses.Failure<LoginResponse>("Could not set up your account. Use a password that meets the password requirements, or sign in if your account already exists.");
        }
        var claimed = await ClaimAsync(item!, user, cancellationToken).ConfigureAwait(false);
        return claimed.IsSuccess ? await responses.CreateAsync(user, cancellationToken).ConfigureAwait(false) : AppResponses.Failure<LoginResponse>(claimed.Message ?? "An error occurred while accepting the invitation.");
    }

    public async Task<AppResponse<string>> AcceptExternalAsync(Guid invitationId, string token, string provider, string providerKey,
        string verifiedEmail, CancellationToken cancellationToken)
    {
        var item = await FindAsync(invitationId, cancellationToken).ConfigureAwait(false);

        if (!IsValid(item, token) || !string.Equals(item!.Email, verifiedEmail, StringComparison.OrdinalIgnoreCase))
            return AppResponses.Failure<string>(Unavailable);
        var linked = await users.FindByLoginAsync(provider, providerKey).ConfigureAwait(false);
        var user = await iam.Users.IgnoreQueryFilters().SingleOrDefaultAsync(user => user.NormalizedEmail == item.NormalizedEmail, cancellationToken).ConfigureAwait(false);

        if (user is not null && (linked?.Id != user.Id || !CanUse(user, item)))
            return AppResponses.Failure<string>("This account already exists. Sign in with its current method before accepting the invitation.");

        if (linked is not null && linked.Id != user?.Id) return AppResponses.Failure<string>("Use the identity invited to this organization.");

        if (user is null)
        {
            if (item.Tenant.MaxUsers > 0 && await iam.Users.IgnoreQueryFilters().CountAsync(account => account.TenantId == item.TenantId && !account.IsDeleted, cancellationToken).ConfigureAwait(false) >= item.Tenant.MaxUsers)
                return AppResponses.Failure<string>("This organization has reached its account limit.");
            user = NewUser(item);
            // Account and external credential are committed together; a failed provider link leaves no usable identity.
            var strategy = iam.Database.CreateExecutionStrategy();
            var created = await strategy.ExecuteAsync(async () =>
            {
                var transaction = await iam.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                var configuredAsyncDisposable = transaction.ConfigureAwait(false);
                await using var transactionLifetime = configuredAsyncDisposable;

                if (!(await users.CreateAsync(user).ConfigureAwait(false)).Succeeded) return false;

                if (!(await users.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider)).ConfigureAwait(false)).Succeeded) return false;
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);

            if (!created) return AppResponses.Failure<string>("Could not establish your external sign-in. Please try again.");
        }
        return await ClaimAsync(item, user, cancellationToken).ConfigureAwait(false);
    }

    private static bool CanUse(AppUser user, TenantAdministratorInvitation item) => !user.IsDeleted && user.IsActive
        && !user.IsPlatformStaff && user.TenantId == item.TenantId && (!user.RequiresOnboarding || user.OnboardingInvitationId == item.Id);

    private AppUser NewUser(TenantAdministratorInvitation item)
    {
        var user = AppUser.Create(timeProvider.GetUtcNow(), item.TenantId, null, item.Email, item.FirstName, item.LastName, item.Email, string.Empty, item.CreatedBy);
        user.IsActive = true;
        user.EmailConfirmed = true;
        user.RequirePasswordChange = false;
        user.BeginOnboarding(item.Id);
        return user;
    }

    private async Task<AppResponse<string>> ClaimAsync(TenantAdministratorInvitation item, AppUser user, CancellationToken cancellationToken)
    {
        user.BeginOnboarding(item.Id);

        if (!(await users.UpdateAsync(user).ConfigureAwait(false)).Succeeded) return AppResponses.Failure<string>("Could not save account setup. Please try again.");

        if (!item.Claim(user.Id, timeProvider.GetUtcNow())) return AppResponses.Failure<string>(Unavailable);

        try { await controlUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false); }
        catch (DbUpdateConcurrencyException) { return AppResponses.Failure<string>("This invitation changed while you were accepting it. Sign in to resume or request a new invitation."); }
        return AppResponses.Success("Invitation accepted. Complete your account security to continue.", user.Id);
    }

    public async Task<AppResponse<bool>> CompleteAsync(string userId, CancellationToken cancellationToken, bool mfaVerified = false)
    {
        var user = await users.FindByIdAsync(userId).ConfigureAwait(false);

        if (user is null || !user.IsActive || user.IsDeleted || user.TenantId != tenantScope.TenantId)
            return AppResponses.Failure<bool>("Account setup is unavailable.");

        if (!user.RequiresOnboarding) return AppResponses.Success("Account setup is complete.", true);

        if (user.IsPlatformStaff) return AppResponses.Failure<bool>("Account setup is unavailable.");
        var item = await FindAsync(user.OnboardingInvitationId!.Value, cancellationToken).ConfigureAwait(false);

        if (item is null || !string.Equals(item.ClaimedUserId, user.Id, StringComparison.Ordinal)
            || item.AcceptanceStatus is not (InvitationAcceptanceStatus.SettingUp or InvitationAcceptanceStatus.Accepted))
            return AppResponses.Failure<bool>(Unavailable);

        if (!user.TwoFactorEnabled) return AppResponses.Failure<bool>("Set up and verify your authenticator before continuing.");

        if (!mfaVerified) return AppResponses.Failure<bool>(AppError.Forbidden("Verify your authenticator again before finishing account setup."));
        var role = await iam.Roles.IgnoreQueryFilters().Where(role => role.IsDefault && role.DefinitionKey == "tenant-administrator" && role.IsActive && !role.IsDeleted && !role.IsPlatformRole)
            .Select(role => role.Name).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (role is null) return AppResponses.Failure<bool>("Administrator access is not ready. Contact support.");

        if (!await users.IsInRoleAsync(user, role).ConfigureAwait(false)
            && !(await users.AddToRoleAsync(user, role).ConfigureAwait(false)).Succeeded)
            return AppResponses.Failure<bool>("Administrator access could not be completed. Please try again.");

        if (item.AcceptanceStatus != InvitationAcceptanceStatus.Accepted)
        {
            item.Accept(user.Id, timeProvider.GetUtcNow());

            try { await controlUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false); }
            catch (DbUpdateConcurrencyException) { return AppResponses.Failure<bool>("This invitation changed. Please sign in again."); }
        }
        user.CompleteOnboarding(timeProvider.GetUtcNow());

        if (!(await users.UpdateAsync(user).ConfigureAwait(false)).Succeeded)
            return AppResponses.Failure<bool>("Account setup could not finish. Sign in again to resume.");
        return AppResponses.Success("Your administrator account is ready.", true);
    }
}
