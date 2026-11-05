using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Features.IAM.Users.Services;
using BT.Infrastructure.Security;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ExchangeSsoCode(IamDBContext iam, ICurrentTenantProvider tenantScope, UserManager<AppUser> users,
    SignInResponseFactory responses, TimeProvider timeProvider) : IRequestHandler<ExchangeSsoCodeCommand, AppResponse<LoginResponse>>
{
    public async Task<AppResponse<LoginResponse>> Handle(ExchangeSsoCodeCommand command, CancellationToken cancellationToken)
    {
        if (command.Code is not { Length: 64 }) return AppResponses.Failure<LoginResponse>("Invalid or expired sign-in code.");
        var hash = SingleUseToken.Hash(command.Code);
        var now = timeProvider.GetUtcNow();
        var code = await iam.Set<SsoAuthorizationCode>().AsNoTracking().SingleOrDefaultAsync(item => item.Hash == hash
            && item.TenantId == tenantScope.TenantId && item.ExpiresAt > now, cancellationToken).ConfigureAwait(false);

        if (code is null) return AppResponses.Failure<LoginResponse>("Invalid or expired sign-in code.");
        var removed = await iam.Set<SsoAuthorizationCode>().Where(item => item.Hash == hash && item.TenantId == tenantScope.TenantId && item.ExpiresAt > now)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        if (removed != 1) return AppResponses.Failure<LoginResponse>("Invalid or expired sign-in code.");
        var user = await users.FindByIdAsync(code.UserId).ConfigureAwait(false);

        if (user is null || user.TenantId != code.TenantId) return AppResponses.Failure<LoginResponse>("This account is unavailable.");
        return await responses.CreateAsync(user, cancellationToken).ConfigureAwait(false);
    }
}
