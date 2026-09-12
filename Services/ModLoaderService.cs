using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public class ModLoaderService
{
    private static readonly string InstalledModsFileName = "installed-mods.json";

    public static string GetInstalledModsPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GtaSaModManager");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, InstalledModsFileName);
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
