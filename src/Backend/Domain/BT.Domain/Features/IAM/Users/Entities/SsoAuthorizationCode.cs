namespace BT.Domain.Features.IAM.Users.Entities;

public sealed class SsoAuthorizationCode
{
    public required string Hash { get; init; }
    public required string UserId { get; init; }
    public Guid TenantId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
