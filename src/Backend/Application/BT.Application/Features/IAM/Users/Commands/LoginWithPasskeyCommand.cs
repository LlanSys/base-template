using System.Text.Json;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;

namespace BT.Application.Features.IAM.Users.Commands;

public sealed record LoginWithPasskeyCommand(string? Username, Guid CorrelationId, JsonElement AssertionResponse) : IRequest<AppResponse<LoginResponse>>;
