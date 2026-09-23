namespace GtaSaModManager.Models;

public sealed class ModRequirementEntry
{
    public string? CheckFile { get; set; }

    public string? CheckFolder { get; set; }

    public List<string> CheckFiles { get; set; } = new();

    public List<string> CheckFolders { get; set; } = new();

    public string? ReqAddress { get; set; }

    public IReadOnlyCollection<string> FilesToCheck =>
        (CheckFiles ?? new List<string>()).Concat(!string.IsNullOrWhiteSpace(CheckFile) ? new[] { CheckFile } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public IReadOnlyCollection<string> FoldersToCheck =>
        (CheckFolders ?? new List<string>()).Concat(!string.IsNullOrWhiteSpace(CheckFolder) ? new[] { CheckFolder } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public bool IsEmpty => string.IsNullOrWhiteSpace(CheckFile)
        && string.IsNullOrWhiteSpace(CheckFolder)
        && (CheckFiles == null || CheckFiles.Count == 0)
        && (CheckFolders == null || CheckFolders.Count == 0)
        && string.IsNullOrWhiteSpace(ReqAddress);
}

public sealed class ModConflictCleanupEntry
{
    public string? File { get; set; }

    public string? Folder { get; set; }

    public List<string> Files { get; set; } = new();

    public List<string> Folders { get; set; } = new();

    public string? ReplaceWith { get; set; }

    public List<string> ReplacesWith { get; set; } = new();

    public IReadOnlyCollection<string> FilesToRemove =>
        (Files ?? new List<string>()).Concat(!string.IsNullOrWhiteSpace(File) ? new[] { File } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public IReadOnlyCollection<string> FoldersToRemove =>
        (Folders ?? new List<string>()).Concat(!string.IsNullOrWhiteSpace(Folder) ? new[] { Folder } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public IReadOnlyCollection<string> ReplacementPaths =>
        (ReplacesWith ?? new List<string>()).Concat(!string.IsNullOrWhiteSpace(ReplaceWith) ? new[] { ReplaceWith } : Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public bool IsEmpty => string.IsNullOrWhiteSpace(File)
        && string.IsNullOrWhiteSpace(Folder)
        && (Files == null || Files.Count == 0)
        && (Folders == null || Folders.Count == 0)
        && string.IsNullOrWhiteSpace(ReplaceWith)
        && (ReplacesWith == null || ReplacesWith.Count == 0);
}

public class ModManifest
{
    public string Type { get; set; } = string.Empty;

    public ModRequirementEntry? Require { get; set; }

    public List<ModRequirementEntry> Requires { get; set; } = new();

    public ModConflictCleanupEntry? ConflictCleanup { get; set; }

    public string NormalizedType
    {
        get
        {
            var raw = Type ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "putinmodloader";
            }

            var normalized = raw.Trim();
            normalized = normalized.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            normalized = normalized.ToLowerInvariant();

            return normalized switch
            {
                "modloader" or "putinmodloader" or "pim" => "putinmodloader",
                "replacing" or "rip" => "replacing",
                "putincleo" or "pic" => "putincleo",
                "putingamefolder" or "pgf" => "putingamefolder",
                "putandreplace" or "par" => "putandreplace",
                "putandreplaces" or "prs" => "putandreplaces",
                "vehicleandskinandweapon" or "vsw" => "vehicleandskinandweapon",
                "vehiclesandskinsandweapons" or "vss" => "vehiclesandskinsandweapons",
                "savesandmissions" or "saw" => "savesandmissions",
                "missiondsl" or "dsl" => "missiondsl",
                _ => normalized
            };
        }
    }

    public bool HasRequirements => GetRequirements().Any();

    public bool HasConflictCleanup => ConflictCleanup != null && !ConflictCleanup.IsEmpty;

    public IEnumerable<ModRequirementEntry> GetRequirements()
    {
        if (Require != null && !Require.IsEmpty)
        {
            yield return Require;
        }

        foreach (var requirement in Requires.Where(item => item != null && !item.IsEmpty))
        {
            yield return requirement;
        }
    }

    public bool IsReplacing => NormalizedType == "replacing";

    public bool IsModLoader => NormalizedType == "putinmodloader";

    public bool IsSingleAssetPackage => NormalizedType == "vehicleandskinandweapon";

    public bool IsMultiAssetPackage => NormalizedType == "vehiclesandskinsandweapons";
}

public sealed record ModReplacementEntry(string Source, string Target);
