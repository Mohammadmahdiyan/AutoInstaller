namespace GtaSaModManager.Services;

public sealed class DependencyInstallationResult
{
    public bool Success { get; init; }
    public bool DependenciesWereMissing { get; init; }
    public int InstalledEntries { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}

public static class DependencyInstallationService
{
    private static readonly string[] RequiredFiles = { "cleo.asi", "modloader.asi" };
    private static readonly string[] RequiredDirectories = { "cleo", "modloader" };

    /// <summary>
    /// True only when cleo.asi, modloader.asi and both the cleo and modloader folders exist.
    /// (It used to be "files OR folders": after an old-CLEO mod was deleted the two empty folders were
    /// enough to make the manager believe CLEO/ModLoader were installed, so ReqFiles was never reinstalled.)
    /// </summary>
    public static bool AreDependenciesInstalled(string gameFolder)
    {
        var filesInstalled = RequiredFiles.All(file => File.Exists(Path.Combine(gameFolder, file)));
        var directoriesInstalled = RequiredDirectories.All(directory => Directory.Exists(Path.Combine(gameFolder, directory)));
        return filesInstalled && directoriesInstalled;
    }

    public static async Task<DependencyInstallationResult> EnsureInstalledAsync(
        string gameFolder,
        string dependencyRoot,
        CancellationToken cancellationToken = default,
        bool forceReinstall = false)
    {
        if (!forceReinstall && AreDependenciesInstalled(gameFolder))
        {
            return new DependencyInstallationResult { Success = true };
        }

        if (string.IsNullOrWhiteSpace(dependencyRoot) || !Directory.Exists(dependencyRoot))
        {
            return Failure("The selected Essentials dependency folder does not exist.");
        }

        if (ModPackageService.GetManifestPath(dependencyRoot) is null)
        {
            return Failure("Missing dependency Modsyn configuration: " + dependencyRoot);
        }

        if (!ModPackageService.TryReadModsynConfiguration(dependencyRoot, out var configuration, out var configError))
        {
            return Failure("The dependency Modsyn configuration is invalid: " + configError);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dependencyType = configuration!.Manifest.NormalizedType;
            var payloadRoot = Directory.Exists(Path.Combine(dependencyRoot, "Essentials"))
                ? Path.Combine(dependencyRoot, "Essentials")
                : dependencyRoot;

            if (string.Equals(dependencyType, "putingamefolder", StringComparison.OrdinalIgnoreCase))
            {
                var copiedDependencyEntries = await CopyDirectoryContentsAsync(payloadRoot, gameFolder, cancellationToken, forceReinstall);
                var dependenciesComplete = AreDependenciesInstalled(gameFolder);
                return dependenciesComplete
                    ? new DependencyInstallationResult
                    {
                        Success = true,
                        DependenciesWereMissing = true,
                        InstalledEntries = copiedDependencyEntries
                    }
                    : Failure("Dependency installation completed, but one or more required files or folders are still missing.", true, copiedDependencyEntries);
            }

            return Failure("This dependency Modsyn type is not supported. Only PutInGameFolder (PGF) dependencies can be auto-installed.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Failure("Dependency installation failed: " + ex.Message);
        }
    }

    private static string GetSafePath(string root, string relativePath, string label)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException(label + " must be a relative path.");
        }

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(label + " escapes its root folder.");
        }

        return fullPath;
    }

    private static bool IsUserConfigurationFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".ini", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> CopyDirectoryContentsAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken,
        bool overwriteExisting = false)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("Dependency payload folder not found: " + sourceDirectory);
        }

        var copiedEntries = 0;
        Directory.CreateDirectory(destinationDirectory);
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(GetSafePath(destinationDirectory, relative, "Dependency destination"));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destination = GetSafePath(destinationDirectory, relative, "Dependency destination");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination))
            {
                // Repair mode (default): never overwrite what is already in the game (user settings, a
                // CLEO/ModLoader version required by an installed mod, ...). Only missing files are put back.
                // Reinstall mode: the Essentials files replace the existing ones, except the user's
                // configuration files (.ini/.log/.json), so settings are not reset.
                if (!overwriteExisting || IsUserConfigurationFile(destination))
                {
                    continue;
                }

                var existing = destination;
                await Task.Run(() => File.Copy(file, existing, true), cancellationToken);
                copiedEntries++;
                continue;
            }

            await Task.Run(() => File.Copy(file, destination, false), cancellationToken);
            copiedEntries++;
        }

        return copiedEntries;
    }

    private static DependencyInstallationResult Failure(string message, bool dependenciesWereMissing = true, int installedEntries = 0)
    {
        return new DependencyInstallationResult
        {
            Success = false,
            DependenciesWereMissing = dependenciesWereMissing,
            InstalledEntries = installedEntries,
            ErrorMessage = message
        };
    }
}
