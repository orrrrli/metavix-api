using Application.Common.Errors;
using Application.Common.Interfaces.Services;

namespace Application.Common.Authorization;

internal static class CurrentUserAccess
{
    public static ErrorOr<Guid> RequireUserId(ICurrentUserService currentUser) =>
        currentUser.UserId is { } userId ? userId : AuthErrors.Forbidden;

    // Collapses the 3-line "resolve-or-return" preamble repeated at every call site
    // of the ErrorOr overload above into a single guard-clause line.
    public static Error? RequireUserId(ICurrentUserService currentUser, out Guid userId)
    {
        var result = RequireUserId(currentUser);
        if (result.IsError)
        {
            userId = default;
            return result.FirstError;
        }

        userId = result.Value;
        return null;
    }
}
