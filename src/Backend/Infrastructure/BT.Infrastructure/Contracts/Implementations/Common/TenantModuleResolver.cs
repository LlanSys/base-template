using BT.Application.Features.IAM.Authorization;
using BT.Domain.Shared.Contracts.Common;

namespace BT.Infrastructure.Contracts.Implementations.Common;

internal sealed class TenantModuleResolver(IEffectiveAccessService access, ICurrentActorProvider actor) : ITenantModuleResolver
{
    public async Task<IReadOnlyList<string>> GetEnabledModulesAsync(CancellationToken cancellationToken = default)
        => (await access.GetAsync(actor.ActorId, cancellationToken).ConfigureAwait(false)).Modules.ToArray();
}
