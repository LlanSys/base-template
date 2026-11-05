using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Contracts.Repositories;
using BT.Domain.Features.HR.Departments.Contracts.Repositories;
using BT.Domain.Features.HR.Employees.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Contracts.Repositories;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Persistence.Common;
using BT.Persistence.Features.HR.DataContext;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.HR;

public sealed class HrUnitOfWork(
    TimeProvider timeProvider,
    HrDBContext context,
    IDepartmentRepository departmentRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeNumberSequenceRepository employeeNumberSequenceRepository,
    IPublisher publisher,
    ILogger<HrUnitOfWork> logger
) : BaseUnitOfWork<HrDBContext>(context, publisher, logger, timeProvider), IHrUnitOfWork
{
    public IDepartmentRepository DepartmentRepository { get; } = departmentRepository;
    public IEmployeeRepository EmployeeRepository { get; } = employeeRepository;
    public IEmployeeNumberSequenceRepository EmployeeNumberSequenceRepository { get; } = employeeNumberSequenceRepository;
}
