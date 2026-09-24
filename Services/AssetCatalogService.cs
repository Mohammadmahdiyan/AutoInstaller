using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public sealed class AssetCatalogService
{
    private readonly string[] _catalogPaths;
    private List<GameAsset>? _assets;
    public string ValidationError { get; private set; } = string.Empty;

    public AssetCatalogService()
    {
        _catalogPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Vehicles.json"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Skins.json"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Weapons.json")
        };
    }

    public IReadOnlyList<GameAsset> LoadAssets()
    {
        if (_assets != null)
        {
            return _assets;
        }

        var assets = new List<GameAsset>();
        foreach (var (catalogPath, assetType) in _catalogPaths.Zip(
            new[] { "Vehicle", "Skin", "Weapon" },
            (path, type) => (path, type)))
        {
            if (!File.Exists(catalogPath))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(catalogPath));
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    ValidationError = Path.GetFileName(catalogPath) + " باید یک آرایه JSON باشد.";
                    continue;
                }

                var catalogAssetCount = 0;
                var catalogCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    var nameFile = GetString(item, "nameFile");
                    if (string.IsNullOrWhiteSpace(nameFile))
                    {
                        ValidationError = Path.GetFileName(catalogPath) + " مشکل دارد: یک فایل مقدار nameFile ندارد.";
                        continue;
                    }

                    var category = ResolveCategory(item, assetType, nameFile);
                    catalogCategories.Add(category);
                    assets.Add(new GameAsset
                    {
                        AssetType = assetType,
                        Id = GetValueAsString(item, "id"),
                        Name = GetString(item, "name") is { Length: > 0 } name ? name : nameFile,
                        NameFile = nameFile,
                        Category = category,
                        Image = GetString(item, "image")
                    });
                    catalogAssetCount++;
                }

                Debug.WriteLine($"[Assets] file={Path.GetFileName(catalogPath)}; type={assetType}; assets={catalogAssetCount}; categories={string.Join(" | ", catalogCategories.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}");
            }
            catch (JsonException ex)
            {
                ValidationError = Path.GetFileName(catalogPath) + " نامعتبر است: " + ex.Message;
            }
        }

        _assets = assets;
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

        var cultureName = CultureInfo.CurrentUICulture.Name;
        var isPersian = cultureName.StartsWith("fa", StringComparison.OrdinalIgnoreCase)
            || cultureName.Contains("Persian", StringComparison.OrdinalIgnoreCase);

        var candidateNames = isPersian
            ? new[] { "Image_not_available_Persion.png", "Image_not_available_Persian.png", "Image_not_available_English.png" }
            : new[] { "Image_not_available_English.png", "Image_not_available_Persion.png", "Image_not_available_Persian.png" };

        foreach (var fallbackName in candidateNames)
        {
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, "Assets", fallbackName);
            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }
        }

        return null;
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

    private static string ResolveCategory(JsonElement item, string assetType, string nameFile)
    {
        var rawCategory = GetString(item, "category");
        if (!string.IsNullOrWhiteSpace(rawCategory))
        {
            return rawCategory.Trim();
        }

        var inferred = InferCategoryFromName(nameFile, assetType);
        return string.IsNullOrWhiteSpace(inferred) ? "General" : inferred;
    }

    private static string InferCategoryFromName(string nameFile, string assetType)
    {
        var normalizedName = nameFile.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return string.Empty;
        }

        if (string.Equals(assetType, "Vehicle", StringComparison.OrdinalIgnoreCase))
        {
            return "General Vehicles";
        }

        if (string.Equals(assetType, "Skin", StringComparison.OrdinalIgnoreCase))
        {
            return "General Skins";
        }

        if (string.Equals(assetType, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedName.ToLowerInvariant() switch
            {
                "fist" or "brassknuckle" or "golfclub" or "nitestick" or "knifecur" or "bat" or "shovel" or "poolcue" or "katana" or "chnsaw" or "gun_dildo1" or "gun_dildo2" or "gun_vibe1" or "gun_vibe2" or "flowera" or "gun_cane" => "Melee",
                "grenade" or "teargas" or "molotov" or "satchel" or "bomb" => "Thrown",
                "colt45" or "silenced" or "desert_eagle" => "Handguns",
                "chromegun" or "sawnoff" or "shotgspa" => "Shotguns",
                "micro_uzi" or "mp5lng" or "tec9" => "Submachine Guns",
                "ak47" or "m4" or "cuntgun" or "sniper" => "Rifles",
                "rocketla" or "heatseek" or "flame" or "minigun" => "Heavy Weapons",
                "spraycan" or "fire_ex" or "camera" or "nvgoggles" or "irgoggles" or "gun_para" or "cellphone" or "jetpack" => "Equipment",
                _ => "General Weapons"
            };
        }

        return "General";
    }
}
