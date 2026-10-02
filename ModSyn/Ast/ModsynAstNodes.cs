using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Ast;

public sealed record ModsynDocumentNode(
    ModsynObjectNode Body,
    ModsynSourceLocation Location);

public sealed record ModsynPropertyNode(
    string Name,
    ModsynValueNode Value,
    ModsynSourceLocation Location);

public sealed record ModsynObjectNode(
    IReadOnlyList<ModsynPropertyNode> Properties,
    ModsynSourceLocation Location) : ModsynValueNode(Location);

public abstract record ModsynValueNode(ModsynSourceLocation Location);

public sealed record ModsynStringNode(
    string Value,
    ModsynSourceLocation Location) : ModsynValueNode(Location);

public sealed record ModsynBooleanNode(
    bool Value,
    ModsynSourceLocation Location) : ModsynValueNode(Location);

public sealed record ModsynNullNode(
    ModsynSourceLocation Location) : ModsynValueNode(Location);

public sealed record ModsynIdentifierNode(
    string Name,
    ModsynSourceLocation Location) : ModsynValueNode(Location);

public sealed record ModsynArrayNode(
    IReadOnlyList<ModsynValueNode> Items,
    ModsynSourceLocation Location) : ModsynValueNode(Location);