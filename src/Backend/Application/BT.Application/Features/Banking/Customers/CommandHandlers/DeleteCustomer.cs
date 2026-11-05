using BT.SharedKernel.Dtos.Common;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.SharedKernel.Features.Banking.Customers.Dtos;

using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Same invalidation pattern as Update: remove the entity entry + bump the list version.
/// </summary>


internal sealed class DeleteCustomerCommandHandler(IBankingUnitOfWork _unitOfWork, ILogger<DeleteCustomerCommandHandler> _logger)
    : IRequestHandler<DeleteCustomerCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteCustomerCommand command, CancellationToken ct)
    {
        try
        {
            var customer = await _unitOfWork.CustomerRepository.FindByIdAsync(command.CustomerId, ct).ConfigureAwait(false);

            if (customer is null)
                return AppResponses.Failure<bool>($"Customer {command.CustomerId} not found.");

            await _unitOfWork.CustomerRepository.SoftDeleteAsync(command.CustomerId, ct).ConfigureAwait(false);

            var saved = await _unitOfWork.CompleteAsync(ct).ConfigureAwait(false) > 0;

            if (!saved)
                return AppResponses.Failure<bool>("Failed to delete customer.");
            return AppResponses.Success(true);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerDeleteFailed(_logger, command.CustomerId, ex);
            throw;
        }

    }
}
