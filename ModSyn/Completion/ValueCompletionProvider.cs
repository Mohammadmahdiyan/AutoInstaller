using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Modsyn.Completion;

public sealed class ValueCompletionProvider
{
    private readonly PathCompletionProvider _pathCompletionProvider;

    public ValueCompletionProvider(PathCompletionProvider? pathCompletionProvider = null)
    {
        _pathCompletionProvider = pathCompletionProvider ?? new PathCompletionProvider();
    }

    public IReadOnlyList<ModsynCompletionItem> GetTypeCompletions(string prefix = "")
    {
        return ModsynLanguageDefinition.Types
            .SelectMany(type => new[]
            {
                new ModsynCompletionItem(type.Name, type.Name, type.Description, ModsynCompletionKind.Type)
            }.Concat(type.Aliases.Select(alias => new ModsynCompletionItem(
                alias,
                alias,
                type.Description,
                ModsynCompletionKind.Type))))
            .Where(item => item.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<ModsynCompletionItem> GetCompletions(
        string propertyName,
        string prefix = "",
        IEnumerable<string>? availablePaths = null)
    {
        if (string.Equals(propertyName, "type", StringComparison.Ordinal))
        {
            return GetTypeCompletions(prefix);
        }

        if (string.Equals(propertyName, "backup", StringComparison.Ordinal))
        {
            return new[]
                {
                    Literal("all", "Back up all files."),
                    Literal("none", "Do not back up files."),
                    Literal("some", "Back up only selected paths.")
                }
                .Where(item => item.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        var definition = FindPropertyDefinition(propertyName);
        if (definition is null)
        {
            return Array.Empty<ModsynCompletionItem>();
        }

        if (definition.AllowedValueKinds.Contains(ModsynValueKind.String))
        {
            return _pathCompletionProvider.GetCompletions(prefix, availablePaths);
        }

        var completions = new List<ModsynCompletionItem>();
        if (definition.AllowedValueKinds.Contains(ModsynValueKind.Boolean))
        {
            completions.Add(Literal("true", "Enable this option."));
            completions.Add(Literal("false", "Disable this option."));
        }

        if (definition.AllowedValueKinds.Contains(ModsynValueKind.Null))
        {
            completions.Add(Literal("null", "Use the default value."));
        }

        if (definition.AllowedValueKinds.Contains(ModsynValueKind.Array))
        {
            completions.Add(new ModsynCompletionItem("[", "[", "Begin an array value.", ModsynCompletionKind.Value));
        }

        if (definition.AllowedValueKinds.Contains(ModsynValueKind.Object))
        {
            completions.Add(new ModsynCompletionItem("{", "{", "Begin an object value.", ModsynCompletionKind.Value));
        }

        return completions
            .Where(item => item.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    private static ModsynPropertyDefinition? FindPropertyDefinition(string propertyName)
    {
        return ModsynLanguageDefinition.RootProperties.TryGetValue(propertyName, out var rootProperty)
            ? rootProperty
            : ModsynLanguageDefinition.RequirementProperties.TryGetValue(propertyName, out var requirementProperty)
                ? requirementProperty
                : ModsynLanguageDefinition.ReplacementProperties.TryGetValue(propertyName, out var replacementProperty)
                    ? replacementProperty
                    : ModsynLanguageDefinition.AddToUserFileProperties.TryGetValue(propertyName, out var userFileProperty)
                        ? userFileProperty
                        : ModsynLanguageDefinition.MixedPartProperties.GetValueOrDefault(propertyName);
    }

    private static ModsynCompletionItem Literal(string value, string description)
    {
        return new ModsynCompletionItem(value, value, description, ModsynCompletionKind.Value);
    }
}