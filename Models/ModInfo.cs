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

public class InstallationManifestEntry
{
    public string ModId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public List<string> InstalledFiles { get; set; } = new();

    public string SourcePackagePath { get; set; } = string.Empty;

    public string InstalledDestination { get; set; } = string.Empty;

    public List<InstallationManifestPart> MixedParts { get; set; } = new();

    public List<InstallationAssetMapping> AssetMappings { get; set; } = new();

    public List<OptionalAssetBackup> OptionalAssetBackups { get; set; } = new();

    /// <summary>ModId of the base mod when this entry is one of its optional/optionals packages.</summary>
    public string ParentModId { get; set; } = string.Empty;

    /// <summary>"optional" or "optionals" for entries installed from the base mod's optional folders.</summary>
    public string OptionalKind { get; set; } = string.Empty;
}

public sealed class InstallationManifestPart
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string ModId { get; set; } = string.Empty;

    public string InstalledDestination { get; set; } = string.Empty;

    public List<string> InstalledFiles { get; set; } = new();

    public List<InstallationAssetMapping> AssetMappings { get; set; } = new();
}

public sealed class InstallationAssetMapping
{
    public string SourceModelName { get; set; } = string.Empty;

    public string TargetModelName { get; set; } = string.Empty;

    public string AssetType { get; set; } = string.Empty;
}

public sealed class OptionalAssetBackup
{
    public string DestinationPath { get; set; } = string.Empty;

    public string BackupFilePath { get; set; } = string.Empty;

    public bool OriginalExisted { get; set; }
}

public class InstallationManifest
{
    public List<InstallationManifestEntry> Entries { get; set; } = new();
}
