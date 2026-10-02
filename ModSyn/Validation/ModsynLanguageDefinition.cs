using System.Collections.ObjectModel;

namespace GtaSaModManager.Modsyn.Validation;

public enum ModsynValueKind
{
    String,
    Boolean,
    Null,
    Identifier,
    Array,
    Object
}

public sealed record ModsynValueKindDefinition(
    ModsynValueKind Kind,
    string Name,
    string Description);

public sealed record ModsynPropertyDefinition(
    string Name,
    IReadOnlyList<ModsynValueKind> AllowedValueKinds,
    string Description,
    IReadOnlyList<ModsynValueKind> ArrayItemKinds,
    IReadOnlyList<string> ApplicableTypes);

public sealed record ModsynTypeDefinition(
    string Name,
    IReadOnlyList<string> Aliases,
    string Description);

public static class ModsynLanguageDefinition
{
    private static readonly ModsynTypeDefinition PutInModLoaderType = Type("PutInModLoader", "Install the package through ModLoader.", "ModLoader", "PIM");
    private static readonly ModsynTypeDefinition ReplacingType = Type("Replacing", "Replace matching game files.", "RIP");
    private static readonly ModsynTypeDefinition PutInCleoType = Type("PutInCleo", "Install selected files in the CLEO folder.", "PIC");
    private static readonly ModsynTypeDefinition PutInGameFolderType = Type("PutInGameFolder", "Install package files in the game folder.", "PGF");
    private static readonly ModsynTypeDefinition PutAndReplaceType = Type("PutAndReplace", "Install files at explicit replacement destinations.", "PAR");
    private static readonly ModsynTypeDefinition PutAndReplacesType = Type("PutAndReplaces", "Install package files and replace matching game files.", "PRS");
    private static readonly ModsynTypeDefinition VehicleAndSkinAndWeaponType = Type("VehicleAndSkinAndWeapon", "Install one selected vehicle, skin, or weapon asset.", "VSW", "VSS", "VehicleAndSkinsAndWeapons");
    private static readonly ModsynTypeDefinition VehiclesAndSkinsAndWeaponsType = Type("VehiclesAndSkinsAndWeapons", "Install multiple selected vehicle, skin, or weapon assets.");
    private static readonly ModsynTypeDefinition SavesAndMissionsType = Type("SavesAndMissions", "Install save or mission files.", "SAM");
    private static readonly ModsynTypeDefinition MissionDslType = Type("MissionDsl", "Install a Mission DSL package.", "DSL");

    private static readonly IReadOnlyList<string> BackupApplicableTypes = Array.AsReadOnly(new[]
    {
        ReplacingType.Name,
        PutAndReplaceType.Name,
        PutAndReplacesType.Name
    });

    public static string DefaultTypeName => PutInModLoaderType.Name;

    public static IReadOnlyList<ModsynTypeDefinition> Types { get; } = Array.AsReadOnly(new[]
    {
        PutInModLoaderType,
        ReplacingType,
        PutInCleoType,
        PutInGameFolderType,
        PutAndReplaceType,
        PutAndReplacesType,
        VehicleAndSkinAndWeaponType,
        VehiclesAndSkinsAndWeaponsType,
        SavesAndMissionsType,
        MissionDslType
    });

