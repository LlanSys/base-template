using BT.Domain.Features.Shared.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;
using BT.Application.Features.Shared.OrgSettings.Mappings;
using MediatR;

namespace BT.Application.Features.Shared.OrgSettings.QueryHandlers;

internal sealed class GetOrgSettingsHandler(ISharedUnitOfWork unitOfWork)
    : IRequestHandler<GetOrgSettingsQuery, AppResponse<IEnumerable<OrgSettingResponse>>>
{
    public async Task<AppResponse<IEnumerable<OrgSettingResponse>>> Handle(GetOrgSettingsQuery request, CancellationToken cancellationToken)
    {
        var keys = OrganizationSettingCatalog.Definitions.Select(definition => definition.Key).ToArray();
        var settings = await unitOfWork.OrgSettingRepository.ListAsync(query => query.Where(setting => keys.Contains(setting.Key)),
            cancellationToken).ConfigureAwait(false);

        var responses = OrganizationSettingCatalog.Definitions.Select(definition =>
        {
            var setting = settings.FirstOrDefault(setting => setting.Key == definition.Key);
            return setting is null ? new OrgSettingResponse(Guid.Empty, definition.Key, definition.DefaultValue, definition.Description,
                new DateTime(2026, 1, 1), "00000000-0000-0000-0000-000000000001", null, null) : setting.ToResponse();
        }).ToArray();

        return AppResponses.Success<IEnumerable<OrgSettingResponse>>("Organization settings loaded.", responses);
    }
}
