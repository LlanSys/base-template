using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.SharedKernel.Features.HR.Employees.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.HR.Employees.QueryHandlers;

// ── Get Active Employees (for RM dropdown) ────────────────────────────────




internal sealed class GetEmployeesQueryHandler(IHrUnitOfWork _hrUnitOfWork, ILogger<GetEmployeesQueryHandler> _logger)
    : IRequestHandler<GetEmployeesQuery, AppResponse<List<EmployeeResponse>>>
{
    public async Task<AppResponse<List<EmployeeResponse>>> Handle(GetEmployeesQuery query, CancellationToken ct)
    {
        try
        {
            var staff = await _hrUnitOfWork.EmployeeRepository.ListAsync(ct: ct).ConfigureAwait(false);
            var departmentIds = staff.Select(static employee => employee.DepartmentId).Distinct().ToArray();
            var departmentRows = await _hrUnitOfWork.DepartmentRepository
                .ListAsync(
                    departments => departments
                        .Where(department => departmentIds.Contains(department.Id))
                        .Select(static department => new { department.Id, department.Name }),
                    ct)
                .ConfigureAwait(false);

            var departments = departmentRows.ToDictionary(static department => department.Id, static department => department.Name);
            var managerIds = staff
                .Select(static employee => employee.ManagerId)
                .Where(static managerId => managerId.HasValue)
                .Select(static managerId => managerId!.Value)
                .Distinct()
                .ToArray();

            var managers = managerIds.Length == 0
                ? new Dictionary<Guid, string>()
                : (await _hrUnitOfWork.EmployeeRepository
                    .ListAsync(
                        employees => employees
                            .Where(manager => managerIds.Contains(manager.Id))
                            .Select(static manager => new { manager.Id, Name = $"{manager.FirstName} {manager.LastName}" }),
                        ct)
                    .ConfigureAwait(false))
                    .ToDictionary(static manager => manager.Id, static manager => manager.Name);

            var mapped = staff
                .Select(employee => employee.ToEmployeeResponse(
                    departments.GetValueOrDefault(employee.DepartmentId, string.Empty),
                    employee.ManagerId.HasValue ? managers.GetValueOrDefault(employee.ManagerId.Value, string.Empty) : string.Empty))
                .ToList();

            return AppResponses.Success($"Success", mapped);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(_logger, ex);
            throw;
        }
    }
}
