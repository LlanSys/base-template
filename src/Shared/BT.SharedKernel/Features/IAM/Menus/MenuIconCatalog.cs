using System.Collections.Frozen;

namespace BT.SharedKernel.Features.IAM.Menus;

public static class MenuIconCatalog
{
    private static readonly Lazy<FrozenSet<string>> MaterialFilledKeys = new(LoadKeys);

    public static IReadOnlySet<string> Keys => MaterialFilledKeys.Value;

    public static bool Contains(string? key)
    {
        return key is not null && Keys.Contains(key.Trim());
    }

    private static FrozenSet<string> LoadKeys()
    {
        using var stream = typeof(MenuIconCatalog).Assembly.GetManifestResourceStream("BT.MenuIconKeys")
            ?? throw new InvalidOperationException("The MudBlazor icon key catalog is missing.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
