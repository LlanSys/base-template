using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace BT.UI.Blazor.Features.ControlPlane.Tenants.Contracts;

internal interface ITenantService
{
    Task<AppResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> SetModulesAsync(Guid id, SetTenantModulesRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<List<TenantResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> SuspendAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> ApproveKYCAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> AddModuleAsync(Guid id, AddTenantModuleRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<TenantResponse>> RemoveModuleAsync(Guid id, string moduleKey, CancellationToken cancellationToken = default);
}
