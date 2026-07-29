namespace Application.UseCases.Auth.Outbox;

public sealed record PasswordResetEmailPayload(string ToEmail, string ToName, string ResetLink);
