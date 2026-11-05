using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;

using MediatR;
namespace BT.Application.Features.Shared.OrgSettings.QueryHandlers;

internal sealed class GetOrgSettingByKeyHandler(ISender sender)
    : IRequestHandler<GetOrgSettingByKeyQuery, AppResponse<OrgSettingResponse>>
{
    public async Task<AppResponse<OrgSettingResponse>> Handle(GetOrgSettingByKeyQuery request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetOrgSettingsQuery(), cancellationToken).ConfigureAwait(false);
        var setting = response.Data?.FirstOrDefault(setting => string.Equals(setting.Key, request.Key, StringComparison.Ordinal));
        return setting is null ? AppResponses.NotFound<OrgSettingResponse>("Supported organization setting not found.") : AppResponses.Success(setting);
    }
}
