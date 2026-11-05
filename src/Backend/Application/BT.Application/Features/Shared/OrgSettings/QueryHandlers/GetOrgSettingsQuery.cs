using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;

using MediatR;

using System.Collections.Generic;

namespace BT.Application.Features.Shared.OrgSettings.QueryHandlers;

public record GetOrgSettingsQuery
    : IRequest<AppResponse<IEnumerable<OrgSettingResponse>>>, ICachableRequest
{
    public string CacheGroup => "tenant-settings";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;

    public bool BypassCache => true;
}
