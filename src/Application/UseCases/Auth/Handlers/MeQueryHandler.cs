using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Common;
using Application.UseCases.Auth.Queries;
using Domain.Enums;

namespace Application.UseCases.Auth.Handlers;

internal sealed class MeQueryHandler : IRequestHandler<MeQuery, ErrorOr<MeResult>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository     _userRepository;

    public MeQueryHandler(ICurrentUserService currentUser, IUserRepository userRepository)
    {
        _currentUser    = currentUser;
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<MeResult>> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.InvalidCredentials;

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        string fullName = user.Role switch
        {
            UserRole.Doctor  => user.Doctor  is not null
                ? $"{user.Doctor.FirstName} {user.Doctor.PaternalLastName}"
                : user.Email,
            UserRole.Patient => user.Patient is not null
                ? $"{user.Patient.FirstName} {user.Patient.LastName}"
                : user.Email,
            _ => user.Email,
        };

        return new MeResult(
            UserId:    user.Id,
            PatientId: user.Patient?.Id,
            DoctorId:  user.Doctor?.Id,
            Email:     user.Email,
            Role:      user.Role.ToString(),
            FullName:  fullName);
    }
}
