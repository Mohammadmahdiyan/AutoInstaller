namespace GtaSaModManager.Modsyn.Completion;

public static class ModsynCompletionService
{
    private static readonly PropertyCompletionProvider PropertyProvider = new();
    private static readonly PathCompletionProvider PathProvider = new();
    private static readonly ValueCompletionProvider ValueProvider = new(PathProvider);

    public static IReadOnlyList<ModsynCompletionItem> GetCompletions(
        ModsynCompletionContext context)
    {
        return context switch
        {
            ModsynCompletionContext.RootProperties or ModsynCompletionContext.RequirementProperties
                or ModsynCompletionContext.AddToUserFileProperties =>
                PropertyProvider.GetCompletions(context),
            ModsynCompletionContext.TypeValues => ValueProvider.GetTypeCompletions(),
            _ => Array.Empty<ModsynCompletionItem>()
        };
    }

    public static IReadOnlyList<ModsynCompletionItem> GetCompletions(
        string source,
        int cursorOffset,
        IEnumerable<string>? availablePaths = null)
    {
        var context = ModsynCompletionContextDetector.Detect(
            source,
            cursorOffset,
            out var prefix,
            out var propertyName,
            out var existingPropertyNames);

        return context switch
        {
            ModsynCompletionContext.RootProperties or ModsynCompletionContext.RequirementProperties
                or ModsynCompletionContext.AddToUserFileProperties =>
                PropertyProvider.GetCompletions(context, prefix, existingPropertyNames),
            ModsynCompletionContext.TypeValues => ValueProvider.GetTypeCompletions(prefix),
            ModsynCompletionContext.PropertyValues when propertyName is not null =>
                ValueProvider.GetCompletions(propertyName, prefix, availablePaths),
            ModsynCompletionContext.PathValues => PathProvider.GetCompletions(prefix, availablePaths),
            _ => Array.Empty<ModsynCompletionItem>()
        };
    }
}