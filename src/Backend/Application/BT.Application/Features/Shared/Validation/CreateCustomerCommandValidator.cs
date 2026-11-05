using BT.Application.Features.Banking.Customers.CommandHandlers;
using BT.SharedKernel.Validation.Features.Banking.Customers.Validators;
using FluentValidation;

namespace BT.Application.Features.Shared.Validation;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.CreateCustomerRequest).SetValidator(new CreateCustomerRequestValidator(timeProvider));
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
