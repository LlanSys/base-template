using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.HR.Employees.Enums;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Features.IAM.Authorization;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateAdminUser(TimeProvider timeProvider, UserManager<AppUser> userManager, ILogger<UpdateAdminUser> logger,
    IamDBContext context,
    IamAccessBoundary boundary)
    : IRequestHandler<UpdateAdminUserCommand, AppResponse<AdminUserListResponse>>
{
    public Task<AppResponse<AdminUserListResponse>> Handle(UpdateAdminUserCommand command, CancellationToken cancellationToken)
        => IdentityWriteScope.RunAsync(context,
            () => HandleCore(command, cancellationToken), cancellationToken);

    private async Task<AppResponse<AdminUserListResponse>> HandleCore(UpdateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var req = command.Request;
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<AdminUserListResponse>("User not found.");
            }

            var gender = Enum.TryParse<Gender>(req.Gender, true, out var parsedGender) ? parsedGender : Gender.Other;

            if (!await boundary.CanAccessUserAsync(command.UpdatedBy, user, cancellationToken).ConfigureAwait(false)
                || !await boundary.CanAssignRolesAsync(command.UpdatedBy, req.Roles, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<AdminUserListResponse>(AppError.Forbidden("You cannot update users or assignments outside your delegated scope."));
            user.UpdateAdminProfile(
                timeProvider.GetUtcNow(),
                req.UserName,
                req.Email,
                req.FirstName,
                req.LastName,
                req.PhoneNumber,
                req.IdNumber,
                gender,
                command.UpdatedBy);

            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);

            if (!updateResult.Succeeded)
            {
                return AppResponses.Failure<AdminUserListResponse>(string.Join(", ", updateResult.Errors.Select(static error => error.Description)));
            }

            if (!await boundary.SetUserRolesAsync(user, req.Roles.ToArray(), command.UpdatedBy, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<AdminUserListResponse>("The selected roles changed. Reload and try again.");

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var response = new AdminUserListResponse(
                user.Id,
                user.UserName ?? string.Empty,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt,
                user.EmployeeId,
                user.CustomerId,
                [.. roles.Order(StringComparer.OrdinalIgnoreCase)]);

            return AppResponses.Success("User updated.", response);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
