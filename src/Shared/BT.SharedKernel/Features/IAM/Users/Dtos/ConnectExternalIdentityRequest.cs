namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record ConnectExternalIdentityRequest(string Provider, string CurrentPassword);
