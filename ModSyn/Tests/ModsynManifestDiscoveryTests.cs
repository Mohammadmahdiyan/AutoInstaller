using GtaSaModManager.Services;
using GtaSaModManager.Modsyn.Parser;
using GtaSaModManager.Modsyn.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynManifestDiscoveryTests
{
    [TestMethod]
    public void GetManifestPath_FindsModModsyn()
    {
        WithPackage("package", packageRoot =>
        {
            var manifest = Write(packageRoot, "mod.modsyn", "mod { type: Replacing }");

            Assert.AreEqual(manifest, ModPackageService.GetManifestPath(packageRoot));
        });
    }

    [TestMethod]
    public void GetManifestPath_FindsConfigModsyn()
    {
        WithPackage("package", packageRoot =>
        {
            var manifest = Write(packageRoot, "config.modsyn", "mod { type: VehicleAndSkinAndWeapon }");

            Assert.AreEqual(manifest, ModPackageService.GetManifestPath(packageRoot));
            Assert.AreEqual("VehicleAndSkinAndWeapon", ModPackageService.ResolveManifest(packageRoot).Type);
        });
    }

    [TestMethod]
    public void GetManifestPath_FindsPackageNameModsyn()
    {
        WithPackage("Named Package", packageRoot =>
        {
            var manifest = Write(packageRoot, "NAMED PACKAGE.MODsyn", "mod {}");

            Assert.AreEqual(manifest, ModPackageService.GetManifestPath(packageRoot));
        });
    }

    [TestMethod]
    public void GetManifestPath_PrefersModThenConfigThenPackageName()
    {
        WithPackage("package", packageRoot =>
        {
            var named = Write(packageRoot, "package.modsyn", "mod { type: MissionDsl }");
            var config = Write(packageRoot, "config.modsyn", "mod { type: PutInCleo }");
            var mod = Write(packageRoot, "mod.modsyn", "mod { type: Replacing }");

            Assert.AreEqual(mod, ModPackageService.GetManifestPath(packageRoot));
            Assert.AreEqual("Replacing", ModPackageService.ResolveManifest(packageRoot).Type);
            File.Delete(mod);
            Assert.AreEqual(config, ModPackageService.GetManifestPath(packageRoot));
            Assert.AreEqual("PutInCleo", ModPackageService.ResolveManifest(packageRoot).Type);
            File.Delete(config);
            Assert.AreEqual(named, ModPackageService.GetManifestPath(packageRoot));
            Assert.AreEqual("MissionDsl", ModPackageService.ResolveManifest(packageRoot).Type);
        });
    }

    [TestMethod]
    public void GetManifestPath_MatchesFilenamesCaseInsensitively()
    {
        WithPackage("package", packageRoot =>
        {
            var manifest = Write(packageRoot, "MoD.MoDsYn", "mod {}");

            Assert.AreEqual(manifest, ModPackageService.GetManifestPath(packageRoot));
        });
    }

    [TestMethod]
    public void UnrelatedJsonFilesAreNotManifests()
    {
        WithPackage("package", packageRoot =>
        {
            Write(packageRoot, "manifest.json", "{\"type\":\"Replacing\"}");
            Write(packageRoot, "other.json", "{}");
            var legacyConfig = Write(packageRoot, "config.json", "{\"type\":\"Replacing\"}");
            var legacyMod = Write(packageRoot, "mod.json", "{\"type\":\"Replacing\"}");
            var legacyPackageManifest = Write(packageRoot, "package.json", "{\"type\":\"Replacing\"}");

            Assert.IsNull(ModPackageService.GetManifestPath(packageRoot));
            Assert.IsTrue(ModPackageService.IsModPackageRoot(packageRoot));
            Assert.IsFalse(ModPackageService.IsMetadataOrNonInstallableFile(Path.Combine(packageRoot, "manifest.json"), packageRoot));
            Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(legacyConfig, packageRoot));
            Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(legacyMod, packageRoot));
            Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(legacyPackageManifest, packageRoot));
            Assert.IsTrue(ModPackageService.TryReadModsynConfiguration(packageRoot, out var configuration, out var error), error);
            Assert.AreEqual("PutInModLoader", configuration!.Manifest.Type);
            var selectedEntries = ModPackageService.ResolveInstallSelection(packageRoot, configuration.Manifest);
            CollectionAssert.AreEquivalent(
                new[] { "manifest.json", "other.json" },
                selectedEntries.Select(entry => Path.GetFileName(entry.SourcePath)).ToArray());
        });
    }

    [TestMethod]
    public void MissingManifestUsesExistingDefaultConfiguration()
    {
        WithPackage("package", packageRoot =>
        {
            Assert.IsNull(ModPackageService.GetManifestPath(packageRoot));
            Assert.IsTrue(ModPackageService.TryValidateModsyn(packageRoot, out var error), error);
            Assert.AreEqual("PutInModLoader", ModPackageService.ResolveManifest(packageRoot).Type);
        });
    }

    [TestMethod]
    public void InvalidModsynIsRejectedWithLocation()
    {
        WithPackage("package", packageRoot =>
        {
            Write(packageRoot, "mod.modsyn", "mod {\n  type: @\n}");

            Assert.IsFalse(ModPackageService.TryValidateModsyn(packageRoot, out var error));
            StringAssert.Contains(error!, "line 2, column 9");
            Assert.IsNull(ModPackageService.TryReadManifest(packageRoot));
            Assert.IsFalse(ModPackageService.IsModPackageRoot(packageRoot));
        });
    }

    [TestMethod]
    public void ModsynPackageLoading_RecognizesEverySupportedPackageType()
    {
        var packageTypes = new[]
        {
            ("PutInModLoader", "PutInModLoader"),
            ("ModLoader", "PutInModLoader"),
            ("PIM", "PutInModLoader"),
            ("Replacing", "Replacing"),
            ("RIP", "Replacing"),
            ("PutInCleo", "PutInCleo"),
            ("PIC", "PutInCleo"),
            ("PutInGameFolder", "PutInGameFolder"),
            ("PGF", "PutInGameFolder"),
            ("PutAndReplace", "PutAndReplace"),
            ("PAR", "PutAndReplace"),
            ("PutAndReplaces", "PutAndReplaces"),
            ("PRS", "PutAndReplaces"),
            ("VehicleAndSkinAndWeapon", "VehicleAndSkinAndWeapon"),
            ("VSW", "VehicleAndSkinAndWeapon"),
            ("VSS", "VehiclesAndSkinsAndWeapons"),
            ("VehicleAndSkinsAndWeapons", "VehiclesAndSkinsAndWeapons"),
            ("VehiclesAndSkinsAndWeapons", "VehiclesAndSkinsAndWeapons"),
            ("SavesAndMissions", "SavesAndMissions"),
            ("SAM", "SavesAndMissions"),
            ("MissionDsl", "MissionDsl"),
            ("DSL", "MissionDsl")
        };

        WithPackage("package", packageRoot =>
        {
            foreach (var (typeName, expectedType) in packageTypes)
            {
                var replacementText = expectedType == "PutAndReplace" ? " replacements: [\"payload.dat\"]" : string.Empty;
                Write(packageRoot, "mod.modsyn", $"mod {{ type: {typeName}{replacementText} }}");

                Assert.IsTrue(
                    ModPackageService.TryReadModsynConfiguration(packageRoot, out var configuration, out var error),
                    $"{typeName}: {error}");
                Assert.AreEqual(expectedType, configuration!.Manifest.Type);
            }

            Write(packageRoot, "mod.modsyn", "mod { type: \"\" }");
            Assert.IsFalse(
                ModPackageService.TryReadModsynConfiguration(packageRoot, out _, out var emptyTypeError));
            StringAssert.Contains(emptyTypeError!, "Unsupported package type");
            Assert.IsNull(ModPackageService.TryReadManifest(packageRoot));
            File.Delete(Path.Combine(packageRoot, "mod.modsyn"));
            Assert.AreEqual("PutInModLoader", ModPackageService.ResolveManifest(packageRoot).Type);
        });
    }

    [TestMethod]
    public void VssAlias_ResolvesToMultiAssetPackage()
    {
        WithPackage("package", packageRoot =>
        {
            Write(packageRoot, "config.modsyn", "mod { type: VSS }");

            var manifest = ModPackageService.ResolveManifest(packageRoot);

            Assert.AreEqual("vehiclesandskinsandweapons", manifest.NormalizedType);
            Assert.IsFalse(manifest.IsSingleAssetPackage);
            Assert.IsTrue(manifest.IsMultiAssetPackage);
        });
    }

    [TestMethod]
    public void ModsynPackageLoading_UsesConvertedCleoSelectionsAndReplacementEntries()
    {
        WithPackage("package", packageRoot =>
        {
            Directory.CreateDirectory(Path.Combine(packageRoot, "scripts"));
            File.WriteAllText(Path.Combine(packageRoot, "root.cs"), "root");
            File.WriteAllText(Path.Combine(packageRoot, "skip.cs"), "skip");
            File.WriteAllText(Path.Combine(packageRoot, "scripts", "nested.cs"), "nested");
            Write(packageRoot, "mod.modsyn", """
                mod {
                  type: PutInCleo
                  installThis: "root.cs"
                  installThese: ["scripts"]
                  ignoreThis: "skip.cs"
                }
                """);

            var manifest = ModPackageService.ResolveManifest(packageRoot);
            Assert.AreEqual("putincleo", manifest.NormalizedType);
            var selection = ModPackageService.ResolveInstallSelection(packageRoot, manifest);
            CollectionAssert.AreEquivalent(
                new[] { "root.cs", "scripts\\nested.cs" },
                selection.Select(entry => entry.RelativeDestination).ToArray());

            Write(packageRoot, "mod.modsyn", """
                mod {
                  type: PutAndReplace
                  replacements: [
                    "data/handling.cfg"
                    { source: "custom/weapon.dat" target: "data/weapon.dat" }
                  ]
                }
                """);
            var replacements = ModPackageService.ReadReplacementEntries(packageRoot);
            Assert.AreEqual(2, replacements.Count);
            Assert.AreEqual("data\\handling.cfg", replacements[0].Target);
            Assert.AreEqual("custom\\weapon.dat", replacements[1].Source);
            Assert.AreEqual("data\\weapon.dat", replacements[1].Target);
        });
    }

    [TestMethod]
    public void ConfigTestCollection_ParsesAndValidatesEveryExample()
    {
        var projectDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (projectDirectory is not null
            && !File.Exists(Path.Combine(projectDirectory.FullName, "Modsyn.Lexer.Tests.csproj")))
        {
            projectDirectory = projectDirectory.Parent;
        }

        Assert.IsNotNull(projectDirectory, "Could not locate the Modsyn test project directory.");
        var configTestsDirectory = Path.GetFullPath(Path.Combine(projectDirectory!.FullName, "..", "config-tests"));
        var exampleFiles = Directory.GetFiles(configTestsDirectory, "*.modsyn")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.AreEqual(8, exampleFiles.Count);
        foreach (var exampleFile in exampleFiles)
        {
            var document = ModsynParser.Parse(File.ReadAllText(exampleFile));
            var validation = ModsynValidator.Validate(document);
            Assert.IsTrue(
                validation.IsValid,
                $"{Path.GetFileName(exampleFile)}: {string.Join(Environment.NewLine, validation.Errors)}");
        }
    }

    private static void WithPackage(string name, Action<string> action)
    {
        var parent = Path.Combine(Path.GetTempPath(), "ModsynDiscovery-" + Guid.NewGuid().ToString("N"));
        var packageRoot = Path.Combine(parent, name);
        Directory.CreateDirectory(packageRoot);
        try
        {
            action(packageRoot);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    private static string Write(string packageRoot, string fileName, string contents)
    {
        var path = Path.Combine(packageRoot, fileName);
        File.WriteAllText(path, contents);
        return path;
    }
}