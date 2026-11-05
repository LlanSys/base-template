using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.HR.Employees.Entities;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Features.HR.Employees.Enums;
using BT.Domain.Features.IAM.Users.Enums;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using BT.Infrastructure.Features.IAM.Authorization;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class CreateAppUser(
    TimeProvider timeProvider,
    UserManager<AppUser> userManager,
    IBankingUnitOfWork bankingUnitOfWork,
    IHrUnitOfWork hrUnitOfWork,
    IIamUnitOfWork iamUnitOfWork,
    ILogger<CreateAppUser> logger,
    IamAccessBoundary boundary) : IRequestHandler<CreateAppUserCommand, AppResponse<AppUserResponse>>
{
    public async Task<AppResponse<AppUserResponse>> Handle(CreateAppUserCommand command, CancellationToken ct)
    {
        var req = command.Request;
        AppUser? createdUser = null;

        try
        {
            if (!await boundary.CanAssignRolesAsync(command.UserId, req.Roles ?? [], ct).ConfigureAwait(false))
                return AppResponses.Failure<AppUserResponse>(AppError.Forbidden("You cannot assign these roles."));
            var actor = await boundary.GetActorAsync(command.UserId, ct).ConfigureAwait(false);

            if (req.EmployeeId.HasValue && req.CustomerId.HasValue)
                return AppResponses.Failure<AppUserResponse>("A user account can be linked to either an employee or a customer, not both.");

            Employee? employee = null;

            if (req.EmployeeId.HasValue)
            {
                employee = await hrUnitOfWork.EmployeeRepository
                    .FindByCondition(e => e.Id == req.EmployeeId.Value)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (employee is null)
                    return AppResponses.Failure<AppUserResponse>("The specified employee does not exist or has been deactivated.");

                var alreadyLinked = await userManager.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.EmployeeId == req.EmployeeId.Value, ct)
                    .ConfigureAwait(false);

                if (alreadyLinked)
                    return AppResponses.Failure<AppUserResponse>("A user account already exists for this employee.");
            }

            Customer? customer = null;

            if (req.CustomerId.HasValue)
            {
                customer = await bankingUnitOfWork.CustomerRepository
                    .FindByCondition(c => c.Id == req.CustomerId.Value)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (customer is null)
                    return AppResponses.Failure<AppUserResponse>("The specified customer does not exist or has been deactivated.");

                var alreadyLinked = await userManager.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.CustomerId == req.CustomerId.Value, ct)
                    .ConfigureAwait(false);

                if (alreadyLinked)
                    return AppResponses.Failure<AppUserResponse>("A user account already exists for this customer.");
            }

            var emailOrUsernameExists = await userManager.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserName == req.Username || u.Email == req.Email, ct)
                .ConfigureAwait(false);

            if (emailOrUsernameExists)
                return AppResponses.Failure<AppUserResponse>("An account with this username or email already exists.");

            var appUser = employee is not null
                ? AppUser.CreateForEmployee(
                    timeProvider.GetUtcNow(),
                    Guid.Empty,
                    employee.Id,
                    req.Username,
                    employee.FirstName,
                    employee.LastName,
                    employee.Email,
                    employee.PhoneNumber,
                    req.IdNumber ?? employee.IdNumber,
                    createdBy: "System")
                : customer is not null
                    ? AppUser.CreateForCustomer(
                        timeProvider.GetUtcNow(),
                        Guid.Empty,
                        customer.Id,
                        req.Username,
                        req.FirstName,
                        req.LastName,
                        req.Email,
                        req.PhoneNumber ?? string.Empty,
                        req.IdNumber ?? string.Empty,
                        createdBy: "System")
                    : AppUser.Create(
                    timeProvider.GetUtcNow(),
                    Guid.Empty,
                    employeeId: null,
                    req.Username,
                    req.FirstName,
                    req.LastName,
                    req.Email,
                    req.PhoneNumber ?? string.Empty,
                    createdBy: "System");

            appUser.SetIdentityProfile(
                req.IdNumber ?? appUser.NationalId,
                Enum.TryParse<Gender>(req.Gender, true, out var g) ? g : Gender.Other);

            appUser.TenantId = actor.TenantId;
            appUser.IsPlatformStaff = actor.IsPlatformStaff;
            appUser.CreatedBy = command.UserId;

            var identityResult = await userManager
                .CreateAsync(appUser, req.Password)
                .ConfigureAwait(false);

            if (!identityResult.Succeeded)
            {
                var error = identityResult.Errors.First().Description;
                ServiceLogDefinitions.LogAppUserCreationWarning(logger, req.Email, error);
                return AppResponses.Failure<AppUserResponse>(error);
            }

            createdUser = appUser;

            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                if (req.Roles?.Any() == true)
                {
                    var roleResult = await userManager
                        .AddToRolesAsync(appUser, req.Roles)
                        .ConfigureAwait(false);

                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException(
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                var profile = AppUserProfile.Create(
                    timeProvider.GetUtcNow(),
                    appUser.Id,
                    appUser.PhoneNumber,
                    appUser.PhoneNumber,
                    appUser.Email,
                    "System");

                await iamUnitOfWork.AppUserProfileRepository
                    .CreateOrUpdateAsync(appUser.Id, profile, ct)
                    .ConfigureAwait(false);

                return true;
            }).ConfigureAwait(false);

            appUser.RaiseAppUserCreatedEvent(timeProvider.GetUtcNow());

            _ = await userManager.GenerateEmailConfirmationTokenAsync(appUser).ConfigureAwait(false);

            ServiceLogDefinitions.LogAppUserCreated(logger, appUser.Id, appUser.Email ?? string.Empty);

            return AppResponses.Success("User created successfully.", appUser.ToAppUserResponse());
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogAppUserCreationFailed(logger, req.Email, ex);

            if (createdUser is not null)
            {
                try
                {
                    await userManager.DeleteAsync(createdUser).ConfigureAwait(false);
                    ServiceLogDefinitions.LogIdentityRollbackSucceeded(logger, req.Email);
                }
                catch (Exception)
                {
                    ServiceLogDefinitions.LogIdentityRollbackCritical(logger, createdUser.Id, req.Email);
                }
            }

            throw;
        }
    }
}
