using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Modsyn.Conversion;

public sealed record ModsynBackupConfiguration(
    ModsynBackupMode Mode,
    IReadOnlyList<string> IncludePaths,
    IReadOnlyList<string> ExcludePaths)
{
    public bool Enabled => Mode != ModsynBackupMode.None;

    public bool ShouldBackup(string relativePath)
    {
        if (Mode == ModsynBackupMode.None)
        {
            return false;
        }

        if (Mode == ModsynBackupMode.All)
        {
            return true;
        }

        var normalizedPath = Normalize(relativePath);
        if (ExcludePaths.Any(path => Matches(path, normalizedPath)))
        {
            return false;
        }

        return IncludePaths.Count == 0 || IncludePaths.Any(path => Matches(path, normalizedPath));
    }

    private static bool Matches(string configuredPath, string candidatePath)
    {
        var normalized = Normalize(configuredPath);
        return string.Equals(normalized, candidatePath, StringComparison.OrdinalIgnoreCase)
            || candidatePath.StartsWith(normalized + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return path.Trim().Replace('/', '\\').Trim('\\');
    }
}

public sealed class ModsynConvertedConfiguration
{
    internal ModsynConvertedConfiguration(
        ModManifest manifest,
        IReadOnlyList<ModReplacementEntry> replacements,
        ModsynBackupConfiguration backup,
        IReadOnlyList<ModsynResolvedRequirement> requirements)
    {
        Manifest = manifest;
        Replacements = replacements;
        Backup = backup;
        Requirements = requirements;
    }

    public ModManifest Manifest { get; }

    public IReadOnlyList<ModReplacementEntry> Replacements { get; }

    public ModsynBackupConfiguration Backup { get; }

    public IReadOnlyList<ModsynResolvedRequirement> Requirements { get; }
}