using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;

using MediatR;

namespace BT.Application.Features.Shared.OrgSettings.QueryHandlers;

public record GetOrgSettingByKeyQuery(string Key)
    : IRequest<AppResponse<OrgSettingResponse>>, ICachableRequest
{
    public string CacheGroup => "tenant-settings";
    public string Discriminator => Key;
    public string? CacheUserId => null;
    public bool IsVersioned => true;

    public bool BypassCache => true;
}
