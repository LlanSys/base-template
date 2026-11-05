using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Users.Dtos;

namespace BT.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

internal interface IAuthSession
{
    event Func<Task>? Changed;
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    CurrentUserResponse? CurrentUser { get; }
    string? ProfilePictureDataUri { get; }
    string? PendingTwoFactorUserId { get; }
    string? LastError { get; }
    bool HasFullAccess { get; }
    bool MfaEnrollmentRequired { get; }

    Task InitializeAsync();
    Task RefreshAsync();
    Task<AppResponse<LoginResponse>> SignInAsync(LoginRequest request);
    Task<AppResponse<LoginResponse>> SignInWithPasskeyAsync(LoginWithPasskeyRequest request);
    Task<AppResponse<LoginResponse>> ExchangeSsoCodeAsync(string code);
    Task<AppResponse<LoginResponse>> AcceptInvitationAsync(SharedKernel.Features.ControlPlane.Tenants.Dtos.AcceptAdministratorInvitationRequest request);
    Task<AppResponse<VerifyOtpResponse>> CompleteTwoFactorAsync(VerifyOtpRequest request);
    Task<AppResponse<bool>> SignOutAsync();
    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    bool IsInRole(string role);
}
