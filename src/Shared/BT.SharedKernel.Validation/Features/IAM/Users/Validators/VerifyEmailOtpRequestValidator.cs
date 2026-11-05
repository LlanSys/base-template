using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.Domain.Features.IAM.Users.Enums;
using BT.SharedKernel.Validation.Validators.Common;
using FluentValidation;
using System;

namespace BT.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class VerifyEmailOtpRequestValidator : Validator<VerifyEmailOtpRequest>
{
    public VerifyEmailOtpRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(450);
        RuleFor(request => request.Code).NotEmpty().Matches("^\\d{6}$");
        RuleFor(request => request.Purpose)
            .NotEmpty()
            .Must(static purpose => Enum.TryParse<OtpPurpose>(purpose, ignoreCase: true, out _))
            .WithMessage("OTP purpose is not supported.");

        RuleFor(request => request.DeviceFingerprint).MaximumLength(256);
    }
}
