namespace BT.SharedKernel.Features.ControlPlane.Modules;

public sealed record ModuleDefinitionResponse(Guid Id, string Key, string Name, string Description, bool IsActive, Guid Revision, IReadOnlyList<string>? ReadinessIssues = null);
