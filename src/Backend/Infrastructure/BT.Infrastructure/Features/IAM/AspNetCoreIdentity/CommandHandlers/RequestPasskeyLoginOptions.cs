using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Users.Entities;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;

using BT.SharedKernel.Features.IAM.Users.Dtos;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RequestPasskeyLoginOptions(
    IPasskeyService passkeyService,
    UserManager<AppUser> userManager,
    IDistributedCache cacheService) : IRequestHandler<RequestPasskeyLoginOptionsCommand, AppResponse<PasskeyLoginOptionsResponse>>
{
    public async Task<AppResponse<PasskeyLoginOptionsResponse>> Handle(RequestPasskeyLoginOptionsCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = null;

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.ToUpperInvariant();
            user = await userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername || u.NormalizedEmail == normalizedUsername, cancellationToken).ConfigureAwait(false);

            if (user == null || !user.IsActive || user.IsDeleted)
            {
                return AppResponses.Failure<PasskeyLoginOptionsResponse>("Invalid login attempt.");
            }
        }

        var options = await passkeyService.RequestAssertionAsync(user?.UserName ?? string.Empty, cancellationToken).ConfigureAwait(false);
        var correlationId = System.Guid.NewGuid();
        var cacheKey = $"Fido2AssertionOptions:{correlationId}";

        await cacheService.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(options),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken).ConfigureAwait(false);

        var response = new PasskeyLoginOptionsResponse(options, correlationId);
        return AppResponses.Success("Login options generated.", response);
    }
}
