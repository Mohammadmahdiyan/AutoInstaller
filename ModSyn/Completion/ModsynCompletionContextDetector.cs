using GtaSaModManager.Modsyn.Lexer;
using GtaSaModManager.Modsyn.Validation;

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
        AddToUserFile,
        MixedPart,
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

        public HashSet<string> PropertyNames { get; } = new(StringComparer.Ordinal);

        public bool ContainsRequirementObjects { get; init; }

        public bool ContainsUserFileObjects { get; init; }

        public bool ContainsMixedPartObjects { get; init; }

        public string? ArrayPropertyName { get; init; }

        public IReadOnlyList<ModsynValueKind> ArrayItemKinds { get; init; } = Array.Empty<ModsynValueKind>();
    }

    public static ModsynCompletionContext Detect(string source, int cursorOffset)
    {
        return Detect(source, cursorOffset, out _);
    }

    public static ModsynCompletionContext Detect(string source, int cursorOffset, out string prefix)
    {
        return Detect(source, cursorOffset, out prefix, out _);
    }

    public static ModsynCompletionContext Detect(
        string source,
        int cursorOffset,
        out string prefix,
        out string? propertyName)
    {
        return Detect(source, cursorOffset, out prefix, out propertyName, out _);
    }

    public static ModsynCompletionContext Detect(
        string source,
        int cursorOffset,
        out string prefix,
        out string? propertyName,
        out IReadOnlySet<string> existingPropertyNames)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (cursorOffset < 0 || cursorOffset > source.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorOffset));
        }

        var sourcePrefix = source[..cursorOffset];
        var openQuoteIndex = FindUnclosedQuote(sourcePrefix);
        if (openQuoteIndex >= 0)
        {
            prefix = sourcePrefix[(openQuoteIndex + 1)..];
            return DetectCore(sourcePrefix[..openQuoteIndex], out propertyName, out existingPropertyNames);
        }

        prefix = GetTrailingIdentifierPrefix(sourcePrefix);
        return DetectCore(sourcePrefix[..(sourcePrefix.Length - prefix.Length)], out propertyName, out existingPropertyNames);
    }

    private static ModsynCompletionContext DetectCore(
        string prefix,
        out string? propertyName,
        out IReadOnlySet<string> existingPropertyNames)
    {
        propertyName = null;
        existingPropertyNames = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<ModsynToken> tokens;
        try
        {
            tokens = new ModsynLexer(prefix).Tokenize();
        }
        catch (ModsynLexException)
        {
            return ModsynCompletionContext.None;
        }

        var containers = new List<ContainerFrame>();
        var hasModStart = false;

        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index];
            var previousToken = index > 0 ? tokens[index - 1] : default;

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
                    ? ObjectScope.Requirement
                    : current.Kind == ContainerKind.Array && current.ContainsUserFileObjects
                        ? ObjectScope.AddToUserFile
                        : current.Kind == ContainerKind.Array && current.ContainsMixedPartObjects
                            ? ObjectScope.MixedPart
                        : current.Kind == ContainerKind.Object
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
                var propertyDefinition = current.Kind == ContainerKind.Object && current.PropertyName is not null
                    ? GetPropertyDefinition(current.Scope, current.PropertyName)
                    : null;
                var containsRequirementObjects = current.Kind == ContainerKind.Object
                    && current.State == ObjectState.ExpectingValue
                    && current.Scope == ObjectScope.Root
                    && current.PropertyName == "requires";
                var containsUserFileObjects = current.Kind == ContainerKind.Object
                    && current.State == ObjectState.ExpectingValue
                    && current.Scope == ObjectScope.Root
                    && current.PropertyName == "addToUserFile";
                var containsMixedPartObjects = current.Kind == ContainerKind.Object
                    && current.State == ObjectState.ExpectingValue
                    && current.Scope == ObjectScope.Root
                    && current.PropertyName == "list";
                containers.Add(new ContainerFrame
                {
                    Kind = ContainerKind.Array,
                    Scope = ObjectScope.Other,
                    ContainsRequirementObjects = containsRequirementObjects,
                    ContainsUserFileObjects = containsUserFileObjects,
                    ContainsMixedPartObjects = containsMixedPartObjects,
                    ArrayPropertyName = current.PropertyName,
                    ArrayItemKinds = propertyDefinition?.ArrayItemKinds ?? Array.Empty<ModsynValueKind>()
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
                ProcessObjectToken(current, token);
            }
        }

        if (containers.Count == 0)
        {
            return ModsynCompletionContext.None;
        }

        var top = containers[^1];
        if (top.Kind == ContainerKind.Object)
        {
            existingPropertyNames = top.PropertyNames;
            if (top.State == ObjectState.ExpectingValue && top.PropertyName is not null)
            {
                if (top.Scope is ObjectScope.Root or ObjectScope.MixedPart && top.PropertyName == "type")
                {
                    propertyName = top.PropertyName;
                    return ModsynCompletionContext.TypeValues;
                }

                propertyName = top.PropertyName;
                if (IsStringValueProperty(top.Scope, top.PropertyName))
                {
                    return ModsynCompletionContext.PathValues;
                }

                return ModsynCompletionContext.PropertyValues;
            }

            return top.Scope switch
            {
                ObjectScope.Root => ModsynCompletionContext.RootProperties,
                ObjectScope.Requirement => ModsynCompletionContext.RequirementProperties,
                ObjectScope.AddToUserFile => ModsynCompletionContext.AddToUserFileProperties,
                ObjectScope.MixedPart => ModsynCompletionContext.MixedPartProperties,
                _ => ModsynCompletionContext.None
            };
        }

        if (top.ArrayItemKinds.Contains(ModsynValueKind.String))
        {
            propertyName = top.ArrayPropertyName;
            return ModsynCompletionContext.PathValues;
        }

        return ModsynCompletionContext.None;
    }

    private static void ProcessObjectToken(ContainerFrame frame, ModsynToken token)
    {
        if (token.Kind == ModsynTokenKind.Comma)
        {
            frame.State = ObjectState.ExpectingProperty;
            frame.PropertyName = null;
            return;
        }

        if (frame.State == ObjectState.ExpectingProperty && token.Kind == ModsynTokenKind.Identifier)
        {
            frame.PropertyName = token.Value;
            frame.PropertyNames.Add(token.Value);
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

    private static bool IsStringValueProperty(ObjectScope scope, string propertyName)
    {
        return GetPropertyDefinition(scope, propertyName) is { } definition
            && definition.AllowedValueKinds.Contains(ModsynValueKind.String);
    }

    private static ModsynPropertyDefinition? GetPropertyDefinition(ObjectScope scope, string propertyName)
    {
        var properties = scope switch
        {
            ObjectScope.Root => ModsynLanguageDefinition.RootProperties,
            ObjectScope.Requirement => ModsynLanguageDefinition.RequirementProperties,
            ObjectScope.AddToUserFile => ModsynLanguageDefinition.AddToUserFileProperties,
            ObjectScope.MixedPart => ModsynLanguageDefinition.MixedPartProperties,
            ObjectScope.Other => ModsynLanguageDefinition.ReplacementProperties,
            _ => null
        };

        return properties is not null && properties.TryGetValue(propertyName, out var definition)
            ? definition
            : null;
    }

    private static int FindUnclosedQuote(string source)
    {
        var quoteIndex = -1;
        for (var index = 0; index < source.Length; index++)
        {
            if (source[index] == '"')
            {
                quoteIndex = quoteIndex < 0 ? index : -1;
            }
        }

        return quoteIndex;
    }

    private static string GetTrailingIdentifierPrefix(string source)
    {
        var start = source.Length;
        while (start > 0 && IsIdentifierCharacter(source[start - 1]))
        {
            start--;
        }

        return source[start..];
    }

    private static bool IsIdentifierCharacter(char character)
    {
        return character == '_' || char.IsLetterOrDigit(character);
    }
}