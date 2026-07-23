namespace Infrastructure.Services;

public sealed class BrevoSettings
{
    public static string SectionName { get; } = "Brevo";

    public string ApiKey       { get; init; } = string.Empty;
    public string SenderEmail  { get; init; } = string.Empty;
    public string SenderName   { get; init; } = string.Empty;
}
