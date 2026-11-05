using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Users.Commands;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace BT.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class InitiateTotpSetupCommandHandler(
    IEncryptionService encryptionService,
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    ILogger<InitiateTotpSetupCommandHandler> logger, TimeProvider timeProvider) : IRequestHandler<InitiateTotpSetupCommand, AppResponse<TwoFactorSetupInfo>>
{
    private const string TotpIssuer = "BaseTemplate.API";

    public async Task<AppResponse<TwoFactorSetupInfo>> Handle(InitiateTotpSetupCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;

        try
        {
            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                return AppResponses.Failure<TwoFactorSetupInfo>("User not found.");
            }

            await iamUnitOfWork.TempTotpSecretRepository.DeleteUserTempSecretsAsync(userId, cancellationToken).ConfigureAwait(false);

            var plainSecret = GenerateSecret();
            var encryptedSecret = encryptionService.Encrypt(plainSecret);

            var tempSecret = TempTotpSecret.Create(
                timeProvider.GetUtcNow(),
                userId,
                encryptedSecret,
                timeProvider.GetUtcNow().AddMinutes(30),
                userId);

            await iamUnitOfWork.TempTotpSecretRepository.CreateAsync(tempSecret, cancellationToken).ConfigureAwait(false);
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            var qrUri = GenerateQrCodeUri(user.Email!, plainSecret);

            return AppResponses.Success("Scan this QR code", new TwoFactorSetupInfo
            {
                QrCodeUri = qrUri,
                ManualEntryKey = plainSecret
            });
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTotpSetupInitiationError(logger, userId, ex);
            throw;
        }
    }

    private static string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    private static string GenerateQrCodeUri(string email, string secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(TotpIssuer)}:" +
            $"{Uri.EscapeDataString(email)}?" +
            $"secret={secret}&" +
            $"issuer={Uri.EscapeDataString(TotpIssuer)}&" +
            "algorithm=SHA1&" +
            "digits=6&" +
            "period=30";
    }
}
