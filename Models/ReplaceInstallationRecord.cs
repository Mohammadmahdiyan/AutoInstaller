namespace GtaSaModManager.Models;

public class ReplaceInstallationRecord
{
    public string BackupId { get; set; } = string.Empty;

    public string ModName { get; set; } = string.Empty;

    public string GameFolder { get; set; } = string.Empty;

    public string OriginalFilePath { get; set; } = string.Empty;

    public string BackupFilePath { get; set; } = string.Empty;

    public string InstalledModFile { get; set; } = string.Empty;

    public DateTime InstallationDate { get; set; } = DateTime.UtcNow;

    public string InstallationType { get; set; } = "Replacing";

    public bool ReplacementSucceeded { get; set; }

    public bool OriginalBackupAvailable { get; set; }

    public bool Restored { get; set; }

    public string? StatusMessage { get; set; }
}
