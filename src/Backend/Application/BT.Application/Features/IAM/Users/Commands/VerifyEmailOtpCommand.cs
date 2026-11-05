using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;

using MediatR;

using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.IAM.Users.Commands;


public record VerifyEmailOtpCommand(VerifyEmailOtpRequest Request) : IRequest<AppResponse<VerifyEmailOtpResponse>>;

