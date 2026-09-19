using System.Globalization;
using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public sealed class AssetCatalogService
{
    private readonly string _catalogPath;
    private List<GameAsset>? _assets;
    public string ValidationError { get; private set; } = string.Empty;

    public AssetCatalogService()
    {
        _catalogPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Assets.json");
    }

    public IReadOnlyList<GameAsset> LoadAssets()
    {
        if (_assets != null)
        {
            return _assets;
        }

        if (!File.Exists(_catalogPath))
        {
            _assets = new List<GameAsset>();
            return _assets;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(_catalogPath));
            var assets = new List<GameAsset>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var assetType = property.Name.ToLowerInvariant() switch
                {
                    "vehicles" => "Vehicle",
                    "skins" => "Skin",
                    "weapons" => "Weapon",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(assetType) || property.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in property.Value.EnumerateArray())
                {
                    var nameFile = GetString(item, "nameFile");
                    if (string.IsNullOrWhiteSpace(nameFile))
                    {
                        ValidationError = "Assets.json مشکل دارد: یک فایل در دستهٔ " + assetType + " مقدار nameFile ندارد.";
                        continue;
                    }

                    assets.Add(new GameAsset
                    {
                        AssetType = assetType,
                        Id = GetValueAsString(item, "id"),
                        Name = GetString(item, "name") is { Length: > 0 } name ? name : nameFile,
                        NameFile = nameFile,
                        Category = GetString(item, "category") is { Length: > 0 } category ? category : "normal",
                        Image = GetString(item, "image")
                    });
                }
            }

            _assets = assets;
        }
        catch (JsonException)
        {
            _assets = new List<GameAsset>();
        }

        return _assets;
    }

    public IReadOnlyList<GameAsset> FindAssetsInPackage(string packageRoot)
    {
        var files = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return LoadAssets()
            .Where(asset => files.Contains(asset.NameFile))
            .GroupBy(asset => asset.AssetType + "|" + asset.NameFile, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(asset => asset.AssetType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ResolveImagePath(GameAsset asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.Image))
        {
            var relative = asset.Image.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var imagePath = Path.Combine(AppContext.BaseDirectory, relative);
            if (File.Exists(imagePath))
            {
                return imagePath;
            }
        }

        var fallbackName = string.Equals(CultureInfo.CurrentCulture.Name, "fa-IR", StringComparison.OrdinalIgnoreCase)
            ? "Image_not_available_Persion.png"
            : "Image_not_available_English.png";
        var fallbackPath = Path.Combine(AppContext.BaseDirectory, "Assets", fallbackName);
        return File.Exists(fallbackPath) ? fallbackPath : null;
    }

    private static string? GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string GetValueAsString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }
}
