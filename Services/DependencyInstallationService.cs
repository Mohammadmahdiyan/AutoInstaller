using System.Text.Json;

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

    public static bool AreDependenciesInstalled(string gameFolder)
    {
        var filesInstalled = RequiredFiles.All(file => File.Exists(Path.Combine(gameFolder, file)));
        var directoriesInstalled = RequiredDirectories.All(directory => Directory.Exists(Path.Combine(gameFolder, directory)));
        return filesInstalled || directoriesInstalled;
    }

    public static async Task<DependencyInstallationResult> EnsureInstalledAsync(
        string gameFolder,
        string baseModsFolder,
        CancellationToken cancellationToken = default)
    {
        if (AreDependenciesInstalled(gameFolder))
        {
            return new DependencyInstallationResult { Success = true };
        }

        if (string.IsNullOrWhiteSpace(baseModsFolder) || !Directory.Exists(baseModsFolder))
        {
            return Failure("The Base Mods Folder is not configured or does not exist.");
        }

        var dependencyRoot = Path.Combine(baseModsFolder, "Scripts", "A1-MyReqFiles");
        var configPath = ModPackageService.GetManifestPath(dependencyRoot);
        if (!Directory.Exists(dependencyRoot) || string.IsNullOrWhiteSpace(configPath))
        {
            return Failure("Missing dependency package manifest: " + dependencyRoot);
        }

        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath, cancellationToken));
            var dependencyType = ReadType(document.RootElement);
            var payloadRoot = Directory.Exists(Path.Combine(dependencyRoot, "Essentials"))
                ? Path.Combine(dependencyRoot, "Essentials")
                : dependencyRoot;

            if (string.Equals(dependencyType, "putingamefolder", StringComparison.OrdinalIgnoreCase)
                || string.Equals(dependencyType, "pgf", StringComparison.OrdinalIgnoreCase))
            {
                var copiedDependencyEntries = await CopyDirectoryContentsAsync(payloadRoot, gameFolder, cancellationToken);
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

            return Failure("This dependency manifest type is not supported. Only PutInGameFolder (PGF) dependencies can be auto-installed.");
        }
        catch (JsonException ex)
        {
            return Failure("The dependency manifest is invalid: " + ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Failure("Dependency installation failed: " + ex.Message);
        }
    }

    private static string ReadType(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, "type", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
                }
            }
        }

        return string.Empty;
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

    private static async Task<int> CopyDirectoryContentsAsync(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken)
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
            await Task.Run(() => File.Copy(file, destination, true), cancellationToken);
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
