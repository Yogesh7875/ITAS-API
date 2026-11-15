using Project.Core.Domain.Users;
using Project.Services.Users.DataModels;

namespace Project.Services.Users;

public interface IUserSessionService
{
    Task<NewSession> CreateSessionAsync(long userId, SessionInfo info);

    Task<(UserSession Session, string NewRefreshToken)?> RotateAsync(string refreshToken);

    Task RevokeAsync(long userId, Guid sessionId, string reason);

    Task<bool> IsSessionActiveAsync(long userId, Guid sessionId);

    Task CleanupAsync(int retentionDays = 30);
}
