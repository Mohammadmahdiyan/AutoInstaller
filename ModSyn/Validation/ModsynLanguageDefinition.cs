using System.Collections.ObjectModel;

namespace GtaSaModManager.Modsyn.Validation;
using System.Reflection;
using System.Text.Json;

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
    private const string MetadataResourceName = "GtaSaModManager.Modsyn.Completion.modsyn-language.json";
    private static readonly LanguageMetadata Metadata = LoadMetadata();

    public static string DefaultTypeName => Metadata.DefaultTypeName;

    public static IReadOnlyList<ModsynTypeDefinition> Types { get; } = Metadata.Types
        .Select(type => new ModsynTypeDefinition(type.Name, Freeze(type.Aliases), type.Description))
        .ToList()
        .AsReadOnly();

    public static IReadOnlyDictionary<ModsynValueKind, ModsynValueKindDefinition> ValueKinds { get; } =
        Metadata.ValueKinds.ToDictionary(
            value => Enum.Parse<ModsynValueKind>(value.Kind, ignoreCase: false),
            value => new ModsynValueKindDefinition(
                Enum.Parse<ModsynValueKind>(value.Kind, ignoreCase: false),
                value.Name,
                value.Description));

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> RootProperties { get; } =
        CreateProperties(Metadata.RootProperties);

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> RequirementProperties { get; } =
        CreateProperties(Metadata.RequirementProperties);

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> ReplacementProperties { get; } =
        CreateProperties(Metadata.ReplacementProperties);

    public static IReadOnlyDictionary<string, ModsynPropertyDefinition> AddToUserFileProperties { get; } =
        CreateProperties(Metadata.AddToUserFileProperties);

    private static readonly IReadOnlyDictionary<string, ModsynTypeDefinition> TypeLookup = BuildTypeLookup();

    public static bool TryResolveType(string? value, out ModsynTypeDefinition? type)
    {
        var normalized = NormalizeTypeKey(value);
        if (normalized.Length == 0)
        {
            type = Types.FirstOrDefault(candidate => candidate.Name == DefaultTypeName);
            return type is not null;
        }

        return TypeLookup.TryGetValue(normalized, out type);
    }

    private static LanguageMetadata LoadMetadata()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MetadataResourceName)
            ?? throw new InvalidOperationException("The Modsyn language metadata resource is missing.");
        return JsonSerializer.Deserialize<LanguageMetadata>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("The Modsyn language metadata resource is invalid.");
    }

    private static IReadOnlyDictionary<string, ModsynPropertyDefinition> CreateProperties(
        IEnumerable<PropertyMetadata> metadata)
    {
        return metadata.ToDictionary(
            property => property.Name,
            property => new ModsynPropertyDefinition(
                property.Name,
                Freeze(property.AllowedValueKinds.Select(kind => Enum.Parse<ModsynValueKind>(kind, ignoreCase: false))),
                property.Description,
                Freeze(property.ArrayItemKinds.Select(kind => Enum.Parse<ModsynValueKind>(kind, ignoreCase: false))),
                Freeze(property.ApplicableTypes)),
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values)
    {
        return values.ToList().AsReadOnly();
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

        return lookup;
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

    private sealed class LanguageMetadata
    {
        public string DefaultTypeName { get; init; } = string.Empty;

        public List<TypeMetadata> Types { get; init; } = new();

        public List<ValueKindMetadata> ValueKinds { get; init; } = new();

        public List<PropertyMetadata> RootProperties { get; init; } = new();

        public List<PropertyMetadata> RequirementProperties { get; init; } = new();

        public List<PropertyMetadata> ReplacementProperties { get; init; } = new();

        public List<PropertyMetadata> AddToUserFileProperties { get; init; } = new();
    }

    private sealed class TypeMetadata
    {
        public string Name { get; init; } = string.Empty;

        public List<string> Aliases { get; init; } = new();

        public string Description { get; init; } = string.Empty;
    }

    private sealed class ValueKindMetadata
    {
        public string Kind { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
    }

    private sealed class PropertyMetadata
    {
        public string Name { get; init; } = string.Empty;

        public List<string> AllowedValueKinds { get; init; } = new();

        public List<string> ArrayItemKinds { get; init; } = new();

        public List<string> ApplicableTypes { get; init; } = new();

        public string Description { get; init; } = string.Empty;
    }
}