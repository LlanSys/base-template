namespace BT.SharedKernel.Features.ControlPlane.Modules;

public sealed record ModuleDefinitionRequest(string Key, string Name, string Description, bool IsActive, Guid Revision);
