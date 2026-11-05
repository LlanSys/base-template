using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.HR.Employees.Dtos;

namespace BT.UI.Blazor.Features.HR.Employees.Contracts.Interfaces;

internal interface IEmployeeService
{
    Task<AppResponse<PagedResponse<EmployeeResponse, Guid>>> SearchAsync(EmployeeSearchRequest request);

    Task<AppResponse<EmployeeResponse>> GetByIdAsync(Guid id);

    Task<AppResponse<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request);

    Task<AppResponse<EmployeeResponse>> UpdateAsync(Guid id, UpdateEmployeeRequest request);

    Task<AppResponse<bool>> DeleteAsync(Guid id);
}
