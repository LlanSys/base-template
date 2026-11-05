using BT.Application.Features.IAM.Users.Commands;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.SharedKernel.Dtos.Common;

using MediatR;
namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ConnectExternalIdentityCommandHandler(IExternalAccountLinkService links) : IRequestHandler<ConnectExternalIdentityCommand, AppResponse<string>>
{
    public Task<AppResponse<string>> Handle(ConnectExternalIdentityCommand request, CancellationToken cancellationToken)
        => links.CreateAsync(request.UserId, request.Request, cancellationToken);
}
