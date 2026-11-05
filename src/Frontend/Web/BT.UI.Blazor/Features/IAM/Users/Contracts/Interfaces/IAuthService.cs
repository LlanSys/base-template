using BT.SharedKernel.Features.IAM.Users.Dtos;
using BT.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BT.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

internal interface IAuthService
{
    Task<AppResponse<LoginResponse>> AcceptInvitationAsync(SharedKernel.Features.ControlPlane.Tenants.Dtos.AcceptAdministratorInvitationRequest request);
    Task<AppResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    Task<AppResponse<LoginResponse>> ExchangeSsoCodeAsync(string code);
    Task<AppResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<AppResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AppResponse<PasswordResetOtpVerificationResponse>> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request);
    Task<AppResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<AppResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    Task<AppResponse<CurrentUserResponse>> GetCurrentUserAsync();
    Task<AppResponse<FileContentResponse>> GetProfilePictureAsync();
    Task<AppResponse<ProfilePictureResponse>> UpdateProfilePictureAsync(byte[] content, string fileName, string contentType);
    Task<AppResponse<TwoFactorSetupInfo>> InitiateTotpSetupAsync();
    Task<AppResponse<VerifyOtpResponse>> VerifyTotpAsync(VerifyOtpRequest request);
    Task<AppResponse<bool>> DisableTotpAsync();
    Task<AppResponse<OtpStatusResponse>> GetTotpStatusAsync(string userId);
    Task<AppResponse<System.Text.Json.JsonElement>> RequestPasskeyRegistrationOptionsAsync();
    Task<AppResponse<bool>> RegisterPasskeyAsync(RegisterPasskeyRequest request);
    Task<AppResponse<PasskeyLoginOptionsResponse>> RequestPasskeyLoginOptionsAsync(RequestPasskeyLoginOptionsRequest request);
    Task<AppResponse<LoginResponse>> LoginWithPasskeyAsync(LoginWithPasskeyRequest request);
    Task<AppResponse<IReadOnlyList<PasskeyResponse>>> GetPasskeysAsync();
    Task<AppResponse<bool>> LogoutAsync();
}
