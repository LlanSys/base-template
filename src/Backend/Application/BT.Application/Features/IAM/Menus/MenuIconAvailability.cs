using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Features.IAM.Menus;

namespace BT.Application.Features.IAM.Menus;

internal static class MenuIconAvailability
{
    internal static async Task<bool> IsAvailableAsync(IIamUnitOfWork unitOfWork, string icon, CancellationToken cancellationToken)
    {
        var key = icon.Trim();

        if (!MenuIconCatalog.Contains(key))
        {
            return await unitOfWork.MenuIconRepository
                .AnyAsync(item => item.IsActive && item.Key == key, cancellationToken)
                .ConfigureAwait(false);
        }

        var disabledKeys = await unitOfWork.MenuIconRepository
            .ListAsync(items => items.Where(static item => !item.IsActive).Select(static item => item.Key), cancellationToken)
            .ConfigureAwait(false);

        return !disabledKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
    }
}
