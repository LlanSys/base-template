using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;

using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Contracts.Interfaces.Common;

public interface IServiceManager
{
    ICacheService CacheService { get; }
    ISessionService SessionService { get; }
    IBackgroundJobService BackgroundJobService { get; }
    IEmailService EmailService { get; }
    ISmsComposer SmsComposer { get; }
    IEncryptionService EncryptionService { get; }
    IAppUserService AppUserService { get; }

}
