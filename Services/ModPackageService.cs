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
        var modJsonPath = Path.Combine(packagePath, "mod.json");
        if (!File.Exists(modJsonPath))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(modJsonPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "mod.json must contain a JSON object.";
                return false;
            }

            if (document.RootElement.EnumerateObject().Any())
            {
                error = "mod.json must be an empty JSON object {}.";
                return false;
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
        var manifestPath = Path.Combine(basePath, "mod.json");
        if (!File.Exists(manifestPath))
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

            return new ModManifest();
        }
        catch
        {
            return null;
        }
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

    public static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException("Source directory not found.");
        }

        Directory.CreateDirectory(destinationDir);

        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
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

            await Task.Run(() => File.Copy(file, destination, false), cancellationToken);
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
                if (!File.Exists("7z.exe"))
                {
                    throw new InvalidOperationException("7z support is not available on this system.");
                }

                await Task.Run(() =>
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "7z.exe",
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