    public static IReadOnlyDictionary<ModsynValueKind, ModsynValueKindDefinition> ValueKinds { get; } =
        new ReadOnlyDictionary<ModsynValueKind, ModsynValueKindDefinition>(new Dictionary<ModsynValueKind, ModsynValueKindDefinition>
        {
            [ModsynValueKind.String] = new(ModsynValueKind.String, "string", "A quoted string value."),
            [ModsynValueKind.Boolean] = new(ModsynValueKind.Boolean, "boolean", "The true or false literal."),
            [ModsynValueKind.Null] = new(ModsynValueKind.Null, "null", "The null literal."),
            [ModsynValueKind.Identifier] = new(ModsynValueKind.Identifier, "identifier", "An unquoted identifier value."),
            [ModsynValueKind.Array] = new(ModsynValueKind.Array, "array", "A sequence of values."),
            [ModsynValueKind.Object] = new(ModsynValueKind.Object, "object", "A set of named properties.")
        });

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> RootProperties { get; } = PropertyDictionary(
        Property("type", "Package installation type.", Kinds(ModsynValueKind.String, ModsynValueKind.Identifier)),
        Property("require", "One requirement object.", Kinds(ModsynValueKind.Object)),
        Property("requires", "An array of requirement objects.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.Object)),
        Property("deleteThis", "One file or directory path to delete.", Kinds(ModsynValueKind.String)),
        Property("deleteThese", "File or directory paths to delete.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String)),
        Property("replacements", "Replacement mappings or string path shorthand.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String, ModsynValueKind.Object)),
        Property("backup", "Whether replacement backups are enabled; null uses the default.", Kinds(ModsynValueKind.Boolean, ModsynValueKind.Null), applicableTypes: BackupApplicableTypes),
        Property("backupThis", "One file or directory path to back up.", Kinds(ModsynValueKind.String), applicableTypes: BackupApplicableTypes),
        Property("backupThese", "File or directory paths to back up.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String), BackupApplicableTypes),
        Property("dontBackupThis", "One file or directory path to exclude from backups.", Kinds(ModsynValueKind.String), applicableTypes: BackupApplicableTypes),
        Property("dontBackupThese", "File or directory paths to exclude from backups.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String), BackupApplicableTypes),
        Property("installThis", "One file or directory path to install.", Kinds(ModsynValueKind.String)),
        Property("installThese", "File or directory paths to install.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String)),
        Property("ignoreThis", "One file or directory path to ignore.", Kinds(ModsynValueKind.String)),
        Property("ignoreThese", "File or directory paths to ignore.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String)));

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> RequirementProperties { get; } = PropertyDictionary(
        Property("checkThis", "One file or directory path checked by this requirement.", Kinds(ModsynValueKind.String)),
        Property("checkThese", "Paths that must all exist for this requirement condition.", Kinds(ModsynValueKind.Array), Kinds(ModsynValueKind.String)),
        Property("reqAddress", "Package address used to satisfy this requirement; takes priority over reqPath.", Kinds(ModsynValueKind.String)),
        Property("reqPath", "Fallback package address used when reqAddress is absent.", Kinds(ModsynValueKind.String)),
        Property("require", "A nested requirement whose missing values fill this requirement.", Kinds(ModsynValueKind.Object)));

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> ReplacementProperties { get; } = PropertyDictionary(
        Property("source", "A package-relative source path.", Kinds(ModsynValueKind.String)),
        Property("target", "A game-relative destination path.", Kinds(ModsynValueKind.String)));

    private static readonly IReadOnlyDictionary<string, ModsynTypeDefinition> TypeLookup = BuildTypeLookup();

    public static bool TryResolveType(string? value, out ModsynTypeDefinition? type)
    {
        var normalized = NormalizeTypeKey(value);
        if (normalized.Length == 0)
        {
            type = PutInModLoaderType;
            return true;
        }

        return TypeLookup.TryGetValue(normalized, out type);
    }

    private static ModsynTypeDefinition Type(string name, string description, params string[] aliases)
    {
        return new ModsynTypeDefinition(name, Array.AsReadOnly(aliases), description);
    }

    private static ModsynPropertyDefinition Property(
        string name,
        string description,
        IReadOnlyList<ModsynValueKind> allowedValueKinds,
        IReadOnlyList<ModsynValueKind>? arrayItemKinds = null,
        IReadOnlyList<string>? applicableTypes = null)
    {
        return new ModsynPropertyDefinition(
            name,
            allowedValueKinds,
            description,
            arrayItemKinds ?? Array.Empty<ModsynValueKind>(),
            applicableTypes ?? Array.Empty<string>());
    }

    private static IReadOnlyList<ModsynValueKind> Kinds(params ModsynValueKind[] kinds)
    {
        return Array.AsReadOnly(kinds);
    }

    private static IReadOnlyDictionary<string, ModsynPropertyDefinition> PropertyDictionary(
        params ModsynPropertyDefinition[] definitions)
    {
        return new ReadOnlyDictionary<string, ModsynPropertyDefinition>(
            definitions.ToDictionary(definition => definition.Name, StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, ModsynTypeDefinition> BuildTypeLookup()
    {
        var lookup = new Dictionary<string, ModsynTypeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in Types)
        {
            lookup.Add(NormalizeTypeKey(type.Name), type);
            foreach (var alias in type.Aliases)
            {
                lookup.Add(NormalizeTypeKey(alias), type);
            }
        }

        return new ReadOnlyDictionary<string, ModsynTypeDefinition>(lookup);
    }

    private static string NormalizeTypeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Trim()
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '_')
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}