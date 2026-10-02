using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Modsyn.Completion;

public static class ModsynCompletionService
{
    public static IReadOnlyList<ModsynCompletionItem> GetCompletions(
        ModsynCompletionContext context)
    {
        return context switch
        {
            ModsynCompletionContext.RootProperties => GetPropertyCompletions(ModsynLanguageDefinition.RootProperties),
            ModsynCompletionContext.RequirementProperties => GetPropertyCompletions(ModsynLanguageDefinition.RequirementProperties),
            ModsynCompletionContext.TypeValues => GetTypeCompletions(),
            _ => Array.Empty<ModsynCompletionItem>()
        };
    }

    public static IReadOnlyList<ModsynCompletionItem> GetCompletions(string source, int cursorOffset)
    {
        var context = ModsynCompletionContextDetector.Detect(source, cursorOffset);
        return GetCompletions(context);
    }

    private static IReadOnlyList<ModsynCompletionItem> GetPropertyCompletions(
        IReadOnlyDictionary<string, ModsynPropertyDefinition> definitions)
    {
        return definitions.Values
            .Select(definition => new ModsynCompletionItem(
                definition.Name,
                definition.Name,
                definition.Description,
                ModsynCompletionKind.Property))
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<ModsynCompletionItem> GetTypeCompletions()
    {
        var completions = new List<ModsynCompletionItem>();
        foreach (var type in ModsynLanguageDefinition.Types)
        {
            completions.Add(new ModsynCompletionItem(
                type.Name,
                type.Name,
                type.Description,
                ModsynCompletionKind.Type));
            completions.AddRange(type.Aliases.Select(alias => new ModsynCompletionItem(
                alias,
                alias,
                type.Description,
                ModsynCompletionKind.Type)));
        }

        return completions.AsReadOnly();
    }
}