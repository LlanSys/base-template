using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using BT.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

namespace BT.UI.Blazor.Features.IAM.Permissions;

internal static class PermissionCatalogLoader
{
    public static async Task<AppResponse<List<PermissionResponse>>> LoadAsync(
        IIamAdminService service,
        Guid? departmentId = null,
        bool forUserAssignment = false)
    {
        List<PermissionResponse> permissions = [];
        var cursors = new HashSet<Guid>();
        Guid? cursor = null;

        while (true)
        {
            var response = await service.SearchPermissionsAsync(
                new PermissionSearchRequest(DepartmentId: departmentId, Cursor: cursor, PageSize: 50),
                forUserAssignment)
                .ConfigureAwait(false);

            if (!response.IsSuccess || response.Data is null)
            {
                return AppResponses.Failure<List<PermissionResponse>>(response.Message ?? "Permissions could not be loaded.");
            }

            permissions.AddRange(response.Data.Items);

            if (!response.Data.HasNextPage)
            {
                return AppResponses.Success(permissions);
            }

            var next = response.Data.NextCursor;

            if (next == Guid.Empty || !cursors.Add(next))
            {
                return AppResponses.Failure<List<PermissionResponse>>("The permission catalog changed. Close this dialog and try again.");
            }

            cursor = next;
        }
    }
}
