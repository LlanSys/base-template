using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Contracts.Repositories;
using BT.Domain.Features.HR.Employees.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Contracts.Repositories;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Persistence.Common;
using BT.Persistence.Features.Banking.DataContext;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.Banking;

public sealed class BankingUnitOfWork(
    TimeProvider timeProvider,
    BankingDBContext context,
    ICustomerRepository customerRepository,
    IPublisher publisher,
    ILogger<BankingUnitOfWork> logger
) : BaseUnitOfWork<BankingDBContext>(context, publisher, logger, timeProvider), IBankingUnitOfWork
{
    public ICustomerRepository CustomerRepository { get; } = customerRepository;
}
