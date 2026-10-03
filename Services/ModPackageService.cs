using System.IO.Compression;
using System.Text.Json;
using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Conversion;
using GtaSaModManager.Modsyn.Parser;
using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Services;

public sealed record SelectedInstallEntry(string SourcePath, string RelativeDestination);

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

        if (GetManifestPath(directoryPath) == null)
        {
            return true;
        }

        return TryValidateModsyn(directoryPath, out _);
    }

    public static bool TryValidateConfigJson(string packagePath, out string? error)
    {
        return TryValidateModsyn(packagePath, out error);
    }

    public static bool TryValidateModJson(string packagePath, out string? error)
    {
        return TryValidateModsyn(packagePath, out error);
    }

    public static bool TryValidateModsyn(string packagePath, out string? error)
    {
        return TryReadModsynConfiguration(packagePath, out _, out error);
    }

    public static bool TryReadModsynConfiguration(
        string packagePath,
        out ModsynConvertedConfiguration? configuration,
        out string? error)
    {
        configuration = null;
        error = null;
        var manifestPath = GetManifestPath(packagePath);

        try
        {
            var source = string.IsNullOrWhiteSpace(manifestPath) ? "mod {}" : File.ReadAllText(manifestPath);
            var document = ModsynParser.Parse(source);
            var validation = ModsynValidator.Validate(document);
            if (!validation.IsValid)
            {
                error = string.Join(Environment.NewLine, validation.Errors);
                return false;
            }

            configuration = ModsynConfigurationConverter.Convert(document, validation, packagePath);
            if (configuration.Manifest.NormalizedType == "putandreplace" && configuration.Replacements.Count == 0)
            {
                configuration = null;
                error = "PutAndReplace requires at least one valid replacement entry.";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or GtaSaModManager.Modsyn.Lexer.ModsynLexException
            or GtaSaModManager.Modsyn.Parser.ModsynParseException)
        {
            error = ex.Message;
            return false;
        }
    }

    public static ModManifest? TryReadManifest(string basePath)
    {
        return TryReadModsynConfiguration(basePath, out var configuration, out _)
            ? configuration!.Manifest
            : null;
    }

    public static string? GetManifestPath(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
        {
            return null;
        }

        var folderName = Path.GetFileName(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var candidateNames = new[] { "mod.modsyn", "config.modsyn", folderName + ".modsyn" };
        try
        {
            var files = Directory.EnumerateFiles(basePath, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach (var candidateName in candidateNames)
            {
                var match = files.FirstOrDefault(file =>
                    string.Equals(Path.GetFileName(file), candidateName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    public static ModManifest ResolveManifest(string basePath)
    {
        return TryReadManifest(basePath) ?? new ModManifest();
    }

    public static ModsynConvertedConfiguration ResolveModsynConfiguration(string packageRoot)
    {
        if (!TryReadModsynConfiguration(packageRoot, out var configuration, out var error))
        {
            throw new InvalidDataException(error ?? "The Modsyn package configuration is invalid.");
        }

        return configuration!;
    }

    public static List<ModReplacementEntry> ReadReplacementEntries(string packageRoot)
    {
        if (!TryReadModsynConfiguration(packageRoot, out var configuration, out var error))
        {
            throw new InvalidDataException(error ?? "The Modsyn package configuration is invalid.");
        }

        var result = configuration!.Replacements.ToList();
        if (result.Count == 0 && ResolveManifest(packageRoot).NormalizedType == "putandreplace")
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

    public static bool TryRestoreReplacementInstallations(string modName)
    {
        var records = LoadReplacementRecords()
            .Where(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (records.Count == 0)
        {
            return false;
        }

        var restored = false;
        foreach (var record in records)
        {
            if (File.Exists(record.BackupFilePath) && !string.IsNullOrWhiteSpace(record.OriginalFilePath))
            {
                var directory = Path.GetDirectoryName(record.OriginalFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(record.BackupFilePath, record.OriginalFilePath, true);
                restored = true;
            }
            else if (File.Exists(record.OriginalFilePath))
            {
                File.Delete(record.OriginalFilePath);
                restored = true;
            }
        }

        var remaining = LoadReplacementRecords();
        remaining.RemoveAll(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase));
        SaveReplacementRecords(remaining);
        return restored;
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

            var hasError = !TryValidateModsyn(directory, out var configError);
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
        return IsMetadataOrNonInstallableFile(path, null);
    }

    public static bool IsMetadataOrNonInstallableFile(string path, string? packageRoot)
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

        if (!string.IsNullOrWhiteSpace(packageRoot))
        {
            var manifestPath = GetManifestPath(packageRoot);
            if (!string.IsNullOrWhiteSpace(manifestPath)
                && string.Equals(Path.GetFullPath(path), Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsLegacyJsonManifestFile(path, packageRoot))
            {
                return true;
            }
        }
        else if (string.Equals(fileName, "mod.modsyn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "config.modsyn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "mod.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "config.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalizedName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
        if (normalizedName.Contains("readme", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsMediaFile(fileName);
    }

    private static bool IsLegacyJsonManifestFile(string path, string packageRoot)
    {
        var packageName = Path.GetFileName(packageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var fileName = Path.GetFileName(path);
        return string.Equals(fileName, "config.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "mod.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, packageName + ".json", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMediaFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetInstallPayloadDirectory(string packageRoot, ModManifest manifest)
    {
        if (manifest.NormalizedType == "putincleo"
            || manifest.IsSingleAssetPackage
            || manifest.IsMultiAssetPackage)
        {
            return packageRoot;
        }

        return GetPayloadDirectory(packageRoot);
    }

    public static List<SelectedInstallEntry> ResolveInstallSelection(string packageRoot, ModManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
        {
            throw new InvalidDataException("The package root does not exist: " + packageRoot);
        }

        var root = Path.GetFullPath(packageRoot);
        var installFiles = NormalizeSelectionPaths(manifest.InstallFiles, "InstallFiles", root);
        var installFolders = NormalizeSelectionPaths(manifest.InstallFolders, "InstallFolders", root);
        var ignoreFiles = NormalizeSelectionPaths(manifest.IgnoreFiles, "IgnoreFiles", root);
        var ignoreFolders = NormalizeSelectionPaths(manifest.IgnoreFolders, "IgnoreFolders", root);
        var explicitlyInstalledFolders = installFolders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = new List<SelectedInstallEntry>();
        var entryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddFile(string sourcePath, string relativeDestination)
        {
            var sourceRelativePath = GetPackageRelativePath(root, sourcePath);
            if (IsMetadataOrNonInstallableFile(sourcePath, root)
                || IsMediaFile(sourcePath)
                || IsIgnoredFile(sourceRelativePath, ignoreFiles)
                || IsInIgnoredFolder(sourceRelativePath, ignoreFolders)
                || ContainsImplicitlyExcludedFolder(sourceRelativePath, explicitlyInstalledFolders))
            {
                return;
            }

            var normalizedDestination = NormalizeRelativeDestination(relativeDestination);
            var key = Path.GetFullPath(sourcePath) + "\0" + normalizedDestination;
            if (entryKeys.Add(key))
            {
                entries.Add(new SelectedInstallEntry(Path.GetFullPath(sourcePath), normalizedDestination));
            }
        }

        if (manifest.HasInstallFiles)
        {
            foreach (var relativePath in installFiles)
            {
                var sourcePath = ResolveSafePackagePath(root, relativePath, "InstallFiles");
                if (!File.Exists(sourcePath))
                {
                    throw new InvalidDataException("InstallFiles entry was not found: " + relativePath);
                }

                AddFile(sourcePath, Path.GetFileName(sourcePath));
            }
        }
        else
        {
            foreach (var sourcePath in Directory.GetFiles(root, "*", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                AddFile(sourcePath, Path.GetFileName(sourcePath));
            }
        }

        if (manifest.HasInstallFolders)
        {
            foreach (var relativeFolder in installFolders)
            {
                var folderPath = ResolveSafePackagePath(root, relativeFolder, "InstallFolders");
                if (!Directory.Exists(folderPath))
                {
                    throw new InvalidDataException("InstallFolders entry was not found: " + relativeFolder);
                }

                AddFolderFiles(folderPath);
            }
        }
        else if (manifest.HasIgnoreFolders)
        {
            foreach (var folderPath in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativeFolder = GetPackageRelativePath(root, folderPath);
                if (IsInIgnoredFolder(relativeFolder, ignoreFolders)
                    || IsProtectedFolderName(Path.GetFileName(folderPath)))
                {
                    continue;
                }

                AddFolderFiles(folderPath);
            }
        }

        void AddFolderFiles(string folderPath)
        {
            foreach (var sourcePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = GetPackageRelativePath(root, sourcePath);
                if (!IsInIgnoredFolder(relativePath, ignoreFolders)
                    && !ContainsImplicitlyExcludedFolder(relativePath, explicitlyInstalledFolders))
                {
                    AddFile(sourcePath, relativePath);
                }
            }
        }

        var destinationCollision = entries
            .GroupBy(entry => entry.RelativeDestination, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Select(entry => entry.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList())
            .FirstOrDefault(sources => sources.Count > 1);
        if (destinationCollision != null)
        {
            throw new InvalidDataException(
                "Install destination collision between '" + destinationCollision[0] + "' and '" + destinationCollision[1] + "'.");
        }

        return entries;
    }

    private static List<string> NormalizeSelectionPaths(IEnumerable<string>? paths, string propertyName, string packageRoot)
    {
        var normalizedPaths = new List<string>();
        foreach (var path in paths ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            normalizedPaths.Add(ResolveSafePackageRelativePath(packageRoot, path, propertyName));
        }

        return normalizedPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ResolveSafePackagePath(string packageRoot, string relativePath, string propertyName)
    {
        var safeRelativePath = ResolveSafePackageRelativePath(packageRoot, relativePath, propertyName);
        return Path.GetFullPath(Path.Combine(packageRoot, safeRelativePath.Replace('\\', Path.DirectorySeparatorChar)));
    }

    private static string ResolveSafePackageRelativePath(string packageRoot, string relativePath, string propertyName)
    {
        var normalizedPath = relativePath.Replace('/', '\\');
        if (Path.IsPathRooted(relativePath)
            || Path.IsPathRooted(normalizedPath)
            || normalizedPath.Split('\\', StringSplitOptions.RemoveEmptyEntries).Any(part => part == ".."))
        {
            throw new InvalidDataException(propertyName + " contains a path outside the package root: " + relativePath);
        }

        var fullRoot = Path.GetFullPath(packageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, normalizedPath.Replace('\\', Path.DirectorySeparatorChar)));
        if (!IsPathWithinRoot(fullRoot, fullPath))
        {
            throw new InvalidDataException(propertyName + " contains a path outside the package root: " + relativePath);
        }

        return NormalizeRelativeDestination(Path.GetRelativePath(fullRoot, fullPath));
    }

    private static bool IsPathWithinRoot(string root, string path)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (string.Equals(normalizedRoot, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rootPrefix = Path.EndsInDirectorySeparator(normalizedRoot)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPackageRelativePath(string packageRoot, string path)
    {
        return NormalizeRelativeDestination(Path.GetRelativePath(packageRoot, path));
    }

    private static string NormalizeRelativeDestination(string path)
    {
        return path.Replace('/', '\\').Trim('\\');
    }

    private static bool IsIgnoredFile(string relativePath, IEnumerable<string> ignoreFiles)
    {
        var fileName = Path.GetFileName(relativePath);
        foreach (var ignoredPath in ignoreFiles)
        {
            if (ignoredPath.Contains('\\'))
            {
                if (string.Equals(relativePath, ignoredPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(fileName, ignoredPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInIgnoredFolder(string relativePath, IEnumerable<string> ignoreFolders)
    {
        foreach (var ignoredFolder in ignoreFolders)
        {
            if (string.Equals(relativePath, ignoredFolder, StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith(ignoredFolder + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsImplicitlyExcludedFolder(string relativeFilePath, HashSet<string> explicitlyInstalledFolders)
    {
        var segments = relativeFilePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        var folderSegments = segments.Take(Math.Max(0, segments.Length - 1));
        var currentPath = string.Empty;
        foreach (var segment in folderSegments)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : currentPath + "\\" + segment;
            if (IsProtectedFolderName(segment) && !explicitlyInstalledFolders.Contains(currentPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProtectedFolderName(string folderName)
    {
        return folderName.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || folderName.Equals(".vscode", StringComparison.OrdinalIgnoreCase)
            || folderName.Equals(".vs", StringComparison.OrdinalIgnoreCase);
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
            .Where(path => !IsResolvedManifestFile(path, packageRoot))
            .ToList();

        if (filesAtRoot.Count > 0)
        {
            return packageRoot;
        }

        return string.Empty;
    }

    private static bool IsResolvedManifestFile(string path, string packageRoot)
    {
        var manifestPath = GetManifestPath(packageRoot);
        return !string.IsNullOrWhiteSpace(manifestPath)
            && string.Equals(Path.GetFullPath(path), Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase);
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
            .Where(file => !excludeMetadataFiles || !IsMetadataOrNonInstallableFile(file, sourceDir))
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
