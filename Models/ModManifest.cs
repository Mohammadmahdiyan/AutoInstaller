namespace GtaSaModManager.Models;

public class ModManifest
{
    public string Type { get; set; } = string.Empty;

    public string NormalizedType => Type.Trim().ToLowerInvariant() switch
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
        _ => string.IsNullOrWhiteSpace(Type) ? "putinmodloader" : Type.Trim().ToLowerInvariant()
    };

    public bool IsReplacing => NormalizedType == "replacing";

    public bool IsModLoader => NormalizedType == "putinmodloader";
}

public sealed record ModReplacementEntry(string Source, string Target);
