using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;
using MediatR;

namespace BT.Application.Features.Shared.OrgSettings.CommandHandlers;

internal sealed class CreateOrgSettingHandler(ISender sender) : IRequestHandler<CreateOrgSettingCommand, AppResponse<OrgSettingResponse>>
{
    public Task<AppResponse<OrgSettingResponse>> Handle(CreateOrgSettingCommand request, CancellationToken cancellationToken)
        => sender.Send(new UpdateOrgSettingCommand(new UpdateOrgSettingRequest(Guid.Empty,
            request.Request.Key, request.Request.Value, request.Request.Description), request.UserId), cancellationToken);
}
