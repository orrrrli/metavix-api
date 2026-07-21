using Application.Common.Errors;
using Application.Common.Interfaces.Services;

namespace Application.Common.Authorization;

internal static class CurrentUserAccess
{
    public static ErrorOr<Guid> RequireUserId(ICurrentUserService currentUser) =>
        currentUser.UserId is { } userId ? userId : AuthErrors.Forbidden;
}
