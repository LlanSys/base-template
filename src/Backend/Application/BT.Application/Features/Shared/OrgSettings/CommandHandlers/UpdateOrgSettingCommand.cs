using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;

using MediatR;

using System.Collections.Generic;

namespace BT.Application.Features.Shared.OrgSettings.CommandHandlers;

public record UpdateOrgSettingCommand(UpdateOrgSettingRequest Request, string UserId)
    : IRequest<AppResponse<OrgSettingResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("tenant-settings")
    ];
}
