using BT.SharedKernel.Features.Shared.Notifications.Dtos;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.SharedKernel.Extensions;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Customers.Enums;
using BT.Domain.Features.Banking.Customers.Events;
using BT.Domain.Features.HR.Employees.Events;
using BT.Domain.Features.IAM.Users.Events;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Entities;
using BT.Infrastructure.Logging;
using BT.Infrastructure.Utilities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.EmailTemplates.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using BT.Infrastructure.Messaging.Consumers;

namespace BT.Infrastructure.Features.Banking.Customers.EmailComposers;


internal sealed class CustomerWelcomeEmailComposer(
    ICacheService _cache,
    ISharedUnitOfWork _sharedUnitOfWork,
    ILogger<CustomerWelcomeEmailComposer> _logger) : EmailComposer<CustomerCreatedIntegrationEvent>(_cache, _sharedUnitOfWork, _logger)
{
    public override async Task<AppResponse<ComposeEmailResponse>> ComposeAsync(CustomerCreatedIntegrationEvent evt, CancellationToken ct)
    {
        var templateType = evt.Type.ToEnum<CustomerType>() switch
        {
            CustomerType.Corporate => EmailTemplateType.Corporate,
            CustomerType.Institutional => EmailTemplateType.Institutional,
            CustomerType.SmallMediumEnterprise => EmailTemplateType.SmallMediumEnterprise,
            CustomerType.Individual => EmailTemplateType.IndividualWelcome,
            CustomerType.Enterprise => EmailTemplateType.EnterpriseWelcome,
            _ => EmailTemplateType.StandardWelcome
        };

        try
        {
            var template = await ResolveTemplateAsync(templateType, ct).ConfigureAwait(false);

            if (template is null)
                return AppResponses.Failure<ComposeEmailResponse>($"Template {templateType} not found");

            var tokens = new Dictionary<string, string>
            {
                ["CustomerId"] = evt.CustomerId.ToString(),
                ["CustomerNumber"] = evt.Number,
                ["CustomerName"] = evt.Name,
                ["CustomerType"] = evt.Type,
                ["Id"] = evt.CustomerId.ToString(),
                ["Number"] = evt.Number,
                ["Name"] = evt.Name,
                ["Type"] = evt.Type,
                ["Email"] = evt.Email,
                ["Date"] = evt.OccurredAt.ToString("f", CultureInfo.InvariantCulture)
            };

            return AppResponses.Success("Email composed", ComposeFromTemplate(
                template,
                evt.Name,
                evt.Email,
                tokens));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogEmailComposeError(_logger, templateType.ToString(), ex);
            throw;
        }
    }

}
