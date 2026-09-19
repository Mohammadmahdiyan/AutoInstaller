namespace GtaSaModManager.Models;

public sealed class GameAsset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameFile { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string AssetType { get; set; } = string.Empty;
}
