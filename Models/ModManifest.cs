namespace GtaSaModManager.Models;

public class ModManifest
{
    public string Type { get; set; } = string.Empty;

    public bool IsReplacing => string.Equals(Type, "Replacing", StringComparison.OrdinalIgnoreCase);

    public bool IsModLoader => string.IsNullOrWhiteSpace(Type) || string.Equals(Type, "ModLoader", StringComparison.OrdinalIgnoreCase);
}
