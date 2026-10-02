namespace GtaSaModManager.Modsyn.Lexer;

public sealed class ModsynLexException : Exception
{
    public ModsynLexException(string message, ModsynSourceLocation location)
        : base($"{message} (line {location.Line}, column {location.Column}).")
    {
        Location = location;
    }

    public ModsynSourceLocation Location { get; }

    public int Line => Location.Line;

    public int Column => Location.Column;
}