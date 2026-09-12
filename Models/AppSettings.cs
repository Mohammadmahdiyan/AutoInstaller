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
    public string? GamePath { get; set; }

    public string? ModSourceFolder { get; set; }

    public string Language { get; set; } = nameof(AppLanguage.English);

    public string Theme { get; set; } = nameof(AppTheme.System);
}
