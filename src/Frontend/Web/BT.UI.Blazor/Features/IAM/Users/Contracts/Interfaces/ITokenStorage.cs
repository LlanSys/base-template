using System;
using System.Collections.Generic;
using System.Text;

namespace BT.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

internal interface ITokenStorage
{
    Task<bool> ClearAsync();
    Task<(string? AccessToken, string? RefreshToken, string? SessionId)> GetAsync();
    Task<bool> SaveAsync(string? accessToken, string? refreshToken, string? sessionId);
}
