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
