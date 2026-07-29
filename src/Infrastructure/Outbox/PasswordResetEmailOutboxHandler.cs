using Application.Common.Interfaces.Services;
using Application.Common.Outbox;
using Application.UseCases.Auth.Outbox;

namespace Infrastructure.Outbox;

public sealed class PasswordResetEmailOutboxHandler : OutboxMessageHandler<PasswordResetEmailPayload>
{
    private readonly IEmailService _emailService;

    public PasswordResetEmailOutboxHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public override string Type => OutboxMessageTypes.PasswordResetEmail;

    protected override Task HandleAsync(PasswordResetEmailPayload payload, CancellationToken cancellationToken) =>
        _emailService.SendPasswordResetEmailAsync(payload.ToEmail, payload.ToName, payload.ResetLink);
}
