namespace BT.Application.Features.IAM.Authorization;

public interface IEffectiveAccessService
{
    Task<EffectiveAccess> GetAsync(string userId, CancellationToken cancellationToken);
}
