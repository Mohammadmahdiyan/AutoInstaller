using GtaSaModManager.Modsyn.Validation;

namespace GtaSaModManager.Modsyn.Completion;

public sealed class PropertyCompletionProvider
{
    public IReadOnlyList<ModsynCompletionItem> GetCompletions(
        ModsynCompletionContext context,
        string prefix = "")
    {
        var definitions = context switch
        {
            ModsynCompletionContext.RootProperties => ModsynLanguageDefinition.RootProperties,
            ModsynCompletionContext.RequirementProperties => ModsynLanguageDefinition.RequirementProperties,
            _ => null
        };

        if (definitions is null)
        {
            return Array.Empty<ModsynCompletionItem>();
        }

        return definitions.Values
            .Where(definition => definition.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(definition => new ModsynCompletionItem(
                definition.Name,
                definition.Name,
                definition.Description,
                ModsynCompletionKind.Property))
            .ToList()
            .AsReadOnly();
    }
}