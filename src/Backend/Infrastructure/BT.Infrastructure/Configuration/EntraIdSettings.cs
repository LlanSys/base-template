namespace BT.Infrastructure.Configuration;

public sealed class EntraIdSettings
{
    public const string SectionName = "EntraId";

    public bool Enabled { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    // Retained for configuration compatibility. Automatic email-only linking is unsupported.
    public bool AutoLinkByVerifiedEmail { get; set; }

    public string Authority =>
        string.IsNullOrWhiteSpace(TenantId)
            ? string.Empty
            : $"https://login.microsoftonline.com/{TenantId.Trim()}";
}
