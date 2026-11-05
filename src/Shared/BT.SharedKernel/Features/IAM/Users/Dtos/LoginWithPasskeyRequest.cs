using System.Text.Json;

namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record LoginWithPasskeyRequest(string? Username, Guid CorrelationId, JsonElement AssertionResponse);
