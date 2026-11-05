using BT.SharedKernel.Extensions;
using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Features.IAM.Users.Enums;
using BT.SharedKernel.Features.Banking.Customers.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class LinkCustomerToExistingUser(TimeProvider timeProvider, UserManager<AppUser> userManager)
    : IRequestHandler<LinkCustomerToExistingUserCommand, AppResponse<CustomerResponse>>
{
    public async Task<AppResponse<CustomerResponse>> Handle(LinkCustomerToExistingUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.AppUserId).ConfigureAwait(false);

        if (user is null)
            return AppResponses.Failure<CustomerResponse>("User not found.");

        if (user.CustomerId.HasValue)
            return AppResponses.Failure<CustomerResponse>("User is already linked to a customer record.");

        user.LinkToCustomer(timeProvider.GetUtcNow(), command.CustomerId);

        await userManager.UpdateAsync(user).ConfigureAwait(false);
        await userManager.AddToRoleAsync(user, Roles.Customer.ToDisplayString()).ConfigureAwait(false);

        return AppResponses.Success<CustomerResponse>("Customer account linked to user.", default!);
    }
}
