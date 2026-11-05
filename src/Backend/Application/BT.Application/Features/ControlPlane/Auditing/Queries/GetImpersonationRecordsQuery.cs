using BT.Domain.Features.ControlPlane.Auditing.Enums;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using BT.Application.Contracts.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BT.SharedKernel.Features.ControlPlane.Auditing.Dtos;

namespace BT.Application.Features.ControlPlane.Auditing.Queries;

public record GetImpersonationRecordsQuery(bool ActiveOnly = false) : IRequest<AppResponse<IReadOnlyList<ImpersonationRecordDto>>>, ICachableRequest
{
    public string CacheGroup => "auditing";
    public string Discriminator => $"impersonation_records_{ActiveOnly}";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}

internal sealed class GetImpersonationRecordsQueryHandler(
    TimeProvider timeProvider,
    IControlPlaneUnitOfWork unitOfWork,
    ICurrentActorProvider actorProvider)
    : IRequestHandler<GetImpersonationRecordsQuery, AppResponse<IReadOnlyList<ImpersonationRecordDto>>>
{
    public async Task<AppResponse<IReadOnlyList<ImpersonationRecordDto>>> Handle(GetImpersonationRecordsQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var query = await unitOfWork.ImpersonationRecords.ListAsync(
            q => request.ActiveOnly
                ? q.Where(x => x.Status == ImpersonationRecordStatus.Active && x.ExpiryTime > now)
                : q,
            cancellationToken).ConfigureAwait(false);

        var dtos = query.Select(x => new ImpersonationRecordDto(
            x.Id,
            x.ActorId,
            x.ActorName,
            x.TargetTenantId,
            x.TargetTenantName,
            x.Reason,
            x.StartTime,
            x.ExpiryTime,
            x.Status.ToString()
        )).ToList();

        return AppResponses.Success<IReadOnlyList<ImpersonationRecordDto>>(dtos);
    }
}
