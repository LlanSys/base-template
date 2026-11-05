using BT.Application.Features.HR.Employees.CommandHandlers;
using BT.SharedKernel.Validation.Features.HR.Employees.Validators;
using FluentValidation;

namespace BT.Application.Features.Shared.Validation;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Request).SetValidator(new CreateEmployeeRequestValidator(timeProvider));
        RuleFor(x => x.User).NotEmpty().WithMessage("Current user is required.");
    }
}
