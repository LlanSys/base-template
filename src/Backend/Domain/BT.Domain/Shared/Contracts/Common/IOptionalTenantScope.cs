namespace BT.Domain.Shared.Contracts.Common;

public interface IOptionalTenantScope
{
    bool IsGlobalDefinition { get; }
}
