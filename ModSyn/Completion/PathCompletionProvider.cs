namespace GtaSaModManager.Modsyn.Completion;

public sealed class PathCompletionProvider
{
    public IReadOnlyList<ModsynCompletionItem> GetCompletions(
        string prefix,
        IEnumerable<string>? availablePaths = null)
    {
        return (availablePaths ?? Enumerable.Empty<string>())
            .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new ModsynCompletionItem(
                path,
                $"\"{path}\"",
                "Package or game file path.",
                ModsynCompletionKind.Path))
            .ToList()
            .AsReadOnly();
    }
}