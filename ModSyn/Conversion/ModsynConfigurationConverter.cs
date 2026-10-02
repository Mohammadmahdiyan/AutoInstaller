using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Ast;
using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Modsyn.Conversion;

public static class ModsynConfigurationConverter
{
    public static ModsynConvertedConfiguration Convert(
        ModsynDocumentNode document,
        ModsynValidationResult validation,
        string? packageRoot = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation.IsValid)
        {
            throw new ArgumentException("Only a semantically valid Modsyn document can be converted.", nameof(validation));
        }

        var root = document.Body;
        var installPaths = ReadPaths(root, "installThis", "installThese");
        var ignorePaths = ReadPaths(root, "ignoreThis", "ignoreThese");
        var (installFiles, installFolders) = ClassifyPackagePaths(installPaths, packageRoot);
        var (ignoreFiles, ignoreFolders) = ClassifyPackagePaths(ignorePaths, packageRoot);
        var manifest = new ModManifest
        {
            Type = validation.NormalizedType,
            DeleteThis = ReadPaths(root, "deleteThis", "deleteThese"),
            InstallFiles = installFiles,
            InstallFolders = installFolders,
            IgnoreFiles = ignoreFiles,
            IgnoreFolders = ignoreFolders
        };
        manifest.Requires.AddRange(validation.Requirements.Select(requirement => new ModRequirementEntry
        {
            CheckPaths = BuildRequirementPaths(requirement),
            ReqAddress = requirement.RequestAddress
        }));

        var replacements = ReadReplacements(root);
        var backup = new ModsynBackupConfiguration(
            validation.BackupEnabled,
            ReadPaths(root, "backupThis", "backupThese").AsReadOnly(),
            ReadPaths(root, "dontBackupThis", "dontBackupThese").AsReadOnly());

        return new ModsynConvertedConfiguration(
            manifest,
            replacements.AsReadOnly(),
            backup,
            validation.Requirements);
    }

    public static ModsynConvertedConfiguration Convert(ModsynDocumentNode document, string? packageRoot = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var validation = ModsynValidator.Validate(document);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                "The Modsyn document is invalid: " + string.Join(" ", validation.Errors),
                nameof(document));
        }

        return Convert(document, validation, packageRoot);
    }

    private static List<string> ReadPaths(ModsynObjectNode root, string singularName, string pluralName)
    {
        var paths = new List<string>();
        foreach (var property in root.Properties)
        {
            if (string.Equals(property.Name, singularName, StringComparison.Ordinal)
                && property.Value is ModsynStringNode singular)
            {
                paths.Add(singular.Value);
            }
            else if (string.Equals(property.Name, pluralName, StringComparison.Ordinal)
                && property.Value is ModsynArrayNode plural)
            {
                paths.AddRange(plural.Items.OfType<ModsynStringNode>().Select(item => item.Value));
            }
        }

        return NormalizePaths(paths);
    }

    private static List<string> BuildRequirementPaths(ModsynResolvedRequirement requirement)
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(requirement.CheckThis))
        {
            paths.Add(requirement.CheckThis);
        }

        paths.AddRange(requirement.CheckThese);
        return NormalizePaths(paths);
    }

    private static List<string> NormalizePaths(IEnumerable<string> paths)
    {
        return paths
            .Select(path => path.Trim().Replace('/', '\\'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (List<string> Files, List<string> Folders) ClassifyPackagePaths(
        IEnumerable<string> paths,
        string? packageRoot)
    {
        var files = new List<string>();
        var folders = new List<string>();
        var fullRoot = !string.IsNullOrWhiteSpace(packageRoot) && Directory.Exists(packageRoot)
            ? Path.GetFullPath(packageRoot)
            : null;

        foreach (var path in paths)
        {
            if (fullRoot is not null && IsExistingPackageDirectory(fullRoot, path))
            {
                folders.Add(path);
            }
            else
            {
                files.Add(path);
            }
        }

        return (files, folders);
    }

    private static bool IsExistingPackageDirectory(string packageRoot, string relativePath)
    {
        var platformPath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(platformPath))
        {
            return false;
        }

        var rootPrefix = Path.TrimEndingDirectorySeparator(packageRoot) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(packageRoot, platformPath));
        return fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(fullPath);
    }

    private static List<ModReplacementEntry> ReadReplacements(ModsynObjectNode root)
    {
        var replacements = new List<ModReplacementEntry>();
        foreach (var property in root.Properties.Where(property => property.Name == "replacements"))
        {
            if (property.Value is not ModsynArrayNode array)
            {
                continue;
            }

            foreach (var item in array.Items)
            {
                if (item is ModsynStringNode shorthand)
                {
                    var path = NormalizePath(shorthand.Value);
                    replacements.Add(new ModReplacementEntry(path, path));
                }
                else if (item is ModsynObjectNode replacement)
                {
                    var source = FindString(replacement, "source");
                    var target = FindString(replacement, "target");
                    if (source is not null && target is not null)
                    {
                        replacements.Add(new ModReplacementEntry(NormalizePath(source), NormalizePath(target)));
                    }
                }
            }
        }

        return replacements;
    }

    private static string? FindString(ModsynObjectNode value, string propertyName)
    {
        return value.Properties
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal))?
            .Value is ModsynStringNode stringNode
                ? stringNode.Value
                : null;
    }

    private static string NormalizePath(string path)
    {
        return path.Trim().Replace('/', '\\');
    }
}