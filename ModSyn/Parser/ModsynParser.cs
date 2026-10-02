using GtaSaModManager.Modsyn.Ast;
using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Parser;

public sealed class ModsynParser
{
    private readonly IReadOnlyList<ModsynToken> _tokens;
    private int _currentIndex;

    public ModsynParser(IReadOnlyList<ModsynToken> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        if (_tokens.Count == 0 || _tokens[^1].Kind != ModsynTokenKind.EndOfFile)
        {
            throw new ArgumentException("The token stream must end with an EOF token.", nameof(tokens));
        }
    }

    public static ModsynDocumentNode Parse(string source)
    {
        var tokens = new ModsynLexer(source).Tokenize();
        return new ModsynParser(tokens).ParseDocument();
    }

    public ModsynDocumentNode ParseDocument()
    {
        var modToken = Expect(ModsynTokenKind.Identifier, "'mod'");
        if (!string.Equals(modToken.Value, "mod", StringComparison.Ordinal))
        {
            throw Error("Expected 'mod' at the start of the document", modToken.Location);
        }

        var openBrace = Expect(ModsynTokenKind.LeftBrace, "'{' after 'mod'");
        var body = ParseObject(openBrace.Location);
        Expect(ModsynTokenKind.EndOfFile, "end of file after the mod document");

        return new ModsynDocumentNode(body, modToken.Location);
    }

    private ModsynObjectNode ParseObject(ModsynSourceLocation location)
    {
        var properties = new List<ModsynPropertyNode>();
        while (Current.Kind != ModsynTokenKind.RightBrace)
        {
            if (Current.Kind == ModsynTokenKind.EndOfFile)
            {
                throw Error("Expected '}' to close the object", Current.Location);
            }

            var name = Expect(ModsynTokenKind.Identifier, "a property name");
            Expect(ModsynTokenKind.Colon, "':' after the property name");
            var value = ParseValue();
            properties.Add(new ModsynPropertyNode(name.Value, value, name.Location));

            Match(ModsynTokenKind.Comma);
        }

        Advance();
        return new ModsynObjectNode(properties.AsReadOnly(), location);
    }

    private ModsynValueNode ParseValue()
    {
        var token = Current;
        switch (token.Kind)
        {
            case ModsynTokenKind.String:
                Advance();
                return new ModsynStringNode(token.Value, token.Location);
            case ModsynTokenKind.True:
                Advance();
                return new ModsynBooleanNode(true, token.Location);
            case ModsynTokenKind.False:
                Advance();
                return new ModsynBooleanNode(false, token.Location);
            case ModsynTokenKind.Null:
                Advance();
                return new ModsynNullNode(token.Location);
            case ModsynTokenKind.Identifier:
                Advance();
                return new ModsynIdentifierNode(token.Value, token.Location);
            case ModsynTokenKind.LeftBrace:
                Advance();
                return ParseObject(token.Location);
            case ModsynTokenKind.LeftBracket:
                Advance();
                return ParseArray(token.Location);
            default:
                throw Error($"Expected a value, found {Describe(token)}", token.Location);
        }
    }

    private ModsynArrayNode ParseArray(ModsynSourceLocation location)
    {
        var items = new List<ModsynValueNode>();
        while (Current.Kind != ModsynTokenKind.RightBracket)
        {
            if (Current.Kind == ModsynTokenKind.EndOfFile)
            {
                throw Error("Expected ']' to close the array", Current.Location);
            }

            items.Add(ParseValue());
            Match(ModsynTokenKind.Comma);
        }

        Advance();
        return new ModsynArrayNode(items.AsReadOnly(), location);
    }

    private ModsynToken Expect(ModsynTokenKind kind, string expected)
    {
        if (Current.Kind != kind)
        {
            throw Error($"Expected {expected}, found {Describe(Current)}", Current.Location);
        }

        var token = Current;
        Advance();
        return token;
    }

    private bool Match(ModsynTokenKind kind)
    {
        if (Current.Kind != kind)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void Advance()
    {
        if (Current.Kind != ModsynTokenKind.EndOfFile)
        {
            _currentIndex++;
        }
    }

    private ModsynToken Current => _tokens[_currentIndex];

    private static ModsynParseException Error(string message, ModsynSourceLocation location)
    {
        return new ModsynParseException(message, location);
    }

    private static string Describe(ModsynToken token)
    {
        return token.Kind switch
        {
            ModsynTokenKind.Identifier => $"identifier '{token.Value}'",
            ModsynTokenKind.String => "a string",
            ModsynTokenKind.True => "'true'",
            ModsynTokenKind.False => "'false'",
            ModsynTokenKind.Null => "'null'",
            ModsynTokenKind.LeftBrace => "'{'",
            ModsynTokenKind.RightBrace => "'}'",
            ModsynTokenKind.LeftBracket => "'['",
            ModsynTokenKind.RightBracket => "']'",
            ModsynTokenKind.Colon => "':'",
            ModsynTokenKind.Comma => "','",
            ModsynTokenKind.EndOfFile => "end of file",
            _ => "an unknown token"
        };
    }
}