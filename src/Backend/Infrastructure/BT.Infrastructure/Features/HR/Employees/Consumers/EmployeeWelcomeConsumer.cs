using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.Domain.Shared.Contracts;
using BT.Infrastructure.Messaging.Consumers;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Infrastructure.Features.HR.Employees.Consumers;


internal sealed class EmployeeWelcomeConsumer(
    TimeProvider timeProvider,
    IEmailComposer<EmployeeCreatedIntegrationEvent> composer,
    ISharedUnitOfWork shared,
    ILogger<IntegrationEventEmailConsumer<EmployeeCreatedIntegrationEvent>> logger)
    : IntegrationEventEmailConsumer<EmployeeCreatedIntegrationEvent>(timeProvider, composer, shared, logger)
{

}
