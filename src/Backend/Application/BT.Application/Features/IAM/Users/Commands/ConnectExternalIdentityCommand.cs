using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;

using MediatR;
namespace BT.Application.Features.IAM.Users.Commands;

public sealed record ConnectExternalIdentityCommand(string UserId, ConnectExternalIdentityRequest Request) : IRequest<AppResponse<string>>;
