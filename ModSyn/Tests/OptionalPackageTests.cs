using System.Runtime.CompilerServices;
using GtaSaModManager.Forms;
using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Conversion;
using GtaSaModManager.Modsyn.Parser;
using GtaSaModManager.Modsyn.Validation;
using GtaSaModManager.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class OptionalPackageTests
{
    [TestMethod]
    public void FolderNames_AreCaseInsensitiveAndAcceptParentheses()
    {
        Assert.IsTrue(ModPackageService.IsOptionalFolderName("optional"));
        Assert.IsTrue(ModPackageService.IsOptionalFolderName("(Optional)"));
        Assert.IsFalse(ModPackageService.IsOptionalFolderName("Optionals"));
        Assert.IsTrue(ModPackageService.IsOptionalsFolderName("OPTIONALS"));
        Assert.IsTrue(ModPackageService.IsOptionalsFolderName("(optionals)"));
        Assert.IsFalse(ModPackageService.IsOptionalsFolderName("optional"));
        Assert.IsFalse(ModPackageService.IsOptionalContainerName("SA.Project2DFX"));
    }

    [TestMethod]
    public void GetPayloadDirectory_SkipsOptionalFoldersEvenWhenTheySortFirst()
    {
        WithPackage(root =>
        {
            Directory.CreateDirectory(Path.Combine(root, "optional"));
            Directory.CreateDirectory(Path.Combine(root, "(optionals)"));
            Directory.CreateDirectory(Path.Combine(root, "SA.Project2DFX"));
            File.WriteAllText(Path.Combine(root, "SA.Project2DFX", "SALodLights.asi"), "x");

            Assert.AreEqual(
                Path.Combine(root, "SA.Project2DFX"),
                ModPackageService.GetPayloadDirectory(root));
        });
    }

    [TestMethod]
    public void GetPayloadDirectory_PreservesMultipleTopLevelInstallFolders()
    {
        WithPackage(root =>
        {
            Directory.CreateDirectory(Path.Combine(root, "cleo"));
            Directory.CreateDirectory(Path.Combine(root, "data"));
            File.WriteAllText(Path.Combine(root, "cleo", "script.cs"), "cleo");
            File.WriteAllText(Path.Combine(root, "data", "cargrp.dat"), "data");

            Assert.AreEqual(root, ModPackageService.GetPayloadDirectory(root));
        });
    }

    [TestMethod]
    public void IsMetadataOrNonInstallableFile_ExcludesFilesInsideOptionalFolders()
    {
        WithPackage(root =>
        {
            var inside = Path.Combine(root, "Optional", "modloader", "x.asi");
            var outside = Path.Combine(root, "Main", "x.asi");

            Assert.IsTrue(ModPackageService.IsInsideOptionalContainer(inside, root));
            Assert.IsFalse(ModPackageService.IsInsideOptionalContainer(outside, root));
            Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(inside, root));
            Assert.IsFalse(ModPackageService.IsMetadataOrNonInstallableFile(outside, root));
            // When the optional folder itself is the package root its content must install normally.
            Assert.IsFalse(ModPackageService.IsMetadataOrNonInstallableFile(
                Path.Combine(root, "Optional", "modloader", "x.asi"),
                Path.Combine(root, "Optional")));
        });
    }

    [TestMethod]
    public void FindOptionalFolders_AndOptionalsPackages()
    {
        WithPackage(root =>
        {
            Directory.CreateDirectory(Path.Combine(root, "(optional)"));
            Directory.CreateDirectory(Path.Combine(root, "Optionals", "ECHO"));
            Directory.CreateDirectory(Path.Combine(root, "Optionals", "Buy More Properties"));
            File.WriteAllText(Path.Combine(root, "Optionals", "readme.txt"), "not a package");

            Assert.AreEqual(Path.Combine(root, "(optional)"), ModPackageService.FindOptionalFolder(root));
            var optionals = ModPackageService.FindOptionalsFolder(root);
            Assert.AreEqual(Path.Combine(root, "Optionals"), optionals);
            CollectionAssert.AreEqual(
                new[] { Path.Combine(optionals!, "Buy More Properties"), Path.Combine(optionals!, "ECHO") },
                ModPackageService.GetOptionalsPackageFolders(optionals));
        });
    }

    [TestMethod]
    public void TagOptionalInstallation_LinksRecordToBaseModAndBaseLookupIgnoresIt()
    {
        WithPackage(root =>
        {
            var gameRoot = Path.Combine(root, "game");
            Directory.CreateDirectory(gameRoot);
            var baseSource = Path.Combine(root, "Mod");
            var optionalSource = Path.Combine(baseSource, "optional");
            Directory.CreateDirectory(optionalSource);
            var installed = Path.Combine(gameRoot, "modloader", "a.asi");
            Directory.CreateDirectory(Path.GetDirectoryName(installed)!);
            File.WriteAllText(installed, "x");

            ModLoaderService.RecordGameInstallation(gameRoot, "putingamefolder", "Mod", baseSource, gameRoot, new[] { installed });
            ModLoaderService.RecordGameInstallation(gameRoot, "putingamefolder", "Mod (optional)", optionalSource, gameRoot, new[] { installed });

            Assert.IsTrue(ModLoaderService.TagOptionalInstallation(gameRoot, optionalSource, "Mod", "optional"));

            var baseRecord = ModLoaderService.FindBaseInstallationBySource(gameRoot, baseSource);
            Assert.IsNotNull(baseRecord);
            Assert.AreEqual("Mod", baseRecord.ModId);
            Assert.IsNull(ModLoaderService.FindBaseInstallationBySource(gameRoot, optionalSource));

            var optionalRecords = ModLoaderService.FindOptionalInstallations(gameRoot, "Mod", "optional");
            Assert.AreEqual(1, optionalRecords.Count);
            Assert.AreEqual("Mod (optional)", optionalRecords[0].ModId);
            Assert.AreEqual(0, ModLoaderService.FindOptionalInstallations(gameRoot, "Mod", "optionals").Count);
        });
    }

    [TestMethod]
    public void OptionalAssetReplacement_MapsSourceNamesToInstalledAssetAndRestoresBackup()
    {
        WithPackage(root =>
        {
            var gameRoot = Path.Combine(root, "game");
            var packageRoot = Path.Combine(root, "optional");
            var destinationRoot = Path.Combine(gameRoot, "modloader", "Real Monster Truck", "Monster");
            Directory.CreateDirectory(packageRoot);
            Directory.CreateDirectory(destinationRoot);
            var optionalDff = Path.Combine(packageRoot, "monster.dff");
            var optionalTxd = Path.Combine(packageRoot, "monster.txd");
            var installedDff = Path.Combine(destinationRoot, "dumper.dff");
            var installedTxd = Path.Combine(destinationRoot, "dumper.txd");
            var cleoScript = Path.Combine(gameRoot, "cleo", "real_monster.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(cleoScript)!);
            File.WriteAllText(optionalDff, "optional dff");
            File.WriteAllText(optionalTxd, "optional txd");
            File.WriteAllText(installedDff, "main dff");
            File.WriteAllText(installedTxd, "main txd");
            File.WriteAllText(cleoScript, "main script");

            var mapping = new InstallationAssetMapping
            {
                SourceModelName = "monster",
                TargetModelName = "dumper",
                AssetType = "Vehicle"
            };
            var files = OptionalAssetReplacementService.ResolveInstallFiles(
                packageRoot,
                destinationRoot,
                new[] { installedDff, installedTxd },
                new[] { mapping });

            Assert.AreEqual(2, files.Count);
            CollectionAssert.AreEquivalent(
                new[] { installedDff, installedTxd },
                files.Select(file => file.DestinationPath).ToArray());

            var backups = OptionalAssetReplacementService.InstallAsync(
                    gameRoot,
                    "Real Monster Truck",
                    files)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual("optional dff", File.ReadAllText(installedDff));
            Assert.AreEqual("optional txd", File.ReadAllText(installedTxd));
            Assert.AreEqual("main script", File.ReadAllText(cleoScript));
            Assert.AreEqual(2, backups.Count);
            foreach (var backup in backups)
            {
                Assert.IsTrue(backup.OriginalExisted);
                Assert.IsTrue(File.Exists(backup.BackupFilePath));
                Assert.IsTrue(backup.BackupFilePath.Contains(
                    Path.Combine(".ModManager", "backup", "Real Monster Truck", "modloader", "Real Monster Truck", "Monster"),
                    StringComparison.OrdinalIgnoreCase));
            }

            OptionalAssetReplacementService.RestoreAsync(gameRoot, backups)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual("main dff", File.ReadAllText(installedDff));
            Assert.AreEqual("main txd", File.ReadAllText(installedTxd));
            Assert.IsFalse(backups.Any(backup => File.Exists(backup.BackupFilePath)));
            Assert.AreEqual("main script", File.ReadAllText(cleoScript));
        });
    }

    [TestMethod]
    public void OptionalAssetReplacement_ReinstallPreservesOriginalBackup()
    {
        WithPackage(root =>
        {
            var gameRoot = Path.Combine(root, "game");
            var packageRoot = Path.Combine(root, "optional");
            var destinationRoot = Path.Combine(gameRoot, "modloader", "Monster");
            Directory.CreateDirectory(packageRoot);
            Directory.CreateDirectory(destinationRoot);
            var source = Path.Combine(packageRoot, "monster.dff");
            var destination = Path.Combine(destinationRoot, "dumper.dff");
            File.WriteAllText(source, "optional v1");
            File.WriteAllText(destination, "main model");
            var files = OptionalAssetReplacementService.ResolveInstallFiles(
                packageRoot,
                destinationRoot,
                new[] { destination },
                new[] { new InstallationAssetMapping { SourceModelName = "monster", TargetModelName = "dumper" } });

            var initialBackups = OptionalAssetReplacementService.InstallAsync(gameRoot, "Parent", files)
                .GetAwaiter()
                .GetResult();
            var originalBackupPath = initialBackups.Single().BackupFilePath;
            File.WriteAllText(source, "optional v2");

            var replacementBackups = OptionalAssetReplacementService.InstallAsync(
                    gameRoot,
                    "Parent",
                    files,
                    initialBackups)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual("optional v2", File.ReadAllText(destination));
            Assert.AreEqual("main model", File.ReadAllText(originalBackupPath));
            Assert.AreEqual(originalBackupPath, replacementBackups.Single().BackupFilePath);

            OptionalAssetReplacementService.RestoreAsync(gameRoot, replacementBackups)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual("main model", File.ReadAllText(destination));
        });
    }

    [TestMethod]
    public void OptionalAssetReplacement_RestoresWhenTheSamePackageChangesTargets()
    {
        var oldDff = Path.Combine("game", "modloader", "Parent", "Monster", "dumper.dff");
        var newDff = Path.Combine("game", "modloader", "Parent", "Monster", "sentinel.dff");
        Assert.IsFalse(OptionalAssetReplacementService.ShouldRestorePreviousInstallation(
            "optional", "optional", new[] { oldDff },
            "optional", "optional", new[] { oldDff }));
        Assert.IsTrue(OptionalAssetReplacementService.ShouldRestorePreviousInstallation(
            "optional", "optional", new[] { oldDff },
            "optional", "optional", new[] { newDff }));
        Assert.IsFalse(OptionalAssetReplacementService.ShouldRestorePreviousInstallation(
            "optional-one", "optionals", new[] { oldDff },
            "optional-two", "optionals", new[] { newDff }));
        Assert.IsTrue(OptionalAssetReplacementService.ShouldRestorePreviousInstallation(
            "optional-one", "optionals", new[] { oldDff },
            "optional-two", "optionals", new[] { oldDff }));
    }

    [TestMethod]
    public void OptionalAssetReplacement_ParentRestoreRestoresOnlyItsAssetAndRemovesChildRecord()
    {
        WithPackage(root =>
        {
            var gameRoot = Path.Combine(root, "game");
            var packageRoot = Path.Combine(root, "parent");
            var optionalRoot = Path.Combine(packageRoot, "optional", "real monster");
            var assetRoot = Path.Combine(gameRoot, "modloader", "Monster Truck", "Monster");
            Directory.CreateDirectory(optionalRoot);
            Directory.CreateDirectory(assetRoot);
            var optionalDff = Path.Combine(optionalRoot, "monster.dff");
            var optionalTxd = Path.Combine(optionalRoot, "monster.txd");
            var installedDff = Path.Combine(assetRoot, "dumper.dff");
            var installedTxd = Path.Combine(assetRoot, "dumper.txd");
            var cleoScript = Path.Combine(gameRoot, "cleo", "real_monster.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(cleoScript)!);
            File.WriteAllText(optionalDff, "optional dff");
            File.WriteAllText(optionalTxd, "optional txd");
            File.WriteAllText(installedDff, "main dff");
            File.WriteAllText(installedTxd, "main txd");
            File.WriteAllText(cleoScript, "main script");
            var mapping = new InstallationAssetMapping
            {
                SourceModelName = "monster",
                TargetModelName = "dumper",
                AssetType = "Vehicle"
            };

            ModLoaderService.RecordMixedInstallation(
                gameRoot,
                "Monster Truck",
                packageRoot,
                Path.Combine(gameRoot, "modloader", "Monster Truck"),
                new[]
                {
                    new InstallationManifestPart
                    {
                        Name = "Monster",
                        Type = "vehicleandskinandweapon",
                        ModId = Path.Combine("Monster Truck", "Monster"),
                        InstalledDestination = assetRoot,
                        InstalledFiles = new List<string> { installedDff, installedTxd },
                        AssetMappings = new List<InstallationAssetMapping> { mapping }
                    }
                });

            var files = OptionalAssetReplacementService.ResolveInstallFiles(
                optionalRoot,
                assetRoot,
                new[] { installedDff, installedTxd },
                new[] { mapping });
            var backups = OptionalAssetReplacementService.InstallAsync(gameRoot, "Monster Truck", files)
                .GetAwaiter()
                .GetResult();
            ModLoaderService.RecordOptionalAssetReplacementInstallation(
                gameRoot,
                "Monster Truck",
                Path.Combine(packageRoot, "optional"),
                "Monster Truck (optional)",
                "optional",
                assetRoot,
                files.Select(file => file.DestinationPath),
                new[] { mapping },
                backups);

            var installedManifest = ModLoaderService.LoadInstallationManifest(
                ModLoaderService.GetGameInstallationsManifestPath(gameRoot));
            var replacementRecord = installedManifest.Entries.Single(entry =>
                string.Equals(entry.Type, "optionalassetreplacement", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Monster Truck", replacementRecord.ParentModId);
            Assert.AreEqual("optional", replacementRecord.OptionalKind);
            Assert.AreEqual(assetRoot, replacementRecord.InstalledDestination);
            CollectionAssert.AreEquivalent(
                new[] { installedDff, installedTxd },
                replacementRecord.InstalledFiles.ToArray());
            Assert.AreEqual("dumper", replacementRecord.AssetMappings.Single().TargetModelName);

            ModLoaderService.RestoreOptionalAssetReplacementsAsync(
                    gameRoot,
                    "Monster Truck",
                    new[] { installedDff })
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual("main dff", File.ReadAllText(installedDff));
            Assert.AreEqual("main txd", File.ReadAllText(installedTxd));
            Assert.AreEqual("main script", File.ReadAllText(cleoScript));
            var entries = ModLoaderService.LoadInstallationManifest(
                    ModLoaderService.GetGameInstallationsManifestPath(gameRoot))
                .Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("mixed", entries[0].Type);
            Assert.IsFalse(backups.Any(backup => File.Exists(backup.BackupFilePath)));
        });
    }

    [TestMethod]
    public void LegacyOptionalCleanup_RemovesOnlyTrackedModelFiles()
    {
        WithPackage(root =>
        {
            var gameRoot = Path.Combine(root, "game");
            var packageRoot = Path.Combine(root, "parent");
            var optionalRoot = Path.Combine(packageRoot, "optional");
            var legacyRoot = Path.Combine(gameRoot, "modloader", "Parent (optional)");
            var assetRoot = Path.Combine(gameRoot, "modloader", "Parent", "Asset");
            Directory.CreateDirectory(legacyRoot);
            Directory.CreateDirectory(assetRoot);
            Directory.CreateDirectory(optionalRoot);
            var legacyModel = Path.Combine(legacyRoot, "monster.dff");
            var legacyScript = Path.Combine(legacyRoot, "keep.cs");
            File.WriteAllText(legacyModel, "legacy model");
            File.WriteAllText(legacyScript, "legacy script");
            var legacyId = "Parent (optional)";
            ModLoaderService.RecordGameInstallation(
                gameRoot,
                "putinmodloader",
                legacyId,
                optionalRoot,
                legacyRoot,
                new[] { legacyModel, legacyScript });
            Assert.IsTrue(ModLoaderService.TagOptionalInstallation(gameRoot, optionalRoot, "Parent", "optional"));
            ModLoaderService.RecordOptionalAssetReplacementInstallation(
                gameRoot,
                "Parent",
                optionalRoot,
                "Parent Optional",
                "optional",
                assetRoot,
                Array.Empty<string>(),
                Array.Empty<InstallationAssetMapping>(),
                Array.Empty<OptionalAssetBackup>());

            var form = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
            typeof(MainForm).GetField("_selectedGamePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(form, gameRoot);
            typeof(MainForm).GetMethod("RemoveLegacyOptionalInstall", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(form, [optionalRoot, "Parent", "optional", assetRoot]);

            Assert.IsFalse(File.Exists(legacyModel));
            Assert.AreEqual("legacy script", File.ReadAllText(legacyScript));
            var remaining = ModLoaderService.LoadInstallationManifest(
                    ModLoaderService.GetGameInstallationsManifestPath(gameRoot))
                .Entries;
            Assert.AreEqual(2, remaining.Count);
            var replacement = remaining.Single(entry => entry.Type == "optionalassetreplacement");
            Assert.AreEqual("Parent", replacement.ParentModId);
            var legacy = remaining.Single(entry => entry.Type == "putinmodloader");
            CollectionAssert.AreEqual(new[] { legacyScript }, legacy.InstalledFiles);
            Assert.IsTrue(Directory.Exists(legacyRoot));
        });
    }

    [TestMethod]
    public void MixedPgfAndVswManifest_ConvertsBothInstallationParts()
    {
        WithPackage(root =>
        {
            Directory.CreateDirectory(Path.Combine(root, "Script"));
            Directory.CreateDirectory(Path.Combine(root, "Monster"));
            var document = ModsynParser.Parse("""
                mod {
                  type: MIX
                  list: [
                    { type: PGF folderName: "Script" }
                    { type: VSW folderName: "Monster" }
                  ]
                }
                """);
            var validation = ModsynValidator.Validate(document);

            Assert.IsTrue(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
            var converted = ModsynConfigurationConverter.Convert(document, validation, root);
            Assert.AreEqual(2, converted.Manifest.MixedParts.Count);
            Assert.AreEqual("putingamefolder", converted.Manifest.MixedParts[0].Manifest.NormalizedType);
            Assert.AreEqual("vehicleandskinandweapon", converted.Manifest.MixedParts[1].Manifest.NormalizedType);
        });
    }

    [TestMethod]
    public void OptionalAssetReplacement_RejectsFilesOutsideInstalledAsset()
    {
        WithPackage(root =>
        {
            var payload = Path.Combine(root, "optional");
            Directory.CreateDirectory(payload);
            File.WriteAllText(Path.Combine(payload, "monster.dff"), "optional");
            var destinationRoot = Path.Combine(root, "game", "modloader", "Monster");

            Assert.ThrowsException<InvalidDataException>(() => OptionalAssetReplacementService.ResolveInstallFiles(
                payload,
                destinationRoot,
                Array.Empty<string>(),
                new[] { new InstallationAssetMapping { SourceModelName = "monster", TargetModelName = "dumper" } }));
        });
    }

    [TestMethod]
    public void OptionalAssetReplacement_MapsNumberedTexturesToDifferentSelectedAssets()
    {
        foreach (var targetName in new[] { "dumper", "sentinel" })
        {
            WithPackage(root =>
            {
                var payload = Path.Combine(root, "optional");
                var gameRoot = Path.Combine(root, "game");
                var destinationRoot = Path.Combine(gameRoot, "modloader", "Parent", "Asset");
                Directory.CreateDirectory(payload);
                Directory.CreateDirectory(destinationRoot);
                File.WriteAllText(Path.Combine(payload, "monster1.txd"), "one");
                File.WriteAllText(Path.Combine(payload, "monster2.txd"), "two");
                var installedFiles = new[]
                {
                    Path.Combine(destinationRoot, targetName + "1.txd"),
                    Path.Combine(destinationRoot, targetName + "2.txd")
                };
                foreach (var installedFile in installedFiles)
                {
                    File.WriteAllText(installedFile, "main");
                }

                var files = OptionalAssetReplacementService.ResolveInstallFiles(
                    payload,
                    destinationRoot,
                    installedFiles,
                    new[]
                    {
                        new InstallationAssetMapping
                        {
                            SourceModelName = "monster",
                            TargetModelName = targetName,
                            AssetType = "Vehicle"
                        }
                    });

                CollectionAssert.AreEquivalent(installedFiles, files.Select(file => file.DestinationPath).ToArray());
            });
        }
    }

    private static void WithPackage(Action<string> test)
    {
        var root = Path.Combine(Path.GetTempPath(), "ModsynOptional-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            test(root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
