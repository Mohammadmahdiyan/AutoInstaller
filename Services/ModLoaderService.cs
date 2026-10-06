using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public class ModLoaderService
{
    private static readonly string InstalledModsFileName = "installed-mods.json";
    private const string GameManagerDirectoryName = ".ModManager";

    public static string GetInstalledModsPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GtaSaModManager");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, InstalledModsFileName);
    }

    public static string GetGameInstallationsManifestPath(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return string.Empty;
        }

        var folder = Path.Combine(gamePath, GameManagerDirectoryName);
        Directory.CreateDirectory(folder);
        SetHidden(folder);

        var manifestPath = Path.Combine(folder, "installations.json");
        var legacyManifestPath = Path.Combine(gamePath, ".zGtaSaModManager", "installations.json");
        if (!File.Exists(manifestPath) && File.Exists(legacyManifestPath))
        {
            File.Copy(legacyManifestPath, manifestPath);
        }

        return manifestPath;
    }

    public static string GetUserFilesInstallationsManifestPath()
    {
        var userFilesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GTA San Andreas User Files");
        var folder = Path.Combine(userFilesRoot, ".zGtaSaModManager");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "installations.json");
    }

    private static string GetExistingUserFilesInstallationsManifestPath()
    {
        var userFilesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GTA San Andreas User Files");
        return Path.Combine(userFilesRoot, ".zGtaSaModManager", "installations.json");
    }

    private static void RemoveEmptyLegacyUserFilesManifestDirectory()
    {
        var legacyDirectory = Path.GetDirectoryName(GetExistingUserFilesInstallationsManifestPath());
        if (string.IsNullOrWhiteSpace(legacyDirectory) || !Directory.Exists(legacyDirectory))
        {
            return;
        }

        try
        {
            if (!Directory.EnumerateFileSystemEntries(legacyDirectory).Any())
            {
                Directory.Delete(legacyDirectory);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void SetHidden(string path)
    {
        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static InstallationManifest LoadInstallationManifest(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            return new InstallationManifest();
        }

        try
        {
            var json = File.ReadAllText(manifestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new InstallationManifest();
            }

            var manifest = JsonSerializer.Deserialize<InstallationManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return manifest ?? new InstallationManifest();
        }
        catch
        {
            return new InstallationManifest();
        }
    }

    public static void SaveInstallationManifest(string manifestPath, InstallationManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(manifestPath))
        {
            var attributes = File.GetAttributes(manifestPath);
            var writableAttributes = attributes & ~(FileAttributes.Hidden | FileAttributes.ReadOnly);
            if (writableAttributes != attributes)
            {
                File.SetAttributes(manifestPath, writableAttributes);
            }
        }

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestPath, json);
    }

    public static void RecordPackageInstallation(
        string modType,
        string modId,
        string sourcePackagePath,
        string installedDestination,
        IEnumerable<string> installedFiles,
        bool mergeExistingFiles = false)
    {
        var gamePath = Path.GetDirectoryName(installedDestination);
        var manifestGamePath = !string.IsNullOrWhiteSpace(gamePath) && Directory.Exists(gamePath) && gamePath.Contains("modloader", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(Path.GetDirectoryName(installedDestination)) ?? gamePath
            : Path.GetDirectoryName(installedDestination) ?? gamePath;

        var manifestPath = string.IsNullOrWhiteSpace(manifestGamePath)
            ? string.Empty
            : GetGameInstallationsManifestPath(manifestGamePath);

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        modId = string.IsNullOrWhiteSpace(modId) ? Guid.NewGuid().ToString("N") : modId;
        var matchingEntries = manifest.Entries
            .Where(entry => string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.InstalledDestination, installedDestination, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var filesToRecord = (mergeExistingFiles
                ? matchingEntries.SelectMany(entry => entry.InstalledFiles ?? new List<string>())
                : Enumerable.Empty<string>())
            .Concat(installedFiles)
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        manifest.Entries.RemoveAll(entry =>
            string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.InstalledDestination, installedDestination, StringComparison.OrdinalIgnoreCase));
        manifest.Entries.Add(new InstallationManifestEntry
        {
            ModId = modId,
            Type = modType,
            SourcePackagePath = sourcePackagePath,
            InstalledDestination = installedDestination,
            InstalledFiles = filesToRecord
        });
        SaveInstallationManifest(manifestPath, manifest);
    }

    public static bool RemoveInstalledPackageFiles(string gamePath, string installedDestination, IEnumerable<string> removedFiles)
    {
        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return false;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        var entry = manifest.Entries.LastOrDefault(item =>
            string.Equals(item.InstalledDestination, installedDestination, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return false;
        }

        var removedPaths = removedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        entry.InstalledFiles = (entry.InstalledFiles ?? new List<string>())
            .Where(path => File.Exists(path) && !removedPaths.Contains(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SaveInstallationManifest(manifestPath, manifest);
        return true;
    }

    public static void RecordGameInstallation(string gamePath, string modType, string modName, string sourcePackagePath, string installedDestination, IEnumerable<string> installedFiles)
    {
        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        var modId = string.IsNullOrWhiteSpace(modName) ? Guid.NewGuid().ToString("N") : modName;
        manifest.Entries.RemoveAll(entry => string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase));
        manifest.Entries.Add(new InstallationManifestEntry
        {
            ModId = modId,
            Type = modType,
            SourcePackagePath = sourcePackagePath,
            InstalledDestination = installedDestination,
            InstalledFiles = installedFiles
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        });
        SaveInstallationManifest(manifestPath, manifest);
    }

    public static bool RemoveGameInstallationRecord(string gamePath, string modId)
    {
        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        if (string.IsNullOrWhiteSpace(manifestPath) || string.IsNullOrWhiteSpace(modId))
        {
            return false;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        if (manifest.Entries.RemoveAll(entry =>
                string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return false;
        }

        SaveInstallationManifest(manifestPath, manifest);
        return true;
    }

    public static InstallationManifestEntry? FindGameInstallationRecord(
        string gamePath,
        string modType,
        string modName,
        string? sourcePackagePath = null)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return null;
        }

        var fullSourcePackagePath = !string.IsNullOrWhiteSpace(sourcePackagePath)
            ? Path.GetFullPath(sourcePackagePath)
            : string.Empty;
        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        return LoadInstallationManifest(manifestPath).Entries.LastOrDefault(entry =>
            string.Equals(entry.Type, modType, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(entry.ModId, modName, StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(fullSourcePackagePath)
                    && !string.IsNullOrWhiteSpace(entry.SourcePackagePath)
                    && string.Equals(
                        Path.GetFullPath(entry.SourcePackagePath),
                        fullSourcePackagePath,
                        StringComparison.OrdinalIgnoreCase)));
    }

    public static List<string> GetUnreplacedInstalledFiles(
        IEnumerable<string> installedFiles,
        IEnumerable<string> replacementFilePaths)
    {
        ArgumentNullException.ThrowIfNull(installedFiles);
        ArgumentNullException.ThrowIfNull(replacementFilePaths);

        var replacementPaths = replacementFilePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return installedFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(path => !replacementPaths.Contains(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void RecordUserFilesInstallation(
        string gamePath,
        string modName,
        string modType,
        IEnumerable<string> installedFiles)
    {
        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        var modId = string.IsNullOrWhiteSpace(modName) ? Guid.NewGuid().ToString("N") : modName;
        manifest.Entries.RemoveAll(entry => string.Equals(entry.ModId, modId, StringComparison.OrdinalIgnoreCase));
        manifest.Entries.Add(new InstallationManifestEntry
        {
            ModId = modId,
            Type = modType,
            InstalledDestination = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GTA San Andreas User Files",
                "DSL"),
            InstalledFiles = installedFiles
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        });
        SaveInstallationManifest(manifestPath, manifest);
    }

    public static (string ManifestPath, InstallationManifestEntry? Entry) FindUserFilesInstallation(
        string gamePath,
        string modId,
        string modType)
    {
        var missionDslDestination = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GTA San Andreas User Files",
            "DSL");
        var manifestPaths = new[]
        {
            GetGameInstallationsManifestPath(gamePath),
            GetExistingUserFilesInstallationsManifestPath()
        };

        foreach (var manifestPath in manifestPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var entry = LoadInstallationManifest(manifestPath).Entries.LastOrDefault(item =>
                string.Equals(item.Type, modType, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(modType, "missiondsl", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(item.InstalledDestination)
                        && string.Equals(
                            Path.GetFullPath(item.InstalledDestination),
                            Path.GetFullPath(missionDslDestination),
                            StringComparison.OrdinalIgnoreCase)));
            if (entry != null)
            {
                RemoveEmptyLegacyUserFilesManifestDirectory();
                return (manifestPath, entry);
            }
        }

        RemoveEmptyLegacyUserFilesManifestDirectory();
        return (string.Empty, null);
    }

    public static bool RemoveInstalledRecord(string modName, string? installedDestination = null)
    {
        var normalizedName = ModPackageService.NormalizeDisplayName(modName);
        var records = LoadInstalledRecords();
        var removed = records.RemoveAll(record =>
            string.Equals(record.ModName, normalizedName, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(installedDestination)
                && string.Equals(record.InstalledDestination, installedDestination, StringComparison.OrdinalIgnoreCase))) > 0;

        if (removed)
        {
            SaveInstalledRecords(records);
        }

        return removed;
    }

    public static bool TryUninstallInstalledMod(string modName, string? installedDestination = null, string? gamePath = null, string modType = "putinmodloader")
    {
        var normalizedName = ModPackageService.NormalizeDisplayName(modName);
        var destination = !string.IsNullOrWhiteSpace(installedDestination)
            ? installedDestination
            : !string.IsNullOrWhiteSpace(gamePath)
                ? Path.Combine(GameService.GetModLoaderFolder(gamePath), normalizedName)
                : string.Empty;

        var removedRecord = RemoveInstalledRecord(normalizedName, destination);

        if (!string.IsNullOrWhiteSpace(destination) && Directory.Exists(destination))
        {
            Directory.Delete(destination, true);
            removedRecord = true;
        }

        if (!string.IsNullOrWhiteSpace(gamePath))
        {
            RemovePackageInstallationByDestination(gamePath, destination);
            TryUninstallByModId(normalizedName, modType, destination, gamePath);
        }

        return removedRecord || (!string.IsNullOrWhiteSpace(destination) && !Directory.Exists(destination));
    }

    private static void RemovePackageInstallationByDestination(string gamePath, string installedDestination)
    {
        if (string.IsNullOrWhiteSpace(installedDestination))
        {
            return;
        }

        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return;
        }

        var manifest = LoadInstallationManifest(manifestPath);
        if (manifest.Entries.RemoveAll(entry =>
                string.Equals(entry.InstalledDestination, installedDestination, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            SaveInstallationManifest(manifestPath, manifest);
        }
    }

    public static bool TryUninstallByModId(string modId, string modType, string? installedDestination = null, string? gamePath = null)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            return false;
        }

        var normalizedType = (modType ?? string.Empty).Trim();
        var normalizedTypeLower = normalizedType.ToLowerInvariant();

        if (normalizedTypeLower == "savesandmissions")
        {
            return TryUninstallUserFilesInstall(modId);
        }

        if (normalizedTypeLower == "missiondsl")
        {
            return TryUninstallMissionDslInstall(gamePath, modId);
        }

        if (normalizedTypeLower == "putinmodloader")
        {
            if (!string.IsNullOrWhiteSpace(installedDestination) && Directory.Exists(installedDestination))
            {
                Directory.Delete(installedDestination, true);
                RemoveInstalledRecord(modId, installedDestination);
                return true;
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return false;
        }

        var manifestPath = GetGameInstallationsManifestPath(gamePath);
        var manifest = LoadInstallationManifest(manifestPath);
        var entry = manifest.Entries.FirstOrDefault(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return false;
        }

        foreach (var file in entry.InstalledFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.InstalledDestination) && Directory.Exists(entry.InstalledDestination))
        {
            Directory.Delete(entry.InstalledDestination, true);
        }

        manifest.Entries.RemoveAll(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase));
        SaveInstallationManifest(manifestPath, manifest);
        return true;
    }

    public static bool TryUninstallUserFilesInstall(string modId)
    {
        var manifestPath = GetUserFilesInstallationsManifestPath();
        var manifest = LoadInstallationManifest(manifestPath);
        var entry = manifest.Entries.FirstOrDefault(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return false;
        }

        foreach (var file in entry.InstalledFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (string.Equals(entry.Type, "missiondsl", StringComparison.OrdinalIgnoreCase))
        {
            var userFilesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "GTA San Andreas User Files");
            ClearDirectoryContents(Path.Combine(userFilesRoot, "DSL"));
        }

        manifest.Entries.RemoveAll(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase));
        SaveInstallationManifest(manifestPath, manifest);
        return true;
    }

    private static bool TryUninstallMissionDslInstall(string? gamePath, string modId)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return TryUninstallUserFilesInstall(modId);
        }

        var (manifestPath, entry) = FindUserFilesInstallation(gamePath, modId, "missiondsl");
        if (entry == null || string.IsNullOrWhiteSpace(manifestPath))
        {
            return false;
        }

        foreach (var file in entry.InstalledFiles ?? new List<string>())
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        var userFilesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GTA San Andreas User Files");
        ClearDirectoryContents(Path.Combine(userFilesRoot, "DSL"));

        var manifest = LoadInstallationManifest(manifestPath);
        manifest.Entries.RemoveAll(item => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Type, "missiondsl", StringComparison.OrdinalIgnoreCase));
        SaveInstallationManifest(manifestPath, manifest);
        return true;
    }

    public static void ClearDirectoryContents(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var entry in Directory.GetFileSystemEntries(directoryPath))
        {
            if (Directory.Exists(entry))
            {
                Directory.Delete(entry, recursive: true);
            }
            else
            {
                File.Delete(entry);
            }
        }
    }

    public static bool TryUninstallModFolder(string modFolderPath)
    {
        if (string.IsNullOrWhiteSpace(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            return false;
        }

        Directory.Delete(modFolderPath, true);
        return true;
    }

    public static List<InstalledModRecord> LoadInstalledRecords()
    {
        var path = GetInstalledModsPath();
        if (!File.Exists(path))
        {
            return new List<InstalledModRecord>();
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<InstalledModRecord>();
            }

            var records = JsonSerializer.Deserialize<List<InstalledModRecord>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return records ?? new List<InstalledModRecord>();
        }
        catch
        {
            return new List<InstalledModRecord>();
        }
    }

    public static void SaveInstalledRecords(IEnumerable<InstalledModRecord> records)
    {
        var path = GetInstalledModsPath();
        var json = JsonSerializer.Serialize(records.ToList(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    public static bool IsInstalled(string modName, string? sourcePackagePath = null)
    {
        var normalizedName = ModPackageService.NormalizeDisplayName(modName);
        var records = LoadInstalledRecords();
        return records.Any(record =>
            string.Equals(record.ModName, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(sourcePackagePath) || string.Equals(record.SourcePackagePath, sourcePackagePath, StringComparison.OrdinalIgnoreCase)));
    }

    public static void RecordInstallation(string modName, string sourcePackagePath, string installedDestination)
    {
        var normalizedName = ModPackageService.NormalizeDisplayName(modName);
        var records = LoadInstalledRecords();
        records.RemoveAll(record => string.Equals(record.ModName, normalizedName, StringComparison.OrdinalIgnoreCase));
        records.Add(new InstalledModRecord
        {
            ModName = normalizedName,
            SourcePackagePath = sourcePackagePath,
            InstalledDestination = installedDestination,
            InstalledAtUtc = DateTime.UtcNow
        });
        SaveInstalledRecords(records);
    }

    public static IEnumerable<ModInfo> GetInstalledMods(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            return Array.Empty<ModInfo>();
        }

        var modLoaderFolder = GameService.GetModLoaderFolder(gamePath);
        if (!Directory.Exists(modLoaderFolder))
        {
            return Array.Empty<ModInfo>();
        }

        var records = LoadInstalledRecords();
        var installedDirectoryNames = Directory.GetDirectories(modLoaderFolder)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mods = new List<ModInfo>();
        foreach (var directory in Directory.GetDirectories(modLoaderFolder))
        {
            var name = Path.GetFileName(directory);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var previewPath = FindPreview(directory);
            var readmePath = FindReadme(directory);
            var record = records.FirstOrDefault(item => string.Equals(item.InstalledDestination, directory, StringComparison.OrdinalIgnoreCase));

            mods.Add(new ModInfo
            {
                Name = record?.ModName ?? ModPackageService.NormalizeDisplayName(name),
                FolderPath = directory,
                PreviewPath = previewPath,
                ReadmePath = readmePath,
                Status = "Installed",
                InstallationMethod = "ModLoader"
            });
        }

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.InstalledDestination) || !Directory.Exists(record.InstalledDestination))
            {
                continue;
            }

            if (installedDirectoryNames.Contains(Path.GetFileName(record.InstalledDestination)))
            {
                continue;
            }

            var previewPath = FindPreview(record.InstalledDestination);
            var readmePath = FindReadme(record.InstalledDestination);
            mods.Add(new ModInfo
            {
                Name = record.ModName,
                FolderPath = record.InstalledDestination,
                PreviewPath = previewPath,
                ReadmePath = readmePath,
                Status = "Installed",
                InstallationMethod = "ModLoader"
            });
        }

        return mods.DistinctBy(mod => mod.FolderPath, StringComparer.OrdinalIgnoreCase);
    }

    public static string? FindPreview(string modFolder)
    {
        foreach (var file in Directory.GetFiles(modFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension is ".jpg" or ".jpeg" or ".png")
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Equals("preview", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
        }

        return null;
    }

    public static string? FindReadme(string modFolder)
    {
        var readme = Directory.GetFiles(modFolder, "README*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        return readme;
    }
}
