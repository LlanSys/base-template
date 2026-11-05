using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.Lookups.Dtos;

namespace BT.UI.Blazor.Features.Shared.Lookups.Contracts.Interfaces;

internal interface ILookupService
{
    Task<AppResponse<IReadOnlyList<LookupCatalogTypeResponse>>> GetCatalogTypesAsync();

    Task<AppResponse<IReadOnlyList<LookupResponse>>> GetAsync(string lookupType);

    Task<AppResponse<LookupResponse>> CreateAsync(string lookupType, CreateLookupRequest request);

    Task<AppResponse<LookupResponse>> UpdateAsync(string lookupType, int id, UpdateLookupRequest request);

    Task<AppResponse<bool>> DeleteAsync(string lookupType, int id);
}
