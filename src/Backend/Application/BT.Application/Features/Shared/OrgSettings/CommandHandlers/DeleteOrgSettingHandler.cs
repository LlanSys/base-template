using BT.Application.Contracts.Interfaces.Common;
using BT.Domain.Features.Shared.Contracts;
using BT.SharedKernel.Dtos.Common;

using MediatR;

using System.Net;

namespace BT.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class DeleteOrgSettingHandler(ISharedUnitOfWork unitOfWork)
    : IRequestHandler<DeleteOrgSettingCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.OrgSettingRepository.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (setting is null)
            return AppResponses.Failure<bool>("Tenant setting not found.");

        if (BT.SharedKernel.Features.Shared.OrgSettings.OrganizationSettingCatalog.Find(setting.Key) is null)
            return AppResponses.Failure<bool>("This setting is not a supported organization preference.");

        await unitOfWork.OrgSettingRepository.DeleteAsync(setting.Id, cancellationToken).ConfigureAwait(false);
        await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success("Organization setting reset to its default.", true);
    }
}
