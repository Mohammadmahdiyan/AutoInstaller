namespace GtaSaModManager.Modsyn.Completion;

public enum ModsynCompletionKind
{
    Property,
    Type,
    Value,
    Path
}

public sealed record ModsynCompletionItem(
    string Label,
    string InsertText,
    string Description,
    ModsynCompletionKind Kind);