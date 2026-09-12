namespace GtaSaModManager.Models;

public class ModInfo
{
    public string Name { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;

    public string? PreviewPath { get; set; }

    public string? ReadmePath { get; set; }

    public string Status { get; set; } = "Installed";

    public string InstallationMethod { get; set; } = "ModLoader";
}

public class ModPackageInfo
{
    public string DisplayName { get; set; } = string.Empty;

    public string PackageRootPath { get; set; } = string.Empty;

    public string PayloadPath { get; set; } = string.Empty;

    public bool HasConfigError { get; set; }

    public string? ConfigError { get; set; }
}

public class InstalledModRecord
{
    public string ModName { get; set; } = string.Empty;

    public string SourcePackagePath { get; set; } = string.Empty;

    public string InstalledDestination { get; set; } = string.Empty;

    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
}
