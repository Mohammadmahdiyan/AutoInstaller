using System.IO.Compression;
using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public class ModPackageService
{
    public static bool IsArchive(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".zip" or ".7z" or ".rar";
    }

    public static string GetSelectedName(string selectedPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(selectedPath);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return SanitizeFolderName(fileName);
        }

        return "Mod";
    }

    public static string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        sanitized = sanitized.Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Mod" : sanitized;
    }

    public static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Mod";
        }

        var normalized = value.Trim();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"^\d+[\s\-_]*", string.Empty);
        normalized = normalized.Replace('_', ' ').Replace('-', ' ');
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

        return string.IsNullOrWhiteSpace(normalized) ? "Mod" : normalized;
    }

    public static bool IsModPackageRoot(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return false;
        }

        var entries = Directory.EnumerateFileSystemEntries(directoryPath).ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        var modJsonPath = Path.Combine(directoryPath, "mod.json");
        if (!File.Exists(modJsonPath))
        {
            return true;
        }

        return TryValidateModJson(directoryPath, out _);
    }

    public static bool TryValidateConfigJson(string packagePath, out string? error)
    {
        return TryValidateModJson(packagePath, out error);
    }

    public static bool TryValidateModJson(string packagePath, out string? error)
    {
        error = null;
        var manifestPath = GetManifestPath(packagePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "mod.json must contain a JSON object.";
                return false;
            }

            var properties = document.RootElement.EnumerateObject().ToList();
            if (properties.Count == 0)
            {
                return true;
            }

            if (!document.RootElement.TryGetProperty("type", out var typeProperty) ||
                typeProperty.ValueKind != JsonValueKind.String)
            {
                error = "mod.json must contain a string type property.";
                return false;
            }

            var manifest = TryReadManifest(packagePath);
            if (manifest == null || !new[]
                {
                    "putinmodloader", "replacing", "putincleo", "putingamefolder",
                    "putandreplace", "putandreplaces", "vehicleandskinandweapon",
                    "vehiclesandskinsandweapons", "savesandmissions", "missiondsl"
                }.Contains(manifest.NormalizedType, StringComparer.OrdinalIgnoreCase))
            {
                error = "mod.json contains an unsupported type.";
                return false;
            }

            if (manifest.NormalizedType == "putandreplace" &&
                (!document.RootElement.TryGetProperty("replacements", out var replacements) || replacements.ValueKind != JsonValueKind.Array))
            {
                error = "PutAndReplace requires a replacements array.";
                return false;
            }

            if (manifest.NormalizedType == "putandreplace")
            {
                try
                {
                    _ = ReadReplacementEntries(packagePath);
                }
                catch (InvalidDataException ex)
                {
                    error = ex.Message;
                    return false;
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static ModManifest? TryReadManifest(string basePath)
    {
        var manifestPath = GetManifestPath(basePath);
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return new ModManifest();
        }

        try
        {
            var json = File.ReadAllText(manifestPath);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var manifest = new ModManifest();
            if (document.RootElement.TryGetProperty("type", out var typeProperty) &&
                typeProperty.ValueKind == JsonValueKind.String)
            {
                manifest.Type = typeProperty.GetString() ?? string.Empty;
            }

            return manifest;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetManifestPath(string basePath)
    {
        var modJsonPath = Path.Combine(basePath, "mod.json");
        if (File.Exists(modJsonPath))
        {
            return modJsonPath;
        }

        var configJsonPath = Path.Combine(basePath, "config.json");
        return File.Exists(configJsonPath) ? configJsonPath : null;
    }

    public static ModManifest ResolveManifest(string basePath)
    {
        return TryReadManifest(basePath) ?? new ModManifest();
    }

    public static List<ModReplacementEntry> ReadReplacementEntries(string packageRoot)
    {
        var configPath = GetManifestPath(packageRoot)
            ?? throw new InvalidDataException("The package manifest was not found.");
        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        if (!document.RootElement.TryGetProperty("replacements", out var replacements) || replacements.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("PutAndReplace requires a replacements array.");
        }

        var result = new List<ModReplacementEntry>();
        foreach (var item in replacements.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var path = item.GetString();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    result.Add(new ModReplacementEntry(path, path));
                }
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                var source = item.TryGetProperty("source", out var sourceProperty) && sourceProperty.ValueKind == JsonValueKind.String
                    ? sourceProperty.GetString()
                    : null;
                var target = item.TryGetProperty("target", out var targetProperty) && targetProperty.ValueKind == JsonValueKind.String
                    ? targetProperty.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                {
                    result.Add(new ModReplacementEntry(source, target));
                }
            }
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException("PutAndReplace contains no valid replacement entries.");
        }

        return result;
    }

    public static bool IsReplacingInstall(string packageRoot)
    {
        return ResolveManifest(packageRoot).IsReplacing;
    }

    public static string GetDefaultBackupRoot()
    {
        return @"C:\ZBackUP-SaModManager\";
    }

    public static string GetReplacementHistoryPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GtaSaModManager");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "replacement-install-history.json");
    }

    public static List<ReplaceInstallationRecord> LoadReplacementRecords()
    {
        var historyPath = GetReplacementHistoryPath();
        if (!File.Exists(historyPath))
        {
            return new List<ReplaceInstallationRecord>();
        }

        try
        {
            var json = File.ReadAllText(historyPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ReplaceInstallationRecord>();
            }

            var records = JsonSerializer.Deserialize<List<ReplaceInstallationRecord>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return records ?? new List<ReplaceInstallationRecord>();
        }
        catch
        {
            return new List<ReplaceInstallationRecord>();
        }
    }

    public static void SaveReplacementRecords(IEnumerable<ReplaceInstallationRecord> records)
    {
        var historyPath = GetReplacementHistoryPath();
        var json = JsonSerializer.Serialize(records.ToList(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(historyPath, json);
    }

    public static void RecordReplacementInstallation(ReplaceInstallationRecord record)
    {
        var records = LoadReplacementRecords();
        records.RemoveAll(item => string.Equals(item.BackupId, record.BackupId, StringComparison.OrdinalIgnoreCase));
        records.Insert(0, record);
        SaveReplacementRecords(records);
    }

    public static string BackupOriginalFileForReplacement(string gameFolder, string originalFilePath, string modName, string? backupRoot = null)
    {
        if (string.IsNullOrWhiteSpace(originalFilePath) || !File.Exists(originalFilePath))
        {
            return string.Empty;
        }

        var targetBackupRoot = string.IsNullOrWhiteSpace(backupRoot) ? GetDefaultBackupRoot() : backupRoot;
        var relativePath = Path.GetRelativePath(gameFolder, originalFilePath)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var backupPath = Path.Combine(targetBackupRoot, SanitizeFolderName(modName), relativePath);
        var backupDirectory = Path.GetDirectoryName(backupPath) ?? targetBackupRoot;
        Directory.CreateDirectory(backupDirectory);
        File.Copy(originalFilePath, backupPath, true);

        return backupPath;
    }

    public static List<ModPackageInfo> DiscoverModPackages(string libraryRoot)
    {
        var result = new List<ModPackageInfo>();
        if (string.IsNullOrWhiteSpace(libraryRoot) || !Directory.Exists(libraryRoot))
        {
            return result;
        }

        foreach (var directory in Directory.GetDirectories(libraryRoot, "*", SearchOption.AllDirectories))
        {
            if (!IsModPackageRoot(directory))
            {
                continue;
            }

            var hasError = !TryValidateModJson(directory, out var configError);
            var payloadPath = GetPayloadDirectory(directory);
            var packageInfo = new ModPackageInfo
            {
                DisplayName = NormalizeDisplayName(Path.GetFileName(directory)),
                PackageRootPath = directory,
                PayloadPath = payloadPath,
                HasConfigError = hasError,
                ConfigError = configError
            };

            result.Add(packageInfo);
        }

        return result
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsMetadataOrNonInstallableFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        var fileName = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        if (string.Equals(fileName, "mod.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "config.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fileName.StartsWith("README", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("readme", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetPayloadDirectory(string packageRoot)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
        {
            return string.Empty;
        }

        var candidateDirectories = Directory.GetDirectories(packageRoot)
            .Where(dir => !string.Equals(Path.GetFileName(dir), "modloader", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Path.GetFileName(dir), "screen", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Path.GetFileName(dir), "preview", StringComparison.OrdinalIgnoreCase))
            .OrderBy(dir => dir, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in candidateDirectories)
        {
            var candidateName = Path.GetFileName(candidate);
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                continue;
            }

            if (string.Equals(candidateName, "config", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidateName.StartsWith("preview", StringComparison.OrdinalIgnoreCase) ||
                candidateName.StartsWith("readme", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return candidate;
        }

        var filesAtRoot = Directory.GetFiles(packageRoot)
            .Where(path => !string.Equals(Path.GetFileName(path), "mod.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filesAtRoot.Count > 0)
        {
            return packageRoot;
        }

        return string.Empty;
    }

    public static string? ResolvePackageRoot(string selectedPath)
    {
        if (Directory.Exists(selectedPath))
        {
            if (IsModPackageRoot(selectedPath))
            {
                return selectedPath;
            }

            foreach (var directory in Directory.GetDirectories(selectedPath, "*", SearchOption.AllDirectories))
            {
                if (IsModPackageRoot(directory))
                {
                    return directory;
                }
            }

            return null;
        }

        if (File.Exists(selectedPath) && IsArchive(selectedPath))
        {
            var tempExtractDir = Path.Combine(Path.GetTempPath(), "GtaSaModManager", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempExtractDir);
            ExtractArchiveAsync(selectedPath, tempExtractDir, null).GetAwaiter().GetResult();

            return ResolvePackageRoot(tempExtractDir);
        }

        return null;
    }

    public static string ResolveModName(string selectedPath, string sourceName)
    {
        if (Directory.Exists(selectedPath))
        {
            var folderName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return NormalizeDisplayName(folderName ?? sourceName);
        }

        if (File.Exists(selectedPath) && IsArchive(selectedPath))
        {
            var archiveName = Path.GetFileNameWithoutExtension(selectedPath);
            return NormalizeDisplayName(archiveName ?? sourceName);
        }

        return NormalizeDisplayName(sourceName);
    }

    public static string GetModRootPath(string gamePath, string modName)
    {
        return Path.Combine(GameService.GetModLoaderFolder(gamePath), modName);
    }

    public static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, IProgress<string>? progress, CancellationToken cancellationToken = default, bool excludeMetadataFiles = false)
    {
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException("Source directory not found.");
        }

        Directory.CreateDirectory(destinationDir);

        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Where(file => !excludeMetadataFiles || !IsMetadataOrNonInstallableFile(file))
            .ToArray();

        for (var i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[i];
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destination = Path.Combine(destinationDir, relativePath);
            var destinationDirectory = Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await Task.Run(() => File.Copy(file, destination, true), cancellationToken);
            progress?.Report(Path.GetFileName(file));
        }
    }

    public static async Task ExtractArchiveAsync(string archivePath, string destinationRoot, IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(archivePath).ToLowerInvariant();

        switch (extension)
        {
            case ".zip":
                ZipFile.ExtractToDirectory(archivePath, destinationRoot, overwriteFiles: true);
                break;
            case ".7z":
                var sevenZipExecutablePath = Path.Combine(AppContext.BaseDirectory, "7z.exe");
                if (!File.Exists(sevenZipExecutablePath))
                {
                    throw new InvalidOperationException(
                        $"7z.exe was not found next to the application. Please place 7z.exe in '{AppContext.BaseDirectory}' so archive extraction can work.");
                }

                await Task.Run(() =>
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sevenZipExecutablePath,
                        Arguments = $"x \"{archivePath}\" -o\"{destinationRoot}\" -y",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    using var process = System.Diagnostics.Process.Start(psi);
                    process!.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Archive extraction failed.");
                    }
                }, cancellationToken);
                break;
            case ".rar":
                throw new NotSupportedException("RAR extraction requires an external dependency and is not implemented in this version.");
            default:
                throw new InvalidOperationException("Unsupported archive format.");
        }

        progress?.Report("Extracted");
    }

    public static string ExtractTemporaryArchive(string archivePath)
    {
        var destinationPath = Path.Combine(Path.GetTempPath(), "GtaSaModManager", Path.GetFileNameWithoutExtension(archivePath));
        Directory.CreateDirectory(destinationPath);

        try
        {
            ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles: true);
            return destinationPath;
        }
        catch
        {
            return destinationPath;
        }
    }
}
