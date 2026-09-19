namespace GtaSaModManager.Models;

public class ModManifest
{
    public string Type { get; set; } = string.Empty;

    public string NormalizedType
    {
        get
        {
            var raw = Type ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "putinmodloader";
            }

            var normalized = raw.Trim();
            normalized = normalized.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            normalized = normalized.ToLowerInvariant();

            return normalized switch
            {
                "modloader" or "putinmodloader" or "pim" => "putinmodloader",
                "replacing" or "rip" => "replacing",
                "putincleo" or "pic" => "putincleo",
                "putingamefolder" or "pgf" => "putingamefolder",
                "putandreplace" or "par" => "putandreplace",
                "putandreplaces" or "prs" => "putandreplaces",
                "vehicleandskinandweapon" or "vsw" => "vehicleandskinandweapon",
                "vehiclesandskinsandweapons" or "vss" => "vehiclesandskinsandweapons",
                "savesandmissions" or "saw" => "savesandmissions",
                "missiondsl" or "dsl" => "missiondsl",
                _ => normalized
            };
        }
    }

    public bool IsReplacing => NormalizedType == "replacing";

    public bool IsModLoader => NormalizedType == "putinmodloader";

    public bool IsSingleAssetPackage => NormalizedType == "vehicleandskinandweapon";

    public bool IsMultiAssetPackage => NormalizedType == "vehiclesandskinsandweapons";
}

public sealed record ModReplacementEntry(string Source, string Target);
