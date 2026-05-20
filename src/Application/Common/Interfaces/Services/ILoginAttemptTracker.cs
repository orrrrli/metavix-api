namespace Application.Common.Interfaces.Services;

public interface ILoginAttemptTracker
{
    bool IsBlocked(string email);
    void RegisterFailure(string email);
    void ResetAttempts(string email);
}
