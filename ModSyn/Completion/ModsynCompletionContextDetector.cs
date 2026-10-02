using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Completion;

public static class ModsynCompletionContextDetector
{
    private enum ContainerKind
    {
        Object,
        Array
    }

    private enum ObjectScope
    {
        Root,
        Requirement,
        Other
    }

    private enum ObjectState
    {
        ExpectingProperty,
        ExpectingColon,
        ExpectingValue
    }

    private sealed class ContainerFrame
    {
        public required ContainerKind Kind { get; init; }

        public required ObjectScope Scope { get; init; }

        public ObjectState State { get; set; } = ObjectState.ExpectingProperty;

        public string? PropertyName { get; set; }

        public bool ContainsRequirementObjects { get; init; }
    }

    public static ModsynCompletionContext Detect(string source, int cursorOffset)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (cursorOffset < 0 || cursorOffset > source.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorOffset));
        }

        var prefix = source[..cursorOffset];
        IReadOnlyList<ModsynToken> tokens;
        try
        {
            tokens = new ModsynLexer(prefix).Tokenize();
        }
        catch (ModsynLexException)
        {
            return DetectOpenTypeString(prefix);
        }

        var containers = new List<ContainerFrame>();
        var hasModStart = false;
        var lastCompletedTypeValue = false;

        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index];
            var previousToken = index > 0 ? tokens[index - 1] : default;
            lastCompletedTypeValue = false;

            if (containers.Count == 0)
            {
                if (token.Kind == ModsynTokenKind.Identifier && token.Value == "mod")
                {
                    hasModStart = true;
                }
                else if (token.Kind == ModsynTokenKind.LeftBrace && hasModStart
                    && previousToken.Kind == ModsynTokenKind.Identifier && previousToken.Value == "mod")
                {
                    containers.Add(new ContainerFrame { Kind = ContainerKind.Object, Scope = ObjectScope.Root });
                }

                continue;
            }

            var current = containers[^1];
            if (token.Kind == ModsynTokenKind.LeftBrace)
            {
                var scope = current.Kind == ContainerKind.Array && current.ContainsRequirementObjects
                    || current.Kind == ContainerKind.Object
                        && current.State == ObjectState.ExpectingValue
                        && current.PropertyName == "require"
                    ? ObjectScope.Requirement
                    : ObjectScope.Other;
                containers.Add(new ContainerFrame { Kind = ContainerKind.Object, Scope = scope });
                CompleteParentValue(containers, current);
                continue;
            }

            if (token.Kind == ModsynTokenKind.LeftBracket)
            {
                var containsRequirementObjects = current.Kind == ContainerKind.Object
                    && current.State == ObjectState.ExpectingValue
                    && current.Scope == ObjectScope.Root
                    && current.PropertyName == "requires";
                containers.Add(new ContainerFrame
                {
                    Kind = ContainerKind.Array,
                    Scope = ObjectScope.Other,
                    ContainsRequirementObjects = containsRequirementObjects
                });
                CompleteParentValue(containers, current);
                continue;
            }

            if (token.Kind is ModsynTokenKind.RightBrace or ModsynTokenKind.RightBracket)
            {
                if (containers.Count > 1)
                {
                    containers.RemoveAt(containers.Count - 1);
                    CompleteParentValue(containers, containers[^1]);
                }
                else if (token.Kind == ModsynTokenKind.RightBrace)
                {
                    containers.Clear();
                }

                continue;
            }

            current = containers[^1];
            if (current.Kind == ContainerKind.Object)
            {
                ProcessObjectToken(current, token, out lastCompletedTypeValue);
            }
            else if (token.Kind is not ModsynTokenKind.Comma)
            {
                lastCompletedTypeValue = false;
            }
        }

        if (containers.Count == 0)
        {
            return ModsynCompletionContext.None;
        }

        var top = containers[^1];
        if (top.Kind == ContainerKind.Object)
        {
            if (top.Scope == ObjectScope.Root && top.State == ObjectState.ExpectingValue && top.PropertyName == "type"
                || top.Scope == ObjectScope.Root && lastCompletedTypeValue)
            {
                return ModsynCompletionContext.TypeValues;
            }

            return top.Scope switch
            {
                ObjectScope.Root => ModsynCompletionContext.RootProperties,
                ObjectScope.Requirement => ModsynCompletionContext.RequirementProperties,
                _ => ModsynCompletionContext.None
            };
        }

        return ModsynCompletionContext.None;
    }

    private static void ProcessObjectToken(
        ContainerFrame frame,
        ModsynToken token,
        out bool completedTypeValue)
    {
        completedTypeValue = false;
        if (token.Kind == ModsynTokenKind.Comma)
        {
            frame.State = ObjectState.ExpectingProperty;
            frame.PropertyName = null;
            return;
        }

        if (frame.State == ObjectState.ExpectingProperty && token.Kind == ModsynTokenKind.Identifier)
        {
            frame.PropertyName = token.Value;
            frame.State = ObjectState.ExpectingColon;
            return;
        }

        if (frame.State == ObjectState.ExpectingColon && token.Kind == ModsynTokenKind.Colon)
        {
            frame.State = ObjectState.ExpectingValue;
            return;
        }

        if (frame.State == ObjectState.ExpectingValue)
        {
            completedTypeValue = frame.Scope == ObjectScope.Root && frame.PropertyName == "type"
                && token.Kind is ModsynTokenKind.Identifier or ModsynTokenKind.String;
            frame.State = ObjectState.ExpectingProperty;
            frame.PropertyName = null;
        }
    }

    private static void CompleteParentValue(List<ContainerFrame> containers, ContainerFrame parent)
    {
        if (containers.Count < 2 || parent.Kind != ContainerKind.Object)
        {
            return;
        }

        parent.State = ObjectState.ExpectingProperty;
        parent.PropertyName = null;
    }

    private static ModsynCompletionContext DetectOpenTypeString(string prefix)
    {
        var quoteIndex = prefix.LastIndexOf('"');
        if (quoteIndex < 0)
        {
            return ModsynCompletionContext.None;
        }

        var precedingContext = Detect(prefix[..quoteIndex], quoteIndex);
        return precedingContext == ModsynCompletionContext.TypeValues
            ? ModsynCompletionContext.TypeValues
            : ModsynCompletionContext.None;
    }
}