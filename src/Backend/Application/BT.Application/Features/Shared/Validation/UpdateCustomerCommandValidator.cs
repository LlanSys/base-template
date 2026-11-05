using BT.Application.Features.Banking.Customers.CommandHandlers;
using BT.SharedKernel.Validation.Features.Banking.Customers.Validators;
using FluentValidation;

namespace BT.Application.Features.Shared.Validation;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Customer ID is required.");
        RuleFor(x => x.UpdateCustomerRequest).SetValidator(new UpdateCustomerRequestValidator(timeProvider));
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
