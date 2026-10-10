using System.Reflection;
using System.Runtime.CompilerServices;
using GtaSaModManager.Controls.DeleteMods;
using GtaSaModManager.Forms;
using GtaSaModManager.Models;
using GtaSaModManager.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class Step1DeleteManifestTests
{
    [TestMethod]
    public void BuildInstalledModDeleteEntries_ExcludesUnrecordedModLoaderFolders()
    {
        var gameRoot = Path.Combine(Path.GetTempPath(), "Step1DeleteManifest-" + Guid.NewGuid().ToString("N"));
        var modLoaderRoot = GameService.GetModLoaderFolder(gameRoot);
        var recordedFolder = Path.Combine(modLoaderRoot, "Recorded Mod");
        var unrecordedFolder = Path.Combine(modLoaderRoot, "Unrecorded Mod");
        Directory.CreateDirectory(recordedFolder);
        Directory.CreateDirectory(unrecordedFolder);

        try
        {
            var installedFile = Path.Combine(recordedFolder, "recorded.dff");
            File.WriteAllText(installedFile, string.Empty);
            File.WriteAllText(Path.Combine(unrecordedFolder, "unrecorded.dff"), string.Empty);
            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameRoot);
            ModLoaderService.SaveInstallationManifest(manifestPath, new InstallationManifest
            {
                Entries =
                [
                    new InstallationManifestEntry
                    {
                        ModId = "Recorded Mod",
                        Type = "putinmodloader",
                        InstalledFiles = [installedFile],
                        InstalledDestination = recordedFolder
                    }
                ]
            });

            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            var builder = typeof(MainForm).GetMethod("BuildInstalledModDeleteEntries", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var entries = (List<DeleteModEntry>)builder.Invoke(form, [gameRoot])!;

            CollectionAssert.AreEqual(new[] { "Recorded Mod" }, entries.Select(entry => entry.Name).ToArray());
        }
        finally
        {
            Directory.Delete(gameRoot, recursive: true);
        }
    }

    [TestMethod]
    public void HasInstallationManifestEntries_RequiresAtLeastOneRecordedEntry()
    {
        var gameRoot = Path.Combine(Path.GetTempPath(), "Step1DeleteVisibility-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(gameRoot);
        File.WriteAllText(Path.Combine(gameRoot, "gta_sa.exe"), string.Empty);

        try
        {
            var hasEntries = typeof(MainForm).GetMethod("HasInstallationManifestEntries", BindingFlags.Static | BindingFlags.NonPublic)!;

            Assert.IsFalse((bool)hasEntries.Invoke(null, [gameRoot])!);

            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameRoot);
            ModLoaderService.SaveInstallationManifest(manifestPath, new InstallationManifest
            {
                Entries =
                [
                    new InstallationManifestEntry
                    {
                        ModId = "Recorded Mod",
                        Type = "putinmodloader"
                    }
                ]
            });

            Assert.IsTrue((bool)hasEntries.Invoke(null, [gameRoot])!);
        }
        finally
        {
            Directory.Delete(gameRoot, recursive: true);
        }
    }

    [TestMethod]
    public void RecordCurrentMixedInstallation_DoesNotCreateEmptyModLoaderContainer()
    {
        var gameRoot = Path.Combine(Path.GetTempPath(), "MixedInstallRecord-" + Guid.NewGuid().ToString("N"));
        var packageRoot = Path.Combine(Path.GetTempPath(), "MixedPackage-" + Guid.NewGuid().ToString("N"));
        var installedFile = Path.Combine(gameRoot, "modloader", "Horse Car", "sweeper.dff");
        Directory.CreateDirectory(Path.GetDirectoryName(installedFile)!);
        Directory.CreateDirectory(Path.Combine(gameRoot, "modloader", "HorseCar"));
        Directory.CreateDirectory(packageRoot);
        File.WriteAllText(installedFile, "model");

        try
        {
            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            typeof(MainForm).GetField("_selectedGamePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, gameRoot);
            typeof(MainForm).GetField("_mixedParentName", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, "HorseCar");
            typeof(MainForm).GetField("_mixedParentRoot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, packageRoot);
            var installedParts = new List<InstallationManifestPart>();
            typeof(MainForm).GetField("_mixedInstalledParts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, installedParts);
            installedParts.Add(new InstallationManifestPart
            {
                Name = "HorseFile",
                Type = "putinmodloader",
                ModId = "Horse Car",
                InstalledDestination = Path.GetDirectoryName(installedFile)!,
                InstalledFiles = [installedFile]
            });

            typeof(MainForm).GetMethod("RecordCurrentMixedInstallation", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);

            Assert.IsFalse(Directory.Exists(Path.Combine(gameRoot, "modloader", "HorseCar")));
            var installation = ModLoaderService.LoadInstallationManifest(
                    ModLoaderService.GetGameInstallationsManifestPath(gameRoot))
                .Entries.Single();
            Assert.AreEqual("mixed", installation.Type);
            Assert.AreEqual("HorseCar", installation.ModId);
            CollectionAssert.AreEqual(new[] { installedFile }, installation.InstalledFiles);
        }
        finally
        {
            Directory.Delete(gameRoot, recursive: true);
            Directory.Delete(packageRoot, recursive: true);
        }
    }

    [TestMethod]
    public void MixedVswInstallation_PersistsSelectedAssetMapping()
    {
        var gameRoot = Path.Combine(Path.GetTempPath(), "MixedVswMapping-" + Guid.NewGuid().ToString("N"));
        var packageRoot = Path.Combine(Path.GetTempPath(), "MixedVswPackage-" + Guid.NewGuid().ToString("N"));
        var installedFile = Path.Combine(gameRoot, "modloader", "Monster Truck", "Monster", "dumper.dff");
        Directory.CreateDirectory(Path.GetDirectoryName(installedFile)!);
        Directory.CreateDirectory(packageRoot);
        File.WriteAllText(installedFile, "model");

        try
        {
            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            typeof(MainForm).GetField("_selectedGamePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, gameRoot);
            typeof(MainForm).GetField("_mixedParentName", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, "Monster Truck");
            typeof(MainForm).GetField("_selectedModName", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, Path.Combine("Monster Truck", "Monster"));
            typeof(MainForm).GetField("_mixedParentRoot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, packageRoot);
            var installedParts = new List<InstallationManifestPart>();
            typeof(MainForm).GetField("_mixedInstalledParts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, installedParts);

            var mappingBuilder = typeof(MainForm).GetMethod("BuildInstallationAssetMappings", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var mappings = (List<InstallationAssetMapping>)mappingBuilder.Invoke(form,
                [new ModManifest { Type = "VSW" }, "monster", new GameAsset { NameFile = "dumper", AssetType = "Vehicle" }])!;
            var part = new ModMixedPackagePart
            {
                Type = "VehicleAndSkinAndWeapon",
                FolderName = "Monster",
                Manifest = new ModManifest { Type = "VSW" }
            };
            typeof(MainForm).GetMethod("AddMixedInstalledPart", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, [part, new List<string> { installedFile }, mappings]);
            typeof(MainForm).GetMethod("RecordCurrentMixedInstallation", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);

            var installation = ModLoaderService.LoadInstallationManifest(
                    ModLoaderService.GetGameInstallationsManifestPath(gameRoot))
                .Entries.Single();
            var recordedMapping = installation.MixedParts.Single().AssetMappings.Single();
            Assert.AreEqual("monster", recordedMapping.SourceModelName);
            Assert.AreEqual("dumper", recordedMapping.TargetModelName);
            Assert.AreEqual("Vehicle", recordedMapping.AssetType);
        }
        finally
        {
            Directory.Delete(gameRoot, recursive: true);
            Directory.Delete(packageRoot, recursive: true);
        }
    }

    [TestMethod]
    public void MixedPgfAndVswInstallFilesToTheirExpectedDestinations()
    {
        var root = Path.Combine(Path.GetTempPath(), "MixedPgfVswInstall-" + Guid.NewGuid().ToString("N"));
        var gameRoot = Path.Combine(root, "game");
        var packageRoot = Path.Combine(root, "Real Monster Truck");
        var pgfRoot = Path.Combine(packageRoot, "Script");
        var vswRoot = Path.Combine(packageRoot, "Monster");
        Directory.CreateDirectory(Path.Combine(pgfRoot, "cleo"));
        Directory.CreateDirectory(vswRoot);
        Directory.CreateDirectory(gameRoot);
        var scriptSource = Path.Combine(pgfRoot, "cleo", "real_monster.cs");
        var dffSource = Path.Combine(vswRoot, "monster.dff");
        var txdSource = Path.Combine(vswRoot, "monster.txd");
        File.WriteAllText(scriptSource, "script");
        File.WriteAllText(dffSource, "dff");
        File.WriteAllText(txdSource, "txd");
        File.WriteAllText(Path.Combine(vswRoot, "mod.modsyn"), "mod { type: VSW }");

        try
        {
            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            SetField(form, "_selectedGamePath", gameRoot);
            SetField(form, "_selectedModName", Path.Combine("Real Monster Truck", "Monster"));
            SetField(form, "_selectedModPackageRoot", vswRoot);
            SetField(form, "_assetCatalogService", new AssetCatalogService());
            SetField(form, "_localizationService", new LocalizationService());
            SetField(form, "_lastStep4FileNames", new List<string>());
            SetField(form, "_selectedAssetForInstall", new GameAsset { NameFile = "dumper", AssetType = "Vehicle" });
            SetField(form, "_step5PreparedPayloadPath", vswRoot);
            SetField(form, "_step5DetectedAssetType", "Vehicle");

            var pgfFiles = new List<string>();
            var pgfInstall = InstallTypedPackage(form,
                pgfRoot,
                "Real Monster Truck",
                pgfRoot,
                new ModManifest { Type = "PGF" },
                pgfFiles,
                new List<InstallationAssetMapping>());
            Assert.IsTrue(pgfInstall);
            Assert.AreEqual("script", File.ReadAllText(Path.Combine(gameRoot, "cleo", "real_monster.cs")));

            var vswFiles = new List<string>();
            var vswMappings = new List<InstallationAssetMapping>();
            var vswInstall = InstallTypedPackage(form,
                vswRoot,
                Path.Combine("Real Monster Truck", "Monster"),
                vswRoot,
                new ModManifest { Type = "VSW" },
                vswFiles,
                vswMappings);
            Assert.IsTrue(vswInstall);
            var installedDff = Path.Combine(gameRoot, "modloader", "Real Monster Truck", "Monster", "dumper.dff");
            var installedTxd = Path.Combine(gameRoot, "modloader", "Real Monster Truck", "Monster", "dumper.txd");
            Assert.AreEqual("dff", File.ReadAllText(installedDff));
            Assert.AreEqual("txd", File.ReadAllText(installedTxd));
            CollectionAssert.AreEquivalent(new[] { installedDff, installedTxd }, vswFiles.ToArray());
            Assert.AreEqual("monster", vswMappings.Single().SourceModelName);
            Assert.AreEqual("dumper", vswMappings.Single().TargetModelName);
            Assert.AreEqual("script", File.ReadAllText(Path.Combine(gameRoot, "cleo", "real_monster.cs")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static bool InstallTypedPackage(
        MainForm form,
        string payloadPath,
        string modName,
        string packageRoot,
        ModManifest manifest,
        List<string> installedFiles,
        List<InstallationAssetMapping> assetMappings)
    {
        var method = typeof(MainForm).GetMethod("InstallTypedPackageAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<bool>)method.Invoke(form,
            [payloadPath, modName, packageRoot, manifest, null, null, false, installedFiles, assetMappings])!;
        return task.GetAwaiter().GetResult();
    }

    private static void SetField<T>(MainForm form, string fieldName, T value)
    {
        typeof(MainForm).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(form, value);
    }

    [TestMethod]
    public void VswInstall_IncludesAndRenamesNumberedTextureVariants()
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), "VswTextureVariants-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageRoot);

        try
        {
            foreach (var fileName in new[]
                     {
                         "infernus.dff",
                         "infernus.txd",
                         "Infernus1.txd",
                         "Infernus2.txd",
                         "Infernus3.txd",
                         "Infernus4.txd",
                         "InfernusBackup.txd"
                     })
            {
                File.WriteAllText(Path.Combine(packageRoot, fileName), string.Empty);
            }

            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            typeof(MainForm).GetField("_selectedModPackageRoot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, packageRoot);
            var getSourceFiles = typeof(MainForm).GetMethod("GetSourceModelFilePaths", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var sourceFiles = (HashSet<string>)getSourceFiles.Invoke(form, [packageRoot, "infernus", true])!;

            CollectionAssert.AreEquivalent(
                new[] { "infernus.dff", "infernus.txd", "Infernus1.txd", "Infernus2.txd", "Infernus3.txd", "Infernus4.txd" },
                sourceFiles.Select(Path.GetFileName).ToArray());

            var mapName = typeof(MainForm).GetMethod("GetAssetInstallModelName", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.AreEqual("bullet", mapName.Invoke(null, ["infernus", "infernus", "bullet", false]));
            Assert.AreEqual("bullet1", mapName.Invoke(null, ["Infernus1", "infernus", "bullet", true]));
            Assert.AreEqual("bullet4", mapName.Invoke(null, ["Infernus4", "infernus", "bullet", true]));
        }
        finally
        {
            Directory.Delete(packageRoot, recursive: true);
        }
    }

    [TestMethod]
    public void Step5AssetSearch_NumericQueriesMatchIdsAndTextQueriesSearchAllFields()
    {
        var assets = new List<GameAsset>
        {
            new() { Id = "400", Name = "Police Car", NameFile = "copcarla", Category = "Emergency" },
            new() { Id = "401", Name = "Rancher", NameFile = "rancher", Category = "Off Road" },
            new() { Id = "402", Name = "Tow Truck", NameFile = "towtruck", Category = "Utility" }
        };
        var filter = typeof(MainForm).GetMethod("FilterAssetsBySearch", BindingFlags.Static | BindingFlags.NonPublic)!;

        var numericResults = (List<GameAsset>)filter.Invoke(null, [assets, "۴۰۱"])!;
        CollectionAssert.AreEqual(new[] { "rancher" }, numericResults.Select(asset => asset.NameFile).ToArray());

        var textResults = (List<GameAsset>)filter.Invoke(null, [assets, "truck"])!;
        CollectionAssert.AreEqual(new[] { "towtruck" }, textResults.Select(asset => asset.NameFile).ToArray());

        var categoryResults = (List<GameAsset>)filter.Invoke(null, [assets, "Off Road"])!;
        CollectionAssert.AreEqual(new[] { "rancher" }, categoryResults.Select(asset => asset.NameFile).ToArray());
    }

    [TestMethod]
    public void OptionalButtonName_UsesNestedPackageNameAndFallsBackToParentName()
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), "OptionalButtonName-" + Guid.NewGuid().ToString("N"));
        var optionalRoot = Path.Combine(packageRoot, "OPTIONAL");
        var optionalPackage = Path.Combine(optionalRoot, "Cavalo Branco");
        Directory.CreateDirectory(Path.Combine(optionalPackage, "modloader", "Horse Car"));

        try
        {
            var getName = typeof(MainForm).GetMethod("GetOptionalPackageDisplayName", BindingFlags.Static | BindingFlags.NonPublic)!;

            Assert.AreEqual("Cavalo Branco", getName.Invoke(null, [optionalRoot, "HorseCar"]));

            Directory.Delete(optionalPackage, recursive: true);
            Directory.CreateDirectory(optionalRoot);
            Assert.AreEqual("Horse Car", getName.Invoke(null, [optionalRoot, "Horse Car"]));
        }
        finally
        {
            Directory.Delete(packageRoot, recursive: true);
        }
    }

    [TestMethod]
    public void RandomModCategoryMenuHeight_IsCappedToScrollableViewport()
    {
        var getHeight = typeof(MainForm).GetMethod("GetRandomModMenuHeight", BindingFlags.Static | BindingFlags.NonPublic)!;

        Assert.AreEqual(116, getHeight.Invoke(null, [3, 900]));
        Assert.AreEqual(420, getHeight.Invoke(null, [30, 900]));
        Assert.AreEqual(184, getHeight.Invoke(null, [30, 200]));
    }

    [TestMethod]
    public void RebuildingWizard_DisposesPreviousPanelsAndTheirChildren()
    {
        using var host = new System.Windows.Forms.Panel();
        var oldPanel = new System.Windows.Forms.Panel();
        var oldButton = new System.Windows.Forms.Button();
        oldPanel.Controls.Add(oldButton);
        host.Controls.Add(oldPanel);

        typeof(MainForm).GetMethod("DisposeChildControls", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [host]);

        Assert.IsTrue(oldPanel.IsDisposed);
        Assert.IsTrue(oldButton.IsDisposed);
        Assert.AreEqual(0, host.Controls.Count);
    }

}
