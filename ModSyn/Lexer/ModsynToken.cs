namespace GtaSaModManager.Modsyn.Lexer;

public enum ModsynTokenKind
{
    Identifier,
    String,
    True,
    False,
    Null,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Colon,
    Comma,
    EndOfFile
}

public readonly record struct ModsynSourceLocation(int Line, int Column);

public readonly record struct ModsynToken(
    ModsynTokenKind Kind,
    string Value,
    ModsynSourceLocation Location);