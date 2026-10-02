using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Parser;

public sealed class ModsynParseException : Exception
{
    public ModsynParseException(string message, ModsynSourceLocation location)
        : base($"{message} (line {location.Line}, column {location.Column}).")
    {
        Location = location;
    }

    public ModsynSourceLocation Location { get; }

    public int Line => Location.Line;

    public int Column => Location.Column;
}