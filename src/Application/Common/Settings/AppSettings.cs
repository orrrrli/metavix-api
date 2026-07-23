namespace Application.Common.Settings;

public sealed class AppSettings
{
    public static string SectionName { get; } = "App";

    public string AppBaseUrl { get; init; } = string.Empty;
}
