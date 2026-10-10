namespace GtaSaModManager.Models;

public enum AppLanguage
{
    English,
    Persian
}

public enum AppTheme
{
    System,
    LightBlue,
    LightPurple,
    LightGreen,
    LightOrange,
    DarkBlue,
    DarkPurple,
    DarkGreen,
    DarkRed
}

public class AppSettings
{
    public bool HasSeenIntro { get; set; }

    public string? GamePath { get; set; }

    public string? ModSourceFolder { get; set; }

    public string? EssentialsPackagePath { get; set; }

    public string? DyomPackagePath { get; set; }

    public string? GameExecutableName { get; set; }

    public string? GameProfileId { get; set; }

    public string Language { get; set; } = nameof(AppLanguage.English);

    public string Theme { get; set; } = nameof(AppTheme.System);
}
