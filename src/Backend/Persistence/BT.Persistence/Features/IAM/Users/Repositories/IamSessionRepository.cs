using BT.Domain.Features.Banking.Customers.Contracts.Repositories;
using BT.Domain.Features.HR.Employees.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Contracts.Repositories;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BT.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamSessionRepository : Repository<AppUserSession>, ISessionRepository
{
    private readonly IamDBContext _iamContext;
    private readonly TimeProvider _timeProvider;

    public IamSessionRepository(IamDBContext context, TimeProvider timeProvider) : base(context)
    {
        _iamContext = context;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<List<AppUserSession>> GetActiveSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        return await _iamContext.AppUserSessions
            .Where(s => s.AppUserId == userId && s.IsActive && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastAccessedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppUserSession?> GetTrackedByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AppUserSession?> GetOldestSessionByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .Where(s => s.AppUserId == userId && s.IsActive)
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<AppUserSession>> GetExpiredSessionsAsync(DateTimeOffset retentionLimit, CancellationToken cancellationToken = default)
    {
        return await _iamContext.AppUserSessions
            .Where(s => s.IsActive && s.IsRevoked && s.ExpiresAt <= retentionLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PurgeOldSessionsAsync(DateTimeOffset retentionLimit, CancellationToken cancellationToken = default)
    {
        var oldSessions = await _iamContext.AppUserSessions
            .Where(s => s.CreatedAt < retentionLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (!oldSessions.Any())
            return false;

        _iamContext.AppUserSessions.RemoveRange(oldSessions);
        return true;
    }
}
