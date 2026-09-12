using System.Text.Json.Serialization;

namespace GtaSaModManager.Models;

public class ModManifest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
