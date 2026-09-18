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
        return RequiredFiles.All(file => File.Exists(Path.Combine(gameFolder, file)))
            && RequiredDirectories.All(directory => Directory.Exists(Path.Combine(gameFolder, directory)));
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
        var configPath = Path.Combine(dependencyRoot, "config.json");
        if (!Directory.Exists(dependencyRoot) || !File.Exists(configPath))
        {
            return Failure("Missing dependency package or config.json: " + dependencyRoot);
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

            var entries = ReadEntries(document.RootElement);
            if (entries.Count == 0)
            {
                return Failure("The dependency config.json does not contain install entries.");
            }

            var installedEntries = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = GetSafeSourcePath(dependencyRoot, entry.Source);
                var destinationPath = GetSafeDestinationPath(gameFolder, entry.Destination);

                if (File.Exists(sourcePath))
                {
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    await Task.Run(() => File.Copy(sourcePath, destinationPath, true), cancellationToken);
                    installedEntries++;
                }
                else if (Directory.Exists(sourcePath))
                {
                    await CopyDirectoryAsync(sourcePath, destinationPath, cancellationToken);
                    installedEntries++;
                }
                else
                {
                    return Failure("Dependency source was not found: " + entry.Source);
                }
            }

            var success = AreDependenciesInstalled(gameFolder);
            return success
                ? new DependencyInstallationResult
                {
                    Success = true,
                    DependenciesWereMissing = true,
                    InstalledEntries = installedEntries
                }
                : Failure("Dependency installation completed, but one or more required files or folders are still missing.", true, installedEntries);
        }
        catch (JsonException ex)
        {
            return Failure("The dependency config.json is invalid: " + ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Failure("Dependency installation failed: " + ex.Message);
        }
    }

    private static List<DependencyEntry> ReadEntries(JsonElement root)
    {
        var entries = new List<DependencyEntry>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            AddArrayEntries(root, entries);
            return entries;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return entries;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("files") || property.NameEquals("folders") || property.NameEquals("directories"))
            {
                AddObjectEntries(property.Value, entries);
            }
            else if (property.NameEquals("install") || property.NameEquals("entries") || property.NameEquals("items"))
            {
                AddArrayEntries(property.Value, entries);
            }
            else if (property.NameEquals("type"))
            {
                continue;
            }
            else if (property.Value.ValueKind == JsonValueKind.String)
            {
                entries.Add(new DependencyEntry(property.Name, property.Value.GetString()!));
            }
        }

        return entries;
    }

    private static string ReadType(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("type", out var typeProperty)
            && typeProperty.ValueKind == JsonValueKind.String)
        {
            return typeProperty.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        return string.Empty;
    }

    private static void AddObjectEntries(JsonElement value, List<DependencyEntry> entries)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                entries.Add(new DependencyEntry(property.Name, property.Value.GetString()!));
            }
        }
    }

    private static void AddArrayEntries(JsonElement value, List<DependencyEntry> entries)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var source = GetString(item, "source", "from", "path", "file", "folder");
            var destination = GetString(item, "destination", "target", "to", "installPath") ?? source;
            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(destination))
            {
                entries.Add(new DependencyEntry(source, destination));
            }
        }
    }

    private static string? GetString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static string GetSafeSourcePath(string root, string relativePath)
    {
        return GetSafePath(root, relativePath, "Dependency source");
    }

    private static string GetSafeDestinationPath(string root, string relativePath)
    {
        return GetSafePath(root, relativePath, "Dependency destination");
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

    private static async Task CopyDirectoryAsync(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destination = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await Task.Run(() => File.Copy(file, destination, true), cancellationToken);
        }
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

    private sealed record DependencyEntry(string Source, string Destination);
}
