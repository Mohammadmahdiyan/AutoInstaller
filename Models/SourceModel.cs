namespace GtaSaModManager.Models;

public enum SourceModelStatus
{
    Pending,
    Mapped,
    KeepOriginal,
    Skipped
}

public sealed class SourceModel
{
    public string BaseName { get; set; } = string.Empty;
    public string? DffPath { get; set; }
    public string? TxdPath { get; set; }
    public string DetectedAssetType { get; set; } = "Unknown";
    public SourceModelStatus Status { get; set; } = SourceModelStatus.Pending;
    public GameAsset? TargetAsset { get; set; }
}