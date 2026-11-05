using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.IAM.Users.Queries;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetCurrentUserProfilePicture(
    UserManager<AppUser> userManager,
    IProfilePictureStorage storage,
    ILogger<GetCurrentUserProfilePicture> logger)
    : IRequestHandler<GetCurrentUserProfilePictureQuery, AppResponse<ProfilePictureFile>>
{
    public async Task<AppResponse<ProfilePictureFile>> Handle(
        GetCurrentUserProfilePictureQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return AppResponses.Failure<ProfilePictureFile>("Authenticated user was not resolved.");
        }

        var user = await userManager.FindByIdAsync(query.UserId).ConfigureAwait(false);

        if (user is null || user.ProfilePictureUrl is null)
        {
            return AppResponses.Failure<ProfilePictureFile>("Profile picture was not found.");
        }

        ProfilePictureFile? profilePicture;

        try
        {
            profilePicture = await storage
                .OpenReadAsync(user.ProfilePictureUrl, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            ServiceLogDefinitions.LogProfilePictureReadUnavailable(logger, user.Id, ex);
            return AppResponses.Failure<ProfilePictureFile>(
                AppError.DependencyUnavailable("Profile picture storage is temporarily unavailable."));
        }

        return profilePicture is null
            ? AppResponses.Failure<ProfilePictureFile>("Profile picture was not found.")
            : AppResponses.Success("Profile picture loaded.", profilePicture);
    }
}
