using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;
namespace BT.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IExternalAccountLinkService
{
    Task<AppResponse<string>> CreateAsync(string userId, ConnectExternalIdentityRequest request, CancellationToken cancellationToken);
    Task<AppResponse<string>> ValidateAsync(string code, string provider, CancellationToken cancellationToken);
    Task<AppResponse<string>> ConnectAsync(string code, string provider, string providerKey, CancellationToken cancellationToken);
}
