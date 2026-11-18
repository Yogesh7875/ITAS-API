using Project.Core.Caching;

namespace Project.Services.Users;

/// <summary>
/// Represents default values related to user sessions
/// </summary>
public static partial class UserSessionDefaults
{
    /// <summary>
    /// Gets a key for the "this session is active" flag
    /// </summary>
    /// <remarks>
    /// {0} : user id
    /// {1} : session id
    /// </remarks>
    public static CacheKey ActiveSessionCacheKey => new("Project.UserSession.active.{0}.{1}", ActiveSessionPrefix);

    /// <summary>
    /// Gets a key prefix for every cached session of one user
    /// </summary>
    /// <remarks>
    /// {0} : user id
    /// </remarks>
    public static string ActiveSessionPrefix => "Project.UserSession.active.{0}";

    /// <summary>
    /// Gets the maximum time (minutes) the flag lives in cache. Revocation also removes it explicitly,
    /// so this only bounds the damage of a missed invalidation.
    /// </summary>
    public static int ActiveSessionCacheMinutes => 5;

    /// <summary>
    /// Gets the lock resource name that serialises logins of one user/app
    /// </summary>
    /// <remarks>
    /// {0} : user id
    /// {1} : app type
    /// </remarks>
    public static string LoginLockKey => "Project.UserSession.login.{0}.{1}";
}
