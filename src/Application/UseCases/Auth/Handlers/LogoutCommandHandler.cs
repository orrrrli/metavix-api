using Application.Common.Interfaces.Persistence;
using Application.UseCases.Auth.Commands;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, ErrorOr<Deleted>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ErrorOr<Deleted>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        RefreshToken? stored = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (stored is not null && !stored.IsRevoked)
            await _refreshTokenRepository.RevokeAsync(stored);

        return Result.Deleted;
    }
}
