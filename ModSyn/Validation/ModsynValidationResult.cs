using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Validation;

public sealed record ModsynValidationError(string Message, ModsynSourceLocation Location)
{
    public int Line => Location.Line;

    public int Column => Location.Column;

    public override string ToString()
    {
        return $"{Message} (line {Line}, column {Column}).";
    }
}

public sealed record ModsynValidationWarning(string Message, ModsynSourceLocation Location)
{
    public int Line => Location.Line;

    public int Column => Location.Column;

    public override string ToString()
    {
        return $"{Message} (line {Line}, column {Column}).";
    }
}

public sealed record ModsynResolvedRequirement(
    string? CheckThis,
    IReadOnlyList<string> CheckThese,
    string? RequestAddress);

public sealed class ModsynValidationResult
{
    internal ModsynValidationResult(
        string normalizedType,
        bool backupEnabled,
        IReadOnlyList<ModsynResolvedRequirement> requirements,
        IReadOnlyList<ModsynValidationError> errors,
        IReadOnlyList<ModsynValidationWarning> warnings)
    {
        NormalizedType = normalizedType;
        BackupEnabled = backupEnabled;
        Requirements = requirements;
        Errors = errors;
        Warnings = warnings;
    }

    public string NormalizedType { get; }

    public bool BackupEnabled { get; }

    public IReadOnlyList<ModsynResolvedRequirement> Requirements { get; }

    public IReadOnlyList<ModsynValidationError> Errors { get; }

    public IReadOnlyList<ModsynValidationWarning> Warnings { get; }

    public bool IsValid => Errors.Count == 0;
}