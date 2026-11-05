using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Menus.Commands;



internal sealed class DeleteMenuCommandHandler(TimeProvider timeProvider, IIamUnitOfWork unitOfWork, ILogger<DeleteMenuCommandHandler> logger)
    : IRequestHandler<DeleteMenuCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteMenuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var menu = await unitOfWork.MenuRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);

            if (menu is null)
            {
                return AppResponses.Failure<bool>($"Menu {command.Id} not found.");
            }

            var hasChildren = await unitOfWork.MenuRepository
                .AnyAsync(child => child.ParentId == command.Id, cancellationToken)
                .ConfigureAwait(false);

            if (hasChildren)
            {
                return AppResponses.Failure<bool>("This menu has child menu items. Move or delete the children first.");
            }

            menu.MarkAsDeleted(command.UserId, timeProvider.GetUtcNow());
            await unitOfWork.MenuRepository.UpdateAsync(menu, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved ? AppResponses.Success("Menu deleted.", true) : AppResponses.Failure<bool>("Menu delete failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(DeleteMenuCommandHandler), ex);
            throw;
        }
    }
}
