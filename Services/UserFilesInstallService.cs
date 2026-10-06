using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public sealed record UserFilesInstallCopy(string SourcePath, string DestinationPath);

public static class UserFilesInstallService
{
    public static IReadOnlyList<UserFilesInstallCopy> CreateCopyPlan(
        IEnumerable<ModUserFileInstallEntry>? entries,
        string packageRoot,
        string baseModsRoot,
        string userFilesRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(userFilesRoot);

        var packagePath = Path.GetFullPath(packageRoot);
        var userFilesPath = Path.GetFullPath(userFilesRoot);
        var baseModsPath = string.IsNullOrWhiteSpace(baseModsRoot) ? string.Empty : Path.GetFullPath(baseModsRoot);
        var plan = new List<UserFilesInstallCopy>();

        foreach (var entry in entries ?? Enumerable.Empty<ModUserFileInstallEntry>())
        {
            var hasFrom = !string.IsNullOrWhiteSpace(entry.From);
            var hasFromBase = !string.IsNullOrWhiteSpace(entry.FromBase);
            if (hasFrom == hasFromBase)
            {
                throw new InvalidDataException("Each addToUserFile entry requires exactly one of 'from' or 'fromBase'.");
            }

            var sourceRoot = hasFrom ? packagePath : baseModsPath;
            if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
            {
                throw new InvalidDataException("The source root for addToUserFile could not be found.");
            }

            var sourceRelativePath = hasFrom ? entry.From! : entry.FromBase!;
            var sourcePath = ResolveRelativePath(sourceRoot, sourceRelativePath, allowEmpty: false);
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                throw new FileNotFoundException("An addToUserFile source does not exist.", sourcePath);
            }

            var destinationFolder = ResolveRelativePath(userFilesPath, entry.To ?? string.Empty, allowEmpty: true);
            if (File.Exists(sourcePath))
            {
                plan.Add(new UserFilesInstallCopy(
                    sourcePath,
                    Path.Combine(destinationFolder, Path.GetFileName(sourcePath))));
                continue;
            }

            var destinationRoot = Path.Combine(destinationFolder, Path.GetFileName(Path.TrimEndingDirectorySeparator(sourcePath)));
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativeFile = Path.GetRelativePath(sourcePath, file);
                plan.Add(new UserFilesInstallCopy(file, Path.Combine(destinationRoot, relativeFile)));
            }
        }

        var duplicateDestination = plan
            .GroupBy(copy => Path.GetFullPath(copy.DestinationPath), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDestination is not null)
        {
            throw new InvalidDataException("Multiple addToUserFile sources target the same destination file.");
        }

        return plan.AsReadOnly();
    }

    private static string ResolveRelativePath(string root, string relativePath, bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            if (allowEmpty)
            {
                return root;
            }

            throw new InvalidDataException("An addToUserFile source path cannot be empty.");
        }

        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized)
            || normalized.Split(Path.DirectorySeparatorChar).Any(segment => segment == ".."))
        {
            throw new InvalidDataException("addToUserFile paths must stay inside their configured root.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var relativeToRoot = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(relativeToRoot)
            || relativeToRoot == ".."
            || relativeToRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("addToUserFile paths must stay inside their configured root.");
        }

        return fullPath;
    }
}