using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Banking.Customers.Dtos;

namespace BT.UI.Blazor.Features.Banking.Customers.Contracts.Interfaces;

internal interface ICustomerService
{
    Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> SearchAsync(CustomerSearchRequest request);
    Task<AppResponse<CustomerResponse>> GetByIdAsync(Guid id);
    Task<AppResponse<CustomerResponse>> CreateAsync(CreateCustomerRequest request);
    Task<AppResponse<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<AppResponse<bool>> DeleteAsync(Guid id);
}
