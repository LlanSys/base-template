using System.ComponentModel.DataAnnotations;

namespace BT.Infrastructure.Configuration;

public sealed class AdministratorInvitationSettings
{
    public const string SectionName = "AdministratorInvitations";
    [Range(1, 72)]
    public int ValidityHours { get; init; } = 48;
}
