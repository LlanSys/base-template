namespace BT.SharedKernel.Features.ControlPlane.Auditing.Dtos;

public sealed record AdministrationAuditResponse(Guid Id, DateTimeOffset OccurredAt, string Actor, string Area, string Action, string ChangedFields);
