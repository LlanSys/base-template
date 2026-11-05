namespace BT.Domain.Shared.Contracts.Settings;

public class PasskeySettings
{
    public const string SectionName = "Passkeys";

    public bool Enabled { get; set; }
    public string ServerDomain { get; set; } = string.Empty;
    public string ServerName { get; set; } = "BaseTemplate";
    public string[] Origins { get; set; } = [];
}
