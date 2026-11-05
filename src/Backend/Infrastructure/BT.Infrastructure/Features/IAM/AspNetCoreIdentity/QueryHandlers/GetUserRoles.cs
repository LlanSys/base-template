using BT.Application.Features.IAM.Users.Queries;
using BT.Domain.Features.IAM.Users.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetUserRoles(UserManager<AppUser> userManager,
    Authorization.IamAccessBoundary boundary)
    : IRequestHandler<GetUserRolesQuery, AppResponse<UserRolesResponse>>
{
    public async Task<AppResponse<UserRolesResponse>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);

        if (user is null)
        {
            return AppResponses.Failure<UserRolesResponse>("User not found.");
        }

        if (!await boundary.CanAccessUserAsync(request.RequestedBy, user, cancellationToken).ConfigureAwait(false))
            return AppResponses.Failure<UserRolesResponse>(AppError.Forbidden("User is outside your administrative scope."));

        var roles = (await userManager.GetRolesAsync(user).ConfigureAwait(false))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return AppResponses.Success("User roles loaded.", new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, roles));
    }
}
