using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Conversion;

namespace GtaSaModManager.Services;

public sealed record OptionalAssetInstallFile(string SourcePath, string DestinationPath);

public static class OptionalAssetReplacementService
{
    public static bool ShouldRestorePreviousInstallation(
        string previousSourcePath,
        string previousOptionalKind,
        IEnumerable<string> previousDestinations,
        string currentSourcePath,
        string currentOptionalKind,
        IEnumerable<string> currentDestinations)
    {
        ArgumentNullException.ThrowIfNull(previousDestinations);
        ArgumentNullException.ThrowIfNull(currentDestinations);

        var previousFiles = previousDestinations
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentFiles = currentDestinations
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isSamePackage = string.Equals(previousSourcePath, currentSourcePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(previousOptionalKind, currentOptionalKind, StringComparison.OrdinalIgnoreCase);

        return isSamePackage
            ? !previousFiles.SetEquals(currentFiles)
            : previousFiles.Overlaps(currentFiles);
    }

    public static IReadOnlyList<OptionalAssetInstallFile> ResolveInstallFiles(
        string optionalPayloadPath,
        string destinationRoot,
        IEnumerable<string> installedAssetFiles,
        IReadOnlyList<InstallationAssetMapping> assetMappings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionalPayloadPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);
        ArgumentNullException.ThrowIfNull(installedAssetFiles);
        ArgumentNullException.ThrowIfNull(assetMappings);

        if (!Directory.Exists(optionalPayloadPath))
        {
            throw new DirectoryNotFoundException("The Optional payload folder could not be found.");
        }

        var installedPaths = installedAssetFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var modelFiles = Directory.GetFiles(optionalPayloadPath, "*", SearchOption.AllDirectories)
            .Where(IsModelFile)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path, optionalPayloadPath))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (modelFiles.Count == 0)
        {
            return Array.Empty<OptionalAssetInstallFile>();
        }

        var modelGroups = modelFiles
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var mappings = assetMappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.SourceModelName)
                && !string.IsNullOrWhiteSpace(mapping.TargetModelName))
            .ToList();
        if (mappings.Count == 0)
        {
            throw new InvalidDataException("The installed asset mapping is not available for this Optional package.");
        }

        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<OptionalAssetInstallFile>();
        foreach (var modelGroup in modelGroups)
        {
            var sourceModelName = modelGroup.Key ?? string.Empty;
            var matchingMappings = mappings
                .Where(mapping => string.Equals(mapping.SourceModelName, sourceModelName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mapping.TargetModelName, sourceModelName, StringComparison.OrdinalIgnoreCase)
                    || modelGroup.All(path => Path.GetExtension(path).Equals(".txd", StringComparison.OrdinalIgnoreCase)
                        && IsNumberedVariant(sourceModelName, mapping.SourceModelName)))
                .DistinctBy(mapping => mapping.SourceModelName + "|" + mapping.TargetModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (matchingMappings.Count == 0 && modelGroups.Count == 1 && mappings.Count == 1)
            {
                matchingMappings.Add(mappings[0]);
            }

            if (matchingMappings.Count != 1)
            {
                throw new InvalidDataException(
                    "The Optional model '" + sourceModelName + "' does not identify exactly one installed asset.");
            }

            var mapping = matchingMappings[0];
            foreach (var sourcePath in modelGroup)
            {
                var extension = Path.GetExtension(sourcePath);
                var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
                var mappedName = MapModelName(sourceName, mapping, extension);
                var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, mappedName + extension));
                if (!installedPaths.Contains(destinationPath))
                {
                    throw new InvalidDataException(
                        "The Optional model destination is not part of the installed asset: " + destinationPath);
                }

                if (!destinations.Add(destinationPath))
                {
                    throw new InvalidDataException("Multiple Optional files target the same installed model file: " + destinationPath);
                }

                result.Add(new OptionalAssetInstallFile(Path.GetFullPath(sourcePath), destinationPath));
            }
        }

        return result.AsReadOnly();
    }

    public static async Task<IReadOnlyList<OptionalAssetBackup>> InstallAsync(
        string gameFolder,
        string parentModName,
        IReadOnlyList<OptionalAssetInstallFile> files,
        IEnumerable<OptionalAssetBackup>? previousBackups = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentModName);
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            throw new InvalidDataException("No Optional model files matched the installed asset.");
        }

        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameFolder));
        var gamePrefix = gameRoot + Path.DirectorySeparatorChar;
        var uniqueFiles = files
            .GroupBy(file => Path.GetFullPath(file.DestinationPath), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (uniqueFiles.Any(group => group.Count() > 1))
        {
            throw new InvalidDataException("Multiple Optional files target the same installed model file.");
        }

        foreach (var file in files)
        {
            if (!File.Exists(file.SourcePath))
            {
                throw new FileNotFoundException("An Optional model file could not be found.", file.SourcePath);
            }

            if (!Path.GetFullPath(file.DestinationPath).StartsWith(gamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("An Optional asset destination escapes the game folder.");
            }
        }

        var previousByDestination = (previousBackups ?? Enumerable.Empty<OptionalAssetBackup>())
            .Where(backup => !string.IsNullOrWhiteSpace(backup.DestinationPath))
            .ToDictionary(backup => Path.GetFullPath(backup.DestinationPath), StringComparer.OrdinalIgnoreCase);
        var newOriginals = files
            .Select(file => Path.GetFullPath(file.DestinationPath))
            .Where(path => File.Exists(path) && !previousByDestination.ContainsKey(path))
            .ToList();
        var backupPlan = newOriginals.Count > 0
            ? BackupStorageService.CreatePlan(gameRoot, parentModName, newOriginals)
            : new BackupStoragePlan { HasBackup = true };
        if (newOriginals.Count > 0 && !backupPlan.HasBackup)
        {
            throw new IOException(backupPlan.ErrorMessage ?? "A backup could not be created for the installed asset.");
        }

        var backups = new List<OptionalAssetBackup>();
        var touched = new List<OptionalAssetBackup>();
        try
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = Path.GetFullPath(file.DestinationPath);
                OptionalAssetBackup backup;
                if (previousByDestination.TryGetValue(destination, out var previous))
                {
                    backup = new OptionalAssetBackup
                    {
                        DestinationPath = destination,
                        BackupFilePath = previous.BackupFilePath,
                        OriginalExisted = previous.OriginalExisted
                    };
                    if (backup.OriginalExisted)
                    {
                        if (string.IsNullOrWhiteSpace(backup.BackupFilePath) || !File.Exists(backup.BackupFilePath))
                        {
                            throw new FileNotFoundException("The original asset backup is missing.", backup.BackupFilePath);
                        }

                        await FileCopyService.CopyFileAsync(backup.BackupFilePath, destination, cancellationToken);
                    }
                    else if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                }
                else if (File.Exists(destination))
                {
                    var backupPath = await ModPackageService.BackupOriginalFileForReplacementAsync(
                        gameRoot,
                        destination,
                        parentModName,
                        backupPlan.BackupRoot,
                        cancellationToken);
                    backup = new OptionalAssetBackup
                    {
                        DestinationPath = destination,
                        BackupFilePath = backupPath,
                        OriginalExisted = true
                    };
                }
                else
                {
                    backup = new OptionalAssetBackup
                    {
                        DestinationPath = destination,
                        OriginalExisted = false
                    };
                }

                backups.Add(backup);
                touched.Add(backup);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await FileCopyService.CopyFileAsync(file.SourcePath, destination, cancellationToken);
            }

            return backups.AsReadOnly();
        }
        catch
        {
            await RestoreFilesWithoutCleanupAsync(touched, cancellationToken);
            foreach (var backup in backups.Where(backup => !previousByDestination.ContainsKey(backup.DestinationPath)))
            {
                DeleteBackupFile(backup.BackupFilePath);
            }

            throw;
        }
    }

    public static async Task RestoreAsync(
        string gameFolder,
        IEnumerable<OptionalAssetBackup> backups,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameFolder);
        ArgumentNullException.ThrowIfNull(backups);
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameFolder));
        var gamePrefix = gameRoot + Path.DirectorySeparatorChar;
        var items = backups.ToList();
        foreach (var backup in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = Path.GetFullPath(backup.DestinationPath);
            if (!destination.StartsWith(gamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("An Optional asset destination escapes the game folder.");
            }

            if (backup.OriginalExisted)
            {
                if (string.IsNullOrWhiteSpace(backup.BackupFilePath) || !File.Exists(backup.BackupFilePath))
                {
                    throw new FileNotFoundException("The original asset backup is missing.", backup.BackupFilePath);
                }

                await FileCopyService.CopyFileAsync(backup.BackupFilePath, destination, cancellationToken);
            }
            else if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }

        foreach (var backup in items)
        {
            DeleteBackupFile(backup.BackupFilePath);
        }
    }

    public static void DiscardBackups(IEnumerable<OptionalAssetBackup> backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        foreach (var backup in backups)
        {
            DeleteBackupFile(backup.BackupFilePath);
        }
    }

    private static async Task RestoreFilesWithoutCleanupAsync(
        IEnumerable<OptionalAssetBackup> backups,
        CancellationToken cancellationToken)
    {
        foreach (var backup in backups.Reverse())
        {
            if (backup.OriginalExisted && File.Exists(backup.BackupFilePath))
            {
                await FileCopyService.CopyFileAsync(backup.BackupFilePath, backup.DestinationPath, cancellationToken);
            }
            else if (!backup.OriginalExisted && File.Exists(backup.DestinationPath))
            {
                File.Delete(backup.DestinationPath);
            }
        }
    }

    private static void DeleteBackupFile(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string MapModelName(
        string sourceName,
        InstallationAssetMapping mapping,
        string extension)
    {
        if (string.Equals(sourceName, mapping.TargetModelName, StringComparison.OrdinalIgnoreCase))
        {
            return mapping.TargetModelName;
        }

        if (string.Equals(sourceName, mapping.SourceModelName, StringComparison.OrdinalIgnoreCase))
        {
            return mapping.TargetModelName;
        }

        if (extension.Equals(".txd", StringComparison.OrdinalIgnoreCase)
            && sourceName.StartsWith(mapping.SourceModelName, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = sourceName[mapping.SourceModelName.Length..];
            if (suffix.Length > 0 && suffix.All(char.IsDigit))
            {
                return mapping.TargetModelName + suffix;
            }
        }

        if (extension.Equals(".txd", StringComparison.OrdinalIgnoreCase)
            && sourceName.StartsWith(mapping.TargetModelName, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = sourceName[mapping.TargetModelName.Length..];
            if (suffix.Length > 0 && suffix.All(char.IsDigit))
            {
                return mapping.TargetModelName + suffix;
            }
        }

        return mapping.TargetModelName;
    }

    private static bool IsNumberedVariant(string modelName, string baseName)
    {
        if (!modelName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = modelName[baseName.Length..];
        return suffix.Length > 0 && suffix.All(char.IsDigit);
    }

    private static bool IsModelFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".dff", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txd", StringComparison.OrdinalIgnoreCase);
    }
}