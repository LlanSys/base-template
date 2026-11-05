using BT.Domain.Features.Shared.Contracts;
using BT.Domain.Features.Shared.OrgSettings.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;
using BT.Application.Features.Shared.OrgSettings.Mappings;
using MediatR;

namespace BT.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class UpdateOrgSettingHandler(ISharedUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOrgSettingCommand, AppResponse<OrgSettingResponse>>
{
    public async Task<AppResponse<OrgSettingResponse>> Handle(UpdateOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var definition = OrganizationSettingCatalog.Find(request.Request.Key);
        var value = request.Request.Value.Trim();

        if (definition is null || !OrganizationSettingCatalog.IsValid(definition.Key, value))
            return AppResponses.Failure<OrgSettingResponse>("Choose a supported setting and a valid value.");
        var setting = await unitOfWork.OrgSettingRepository.FirstOrDefaultAsync(
            setting => setting.Key == definition.Key, cancellationToken).ConfigureAwait(false);

        if (setting is not null && setting.Id != request.Request.Id && request.Request.Id != Guid.Empty)
            return AppResponses.Failure<OrgSettingResponse>("The setting changed. Reload before saving.");

        if (setting is null)
        {
            setting = new OrgSetting(definition.Key, value, request.UserId, definition.Description);
            await unitOfWork.OrgSettingRepository.CreateAsync(setting, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            setting.Value = value;
            setting.Description = definition.Description;
            await unitOfWork.OrgSettingRepository.UpdateAsync(setting, cancellationToken).ConfigureAwait(false);
        }
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success("Organization setting saved.", setting.ToResponse());
    }
}
