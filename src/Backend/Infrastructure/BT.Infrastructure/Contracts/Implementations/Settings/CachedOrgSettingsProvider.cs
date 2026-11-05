using System.Text.Json;
using BT.Domain.Features.Shared.Contracts;
using BT.Domain.Shared.Contracts.Settings;
using BT.SharedKernel.Features.Shared.OrgSettings;

namespace BT.Infrastructure.Contracts.Implementations.Settings;

/// <summary>Request-scoped supported preferences. No technical configuration or secrets can be overridden.</summary>
public sealed class CachedOrgSettingsProvider(ISharedUnitOfWork unitOfWork) : IOrgSettingsProvider
{
    private Dictionary<string, string>? _values;

    public async Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        var definition = OrganizationSettingCatalog.Find(key);

        if (definition is null) return null;

        if (_values is null)
        {
            var keys = OrganizationSettingCatalog.Definitions.Select(definition => definition.Key).ToArray();
            var settings = await unitOfWork.OrgSettingRepository.ListAsync(
                query => query.Where(setting => keys.Contains(setting.Key)), ct).ConfigureAwait(false);

            _values = settings.ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);
        }
        return _values.TryGetValue(key, out var value) && OrganizationSettingCatalog.IsValid(key, value) ? value : definition.DefaultValue;
    }

    public async Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await GetSettingAsync(key, ct).ConfigureAwait(false);

        if (value is null) return default;

        if (typeof(T) == typeof(string)) return (T)(object)value;
        return JsonSerializer.Deserialize<T>(value);
    }
}
