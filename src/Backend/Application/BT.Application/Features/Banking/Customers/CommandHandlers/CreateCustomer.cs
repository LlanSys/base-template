using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.Banking.Customers.Contracts.Interfaces;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.SharedKernel.Extensions;
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
using BT.Domain.Features.Banking.Customers.Entities;
using BT.Domain.Features.Banking.Customers.Enums;
using BT.Domain.Features.Banking.Customers.ValueObjects;
using BT.SharedKernel.Features.Banking.Customers.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BT.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Invalidation: bump the global version tokens for customer lists and dashboards.
/// No entity key to delete (the entity does not exist in cache yet).
/// Versioned list entries are orphaned in O(1).
/// </summary>


internal sealed class CreateCustomerCommandHandler(
    TimeProvider timeProvider,
    IBankingUnitOfWork _bankingUnitOfWork,
    ILogger<CreateCustomerCommandHandler> _logger,
    ICustomerNumberGenerator _customerNumberGenerator

) : IRequestHandler<CreateCustomerCommand, AppResponse<CustomerResponse>>
{
    public async Task<AppResponse<CustomerResponse>> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        try
        {
            var req = command.CreateCustomerRequest;
            var customerNumber = await _customerNumberGenerator.GenerateAsync(ct).ConfigureAwait(false);

            // Build owned entities
            var corporateDetails = CorporateDetail.Create(
                req.CompanyName,
                req.LineOfBusiness.ToEnum<LineOfBusiness>(),
                req.NatureOfBusiness,
                req.IdentificationType.ToEnum<IdentificationType>(),
                req.RegistrationNumber,
                req.DateOfRegistration,
                req.LineOfBusinessMoreInfo,
                req.RegisteredAt,
                req.RegisteredOffice,
                req.BusinessStartedYear,
                req.NumberOfEmployees,
                req.Website,
                req.TINNumber,
                req.Classification,
                req.Comments

            );

            var address = Address.Create(
                req.ResidentialAddress,
                req.Country,
                req.Region,
                req.Ward,
                req.District,
                req.Mobile,
                req.Email,
                req.BusinessAddress,
                req.OfficeAddress,
                req.MailingAddress,
                req.Street,
                req.ZipCode,
                req.PhoneHome,
                req.PhoneWork,
                req.FaxNo,
                req.LandMark
            );

            var communicationPrefs = CommunicationPreference.Create(
                canSendGreetings: req.CanSendGreetings,
                canSendAssociateSpecialOffer: req.CanSendAssociateSpecialOffer,
                canSendOurSpecialOffers: req.CanSendOurSpecialOffers,
                statementOnline: req.StatementOnline,
                mobileAlert: req.MobileAlert
            );

            // Create aggregate root — domain rules enforced inside
            var customer = Customer.Create(
                timeProvider.GetUtcNow(),
                customerNumber,
                req.CompanyName,
                req.Type.ToEnum<CustomerType>(),
                req.SegmentType.ToEnum<SegmentType>(),
                req.SubSegmentType.ToEnum<SubSegmentType>(),
                req.RelationshipManagerId,
                req.OpenedOn,
                corporateDetails,
                address,
                communicationPrefs,
                // CreatedBy is populated by the audit interceptor in BankingDBContext.SaveChangesAsync
                // via ICurrentActorProvider; defaults to "System" for unauthenticated flows.
                string.Empty
            );

            await _bankingUnitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _bankingUnitOfWork.CustomerRepository.CreateAsync(customer, ct).ConfigureAwait(false);
                return true;
            }, ct).ConfigureAwait(false);

            LogDefinitions.LogCustomerCreated(_logger, customer.Number, customer.CorporateDetail.CompanyName);
            return AppResponses.Success($"Customer {customer.Number} created successfully.", customer.ToCustomerResponse());

        }
        catch (ArgumentException ex)
        {
            LogDefinitions.LogCustomerCreateValidationFailed(_logger, ex);
            return AppResponses.Failure<CustomerResponse>("Customer details are invalid. Please review the form and try again.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerCreateFailed(_logger, ex);
            throw;
        }
    }
}
