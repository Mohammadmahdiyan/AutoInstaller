namespace GtaSaModManager.Modsyn.Lexer;

public sealed class ModsynLexer
{
    private readonly string _source;
    private int _index;
    private int _line = 1;
    private int _column = 1;

    public ModsynLexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public IReadOnlyList<ModsynToken> Tokenize()
    {
        var tokens = new List<ModsynToken>();
        while (true)
        {
            SkipWhitespace();
            if (AtEnd)
            {
                tokens.Add(new ModsynToken(
                    ModsynTokenKind.EndOfFile,
                    string.Empty,
                    CurrentLocation));
                return tokens;
            }

            var location = CurrentLocation;
            var current = Current;
            if (IsIdentifierStart(current))
            {
                tokens.Add(ReadIdentifier(location));
                continue;
            }

            if (current == '"')
            {
                tokens.Add(ReadString(location));
                continue;
            }

            var punctuationKind = current switch
            {
                '{' => ModsynTokenKind.LeftBrace,
                '}' => ModsynTokenKind.RightBrace,
                '[' => ModsynTokenKind.LeftBracket,
                ']' => ModsynTokenKind.RightBracket,
                ':' => ModsynTokenKind.Colon,
                ',' => ModsynTokenKind.Comma,
                _ => (ModsynTokenKind?)null
            };

            if (punctuationKind.HasValue)
            {
                Advance();
                tokens.Add(new ModsynToken(punctuationKind.Value, current.ToString(), location));
                continue;
            }

            throw new ModsynLexException($"Unexpected character '{current}'", location);
        }
    }

    private bool AtEnd => _index >= _source.Length;

    private char Current => _source[_index];

    private ModsynSourceLocation CurrentLocation => new(_line, _column);

    private ModsynToken ReadIdentifier(ModsynSourceLocation location)
    {
        var start = _index;
        Advance();
        while (!AtEnd && IsIdentifierPart(Current))
        {
            Advance();
        }

        var value = _source[start.._index];
        var kind = value switch
        {
            "true" => ModsynTokenKind.True,
            "false" => ModsynTokenKind.False,
            "null" => ModsynTokenKind.Null,
            _ => ModsynTokenKind.Identifier
        };

        return new ModsynToken(kind, value, location);
    }

    private ModsynToken ReadString(ModsynSourceLocation location)
    {
        Advance();
        var start = _index;
        while (!AtEnd)
        {
            if (Current == '"')
            {
                var value = _source[start.._index];
                Advance();
                return new ModsynToken(ModsynTokenKind.String, value, location);
            }

            if (Current is '\r' or '\n')
            {
                throw new ModsynLexException("A string cannot contain a line break", CurrentLocation);
            }

            Advance();
        }

        throw new ModsynLexException("Unterminated string literal", CurrentLocation);
    }

    private void SkipWhitespace()
    {
        while (!AtEnd && Current is ' ' or '\t' or '\r' or '\n')
        {
            Advance();
        }
    }

    private void Advance()
    {
        var current = Current;
        _index++;

        if (current == '\r')
        {
            if (!AtEnd && Current == '\n')
            {
                _index++;
            }

            _line++;
            _column = 1;
        }
        else if (current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
    }

    private static bool IsIdentifierStart(char character)
    {
        return character == '_' || char.IsLetter(character);
    }

    private static bool IsIdentifierPart(char character)
    {
        return character == '_' || char.IsLetterOrDigit(character);
    }
}