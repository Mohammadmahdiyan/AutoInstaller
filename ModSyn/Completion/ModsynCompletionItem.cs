namespace GtaSaModManager.Modsyn.Completion;

public enum ModsynCompletionKind
{
    Property,
    Type
}

public sealed record ModsynCompletionItem(
    string Label,
    string InsertText,
    string Description,
    ModsynCompletionKind Kind);