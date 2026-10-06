using GtaSaModManager.Services;
using GtaSaModManager.Models;
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
    public void CreateDefaultManifestFile_WritesValidModsynWhenMissing()
    {
        WithPackage("package", packageRoot =>
        {
            var manifestPath = ModPackageService.CreateDefaultManifestFile(packageRoot);

            Assert.AreEqual(Path.Combine(packageRoot, "mod.modsyn"), manifestPath);
            Assert.IsTrue(ModPackageService.TryValidateModsyn(packageRoot, out var error), error);
            Assert.AreEqual(manifestPath, ModPackageService.GetManifestPath(packageRoot));
        });
    }

    [TestMethod]
    public void CreateDefaultManifestFile_DoesNotReplaceAnExistingInvalidManifest()
    {
        WithPackage("package", packageRoot =>
        {
            var manifestPath = Write(packageRoot, "mod.modsyn", "mod { type: @ }");

            Assert.AreEqual(manifestPath, ModPackageService.CreateDefaultManifestFile(packageRoot));
            Assert.AreEqual("mod { type: @ }", File.ReadAllText(manifestPath));
            Assert.IsFalse(ModPackageService.TryValidateModsyn(packageRoot, out _));
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
            ("SaveAndMission", "SaveAndMission"),
            ("SavesAndMissions", "SavesAndMissions"),
            ("SAM", "SaveAndMission"),
            ("SMS", "SavesAndMissions"),
            ("MissionDsl", "MissionDsl"),
            ("DSL", "MissionDsl")
        };

        WithPackage("package", packageRoot =>
        {
            foreach (var (typeName, expectedType) in packageTypes)
            {
                var replacementText = expectedType == "PutAndReplace"
                    ? " backup: some backupThis: \"payload.dat\" replacements: [\"payload.dat\"]"
                    : string.Empty;
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
    public void MixedAlias_ResolvesPartsAndUsesPackageRootAsPayload()
    {
        WithPackage("Mixed Pack", packageRoot =>
        {
            Directory.CreateDirectory(Path.Combine(packageRoot, "gta3img"));
            Directory.CreateDirectory(Path.Combine(packageRoot, "animations"));
            Write(packageRoot, "mod.modsyn", """
                mod {
                  type: MIX
                  list: [
                    { type: VSW folderName: "gta3img" }
                    { type: Replacing folderName: "animations" backup: none }
                  ]
                }
                """);

            var manifest = ModPackageService.ResolveManifest(packageRoot);

            Assert.AreEqual("mixed", manifest.NormalizedType);
            Assert.AreEqual(2, manifest.MixedParts.Count);
            Assert.AreEqual(
                Path.GetFullPath(packageRoot),
                ModPackageService.GetInstallPayloadDirectory(packageRoot, manifest));
        });
    }

    [TestMethod]
    public void PackageInstallation_RecordsVssAsMultiAssetType()
    {
        WithPackage("package", packageRoot =>
        {
            var gameRoot = Path.Combine(packageRoot, "game");
            var installedDirectory = Path.Combine(gameRoot, "modloader", "poco");
            Directory.CreateDirectory(installedDirectory);
            var installedFile = Path.Combine(installedDirectory, "grenade.dff");
            File.WriteAllText(installedFile, "model");

            ModLoaderService.RecordPackageInstallation(
                "vehiclesandskinsandweapons",
                "Weapons\\poco",
                packageRoot,
                installedDirectory,
                new[] { installedFile });

            var installationsPath = ModLoaderService.GetGameInstallationsManifestPath(gameRoot);
            var recordedInstallation = ModLoaderService.LoadInstallationManifest(installationsPath).Entries.Single();

            Assert.AreEqual("vehiclesandskinsandweapons", recordedInstallation.Type);
        });
    }

    [TestMethod]
    public void MixedInstallation_RecordsFilesPerPartAndFindsParentBySource()
    {
        WithPackage("Mixed Pack", packageRoot =>
        {
            var gameRoot = Path.Combine(packageRoot, "game");
            var vehicleFile = Path.Combine(gameRoot, "modloader", "Mixed Pack", "gta3img", "male01.dff");
            var animationFile = Path.Combine(gameRoot, "anim", "anim.ifp");
            Directory.CreateDirectory(Path.GetDirectoryName(vehicleFile)!);
            Directory.CreateDirectory(Path.GetDirectoryName(animationFile)!);
            File.WriteAllText(vehicleFile, "model");
            File.WriteAllText(animationFile, "animation");

            ModLoaderService.RecordMixedInstallation(
                gameRoot,
                "Mixed Pack",
                packageRoot,
                Path.Combine(gameRoot, "modloader", "Mixed Pack"),
                new[]
                {
                    new InstallationManifestPart
                    {
                        Name = "gta3img",
                        Type = "vehicleandskinandweapon",
                        ModId = "Mixed Pack_gta3img",
                        InstalledDestination = Path.GetDirectoryName(vehicleFile)!,
                        InstalledFiles = new List<string> { vehicleFile }
                    },
                    new InstallationManifestPart
                    {
                        Name = "animations",
                        Type = "replacing",
                        ModId = "Mixed Pack_animations",
                        InstalledDestination = gameRoot,
                        InstalledFiles = new List<string> { animationFile }
                    }
                });

            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameRoot);
            var entry = ModLoaderService.FindGameInstallationRecord(gameRoot, "mixed", "Mixed Pack", packageRoot);

            Assert.IsNotNull(entry);
            Assert.AreEqual("mixed", entry.Type);
            Assert.AreEqual(2, entry.MixedParts.Count);
            CollectionAssert.AreEquivalent(new[] { vehicleFile, animationFile }, entry.InstalledFiles);
            Assert.AreEqual(1, ModLoaderService.LoadInstallationManifest(manifestPath).Entries.Count);

            Assert.IsTrue(ModLoaderService.RemoveMixedInstallationPart(gameRoot, "Mixed Pack", "Mixed Pack_gta3img"));
            var remainingPart = ModLoaderService.FindGameInstallationRecord(gameRoot, "mixed", "Mixed Pack", packageRoot);
            Assert.IsNotNull(remainingPart);
            Assert.AreEqual(1, remainingPart.MixedParts.Count);
            CollectionAssert.AreEqual(new[] { animationFile }, remainingPart.InstalledFiles);

            Assert.IsTrue(ModLoaderService.RemoveMixedInstallationPart(gameRoot, "Mixed Pack", "Mixed Pack_animations"));
            Assert.IsNull(ModLoaderService.FindGameInstallationRecord(gameRoot, "mixed", "Mixed Pack", packageRoot));
            Assert.AreEqual(0, ModLoaderService.LoadInstallationManifest(manifestPath).Entries.Count);
        });
    }

    [TestMethod]
    public void AssetSelection_IsEnabledOnlyForVswAndVssPackageTypes()
    {
        Assert.IsFalse(new ModManifest { Type = "PIM" }.SupportsAssetSelection);
        Assert.IsFalse(new ModManifest { Type = "Replacing" }.SupportsAssetSelection);
        Assert.IsTrue(new ModManifest { Type = "VSW" }.SupportsAssetSelection);
        Assert.IsTrue(new ModManifest { Type = "VSS" }.SupportsAssetSelection);
    }

    [TestMethod]
    public void PutInGameFolder_UsesPackageRootAsInstallPayload()
    {
        WithPackage("Ragdoll", packageRoot =>
        {
            Directory.CreateDirectory(Path.Combine(packageRoot, "CLEO"));
            File.WriteAllText(Path.Combine(packageRoot, "Ragdoll_physics.asi"), "asi");
            File.WriteAllText(Path.Combine(packageRoot, "CLEO", "Ragdoll_FrameAdjust.cs"), "cleo");
            Write(packageRoot, "Ragdoll.modsyn", "mod { type: PutInGameFolder }");

            var manifest = ModPackageService.ResolveManifest(packageRoot);

            Assert.AreEqual(
                Path.GetFullPath(packageRoot),
                ModPackageService.GetInstallPayloadDirectory(packageRoot, manifest));
        });
    }

    [TestMethod]
    public void BackupStorage_SelectsOnlyExistingIncludedGameFiles()
    {
        WithPackage("Football", packageRoot =>
        {
            Write(packageRoot, "mod.modsyn", """
                mod {
                  type: PutAndReplace
                  backup: some
                  backupThis: "data/maps/generic"
                  dontBackupThis: "data/maps/generic/skip.ide"
                }
                """);

            var gameRoot = Path.Combine(Path.GetTempPath(), "ModsynBackupSelection-" + Guid.NewGuid().ToString("N"));
            var includedFile = Path.Combine(gameRoot, "data", "maps", "generic", "multiobj.ide");
            var excludedFile = Path.Combine(gameRoot, "data", "maps", "generic", "skip.ide");
            var missingFile = Path.Combine(gameRoot, "data", "maps", "generic", "missing.ide");
            Directory.CreateDirectory(Path.GetDirectoryName(includedFile)!);
            File.WriteAllText(includedFile, "original");
            File.WriteAllText(excludedFile, "original");

            try
            {
                var backup = ModPackageService.ResolveModsynConfiguration(packageRoot).Backup;
                var selected = BackupStorageService.SelectFilesToBackup(
                    gameRoot,
                    new[] { includedFile, excludedFile, missingFile },
                    backup);

                CollectionAssert.AreEqual(new[] { includedFile }, selected);
            }
            finally
            {
                Directory.Delete(gameRoot, recursive: true);
            }
        });
    }

    [TestMethod]
    public void Replacing_UsesPackageRootAsInstallPayload()
    {
        WithPackage("SFX Audio Original", packageRoot =>
        {
            Directory.CreateDirectory(Path.Combine(packageRoot, "audio", "SFX"));
            File.WriteAllText(Path.Combine(packageRoot, "audio", "SFX", "FEET"), "audio data");
            Write(packageRoot, "mod.modsyn", "mod { type: Replacing }");

            var manifest = ModPackageService.ResolveManifest(packageRoot);

            Assert.AreEqual(
                Path.GetFullPath(packageRoot),
                ModPackageService.GetInstallPayloadDirectory(packageRoot, manifest));
        });
    }

    [TestMethod]
    public void PutAndReplaceTypes_UsePackageRootAsInstallPayload()
    {
        WithPackage("Ben10", packageRoot =>
        {
            Directory.CreateDirectory(Path.Combine(packageRoot, "models"));
            Directory.CreateDirectory(Path.Combine(packageRoot, "modloader", "Ben10 Mod"));
            File.WriteAllText(Path.Combine(packageRoot, "bass.dll"), "root file");
            File.WriteAllText(Path.Combine(packageRoot, "models", "hud.txd"), "nested file");

            foreach (var type in new[] { "PutAndReplace", "PutAndReplaces" })
            {
                var selectors = type == "PutAndReplace" ? "backup: some backupThis: \"models\"" : string.Empty;
                Write(packageRoot, "mod.modsyn", $"mod {{ type: {type} {selectors} }}");
                var manifest = ModPackageService.ResolveManifest(packageRoot);

                Assert.AreEqual(
                    Path.GetFullPath(packageRoot),
                    ModPackageService.GetInstallPayloadDirectory(packageRoot, manifest),
                    type);
            }
        });
    }

    [TestMethod]
    public void MissionDsl_UsesPackageRootForReadmeAndImagePreviews()
    {
        WithPackage("Missions", packageRoot =>
        {
            var dslPayload = Path.Combine(packageRoot, "DSL");
            Directory.CreateDirectory(dslPayload);
            File.WriteAllText(Path.Combine(packageRoot, "readme.txt"), "instructions");
            File.WriteAllText(Path.Combine(packageRoot, "gallery1.jpg"), "preview");
            Write(packageRoot, "mod.modsyn", "mod { type: DSL }");

            var manifest = ModPackageService.ResolveManifest(packageRoot);
            var payloadPath = ModPackageService.GetInstallPayloadDirectory(packageRoot, manifest);

            Assert.AreEqual(packageRoot, ModPackageService.GetPreviewDirectory(packageRoot, payloadPath, manifest));
            Assert.AreEqual(dslPayload, payloadPath);
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
                                    backup: some
                                    backupThis: "data"
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
    public void BackupStorage_DefaultPlanStoresModFilesUnderManagerBackups()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynBackup-" + Guid.NewGuid().ToString("N"));
        var originalFile = Path.Combine(gameFolder, "data", "handling.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(originalFile)!);
        File.WriteAllText(originalFile, "original");
        try
        {
            var plan = BackupStorageService.CreatePlan(gameFolder, "Ben10", new[] { originalFile });

            Assert.IsTrue(plan.HasBackup, plan.ErrorMessage);
            Assert.AreEqual(Path.Combine(gameFolder, ".ModManager", "backup"), plan.BackupRoot);
            var backupFile = ModPackageService.BackupOriginalFileForReplacement(gameFolder, originalFile, "Ben10", plan.BackupRoot);
            Assert.AreEqual(Path.Combine(gameFolder, ".ModManager", "backup", "Ben10", "data", "handling.cfg"), backupFile);
        }
        finally
        {
            if (Directory.Exists(gameFolder))
            {
                Directory.Delete(gameFolder, recursive: true);
            }
        }
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

    [TestMethod]
    public void BackupStorage_ResolvesModBackupDirectoryFromReplacementRecords()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynBackupDirectory-" + Guid.NewGuid().ToString("N"));
        var backupDirectory = Path.Combine(gameFolder, ".ModManager", "backup", "Football");
        var originalFile = Path.Combine(gameFolder, "data", "maps", "generic", "multiobj.ide");
        var backupFile = Path.Combine(backupDirectory, "data", "maps", "generic", "multiobj.ide");
        Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
        File.WriteAllText(backupFile, "original");

        try
        {
            var directories = BackupStorageService.GetModBackupDirectories(
                gameFolder,
                "Football",
                new[]
                {
                    new ReplaceInstallationRecord
                    {
                        GameFolder = gameFolder,
                        OriginalFilePath = originalFile,
                        BackupFilePath = backupFile
                    }
                });

            CollectionAssert.AreEqual(new[] { backupDirectory }, directories);
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [TestMethod]
    public void ModLoaderService_FindsInstalledFilesWithoutReplacementHistory()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynInstalledFiles-" + Guid.NewGuid().ToString("N"));
        var replacedFile = Path.Combine(gameFolder, "data", "old.cfg");
        var addedFile = Path.Combine(gameFolder, "new", "plugin.asi");

        var unreplacedFiles = ModLoaderService.GetUnreplacedInstalledFiles(
            new[] { replacedFile, addedFile },
            new[] { replacedFile });

        CollectionAssert.AreEqual(new[] { Path.GetFullPath(addedFile) }, unreplacedFiles);
    }

    [TestMethod]
    public void ModLoaderService_RemovesOnlyTheRequestedGameInstallationRecord()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynInstallationRecord-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(gameFolder);

        try
        {
            ModLoaderService.RecordGameInstallation(gameFolder, "putandreplace", "Football", "package-a", gameFolder, Array.Empty<string>());
            ModLoaderService.RecordGameInstallation(gameFolder, "replacing", "Other Mod", "package-b", gameFolder, Array.Empty<string>());

            Assert.IsTrue(ModLoaderService.RemoveGameInstallationRecord(gameFolder, "Football"));

            var remaining = ModLoaderService.LoadInstallationManifest(
                ModLoaderService.GetGameInstallationsManifestPath(gameFolder)).Entries;
            CollectionAssert.AreEqual(new[] { "Other Mod" }, remaining.Select(entry => entry.ModId).ToArray());
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [TestMethod]
    public void ModLoaderService_FindsExistingReplacingInstallByNameOrPackagePath()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynFindInstallation-" + Guid.NewGuid().ToString("N"));
        var packageRoot = Path.Combine(Path.GetTempPath(), "Football-RIP");
        Directory.CreateDirectory(gameFolder);

        try
        {
            ModLoaderService.RecordGameInstallation(
                gameFolder,
                "replacing",
                "Football",
                packageRoot,
                gameFolder,
                Array.Empty<string>());

            Assert.IsNotNull(ModLoaderService.FindGameInstallationRecord(gameFolder, "replacing", "Football"));
            Assert.IsNotNull(ModLoaderService.FindGameInstallationRecord(gameFolder, "replacing", "Renamed Mod", packageRoot));
            Assert.IsNull(ModLoaderService.FindGameInstallationRecord(gameFolder, "putandreplaces", "Football"));
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [TestMethod]
    public void ModLoaderService_ClearDirectoryContentsKeepsRootAndRemovesNestedEntries()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ModsynClearDsl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "nested", "deeper"));
        File.WriteAllText(Path.Combine(directory, "save.dat"), "mission");
        File.WriteAllText(Path.Combine(directory, "nested", "keep.txt"), "data");

        try
        {
            ModLoaderService.ClearDirectoryContents(directory);

            Assert.IsTrue(Directory.Exists(directory));
            Assert.AreEqual(0, Directory.GetFileSystemEntries(directory).Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void MissionDslInstallation_IsRecordedInGameInstallationsManifest()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynMissionDslManifest-" + Guid.NewGuid().ToString("N"));
        var installedFile = Path.Combine(Path.GetTempPath(), "ModsynMissionDslFile-" + Guid.NewGuid().ToString("N"), "DSL", "mission.dat");
        Directory.CreateDirectory(gameFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(installedFile)!);
        File.WriteAllText(installedFile, "mission");

        try
        {
            ModLoaderService.RecordUserFilesInstallation(
                gameFolder,
                "Missions",
                "missiondsl",
                new[] { installedFile });

            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameFolder);
            var entry = ModLoaderService.LoadInstallationManifest(manifestPath).Entries.Single();
            var foundInstallation = ModLoaderService.FindUserFilesInstallation(gameFolder, "Renamed Mission Package", "missiondsl");

            Assert.AreEqual("Missions", entry.ModId);
            Assert.AreEqual("missiondsl", entry.Type);
            CollectionAssert.AreEqual(new[] { installedFile }, entry.InstalledFiles);
            Assert.AreEqual(manifestPath, foundInstallation.ManifestPath);
            Assert.IsNotNull(foundInstallation.Entry);
            Assert.AreEqual(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GTA San Andreas User Files", "DSL"),
                entry.InstalledDestination);
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
            Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(installedFile)!)!, recursive: true);
        }
    }

    [TestMethod]
    public void SaveMissionInstallation_MergesSlotsAndUninstallsOnlyRecordedFiles()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynSaveMission-" + Guid.NewGuid().ToString("N"));
        var userFilesRoot = Path.Combine(gameFolder, "Documents", "GTA San Andreas User Files");
        var firstSlot = Path.Combine(userFilesRoot, "GTASAsf6.b");
        var secondSlot = Path.Combine(userFilesRoot, "DYOM1.dat");
        var unrelatedSlot = Path.Combine(userFilesRoot, "GTASAsf1.b");
        Directory.CreateDirectory(userFilesRoot);
        Directory.CreateDirectory(gameFolder);
        File.WriteAllText(firstSlot, "save");
        File.WriteAllText(secondSlot, "mission");
        File.WriteAllText(unrelatedSlot, "unrelated");

        try
        {
            ModLoaderService.RecordUserFilesInstallation(
                gameFolder,
                "Campaign Pack",
                "savesandmissions",
                new[] { firstSlot },
                userFilesRoot,
                sourcePackagePath: Path.Combine(gameFolder, "Campaign Pack"));
            ModLoaderService.RecordUserFilesInstallation(
                gameFolder,
                "Campaign Pack",
                "savesandmissions",
                new[] { secondSlot },
                userFilesRoot,
                mergeExistingFiles: true);

            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gameFolder);
            var entry = ModLoaderService.LoadInstallationManifest(manifestPath).Entries.Single();
            CollectionAssert.AreEquivalent(new[] { firstSlot, secondSlot }, entry.InstalledFiles);
            Assert.AreEqual(Path.Combine(gameFolder, "Campaign Pack"), entry.SourcePackagePath);

            Assert.IsTrue(ModLoaderService.TryUninstallByModId(
                "Campaign Pack",
                "savesandmissions",
                installedDestination: null,
                gamePath: gameFolder));

            Assert.IsFalse(File.Exists(firstSlot));
            Assert.IsFalse(File.Exists(secondSlot));
            Assert.IsTrue(File.Exists(unrelatedSlot));
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [DataTestMethod]
    [DataRow("saveandmission")]
    [DataRow("savesandmissions")]
    public void SaveMissionInstallation_UninstallsRecordedFilesForEachType(string modType)
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynSaveMissionUninstall-" + Guid.NewGuid().ToString("N"));
        var installedFile = Path.Combine(gameFolder, "Documents", "GTA San Andreas User Files", "GTASAsf3.b");
        Directory.CreateDirectory(Path.GetDirectoryName(installedFile)!);
        File.WriteAllText(installedFile, "save");

        try
        {
            ModLoaderService.RecordUserFilesInstallation(
                gameFolder,
                "Single Save Pack",
                modType,
                new[] { installedFile });

            Assert.IsTrue(ModLoaderService.TryUninstallByModId(
                "Single Save Pack",
                modType,
                installedDestination: null,
                gamePath: gameFolder));
            Assert.IsFalse(File.Exists(installedFile));
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [TestMethod]
    public async Task AddToUserFile_CopiesPackageAndBaseSourcesAndUninstallsRecordedFiles()
    {
        var gameFolder = Path.Combine(Path.GetTempPath(), "ModsynUserFileAddOn-" + Guid.NewGuid().ToString("N"));
        var packageRoot = Path.Combine(gameFolder, "Package");
        var packageFolder = Path.Combine(packageRoot, "JLNSJ");
        var baseModsRoot = Path.Combine(gameFolder, "BaseMods");
        var baseFile = Path.Combine(baseModsRoot, "Scripts", "DYOM", "text.gxt");
        var userFilesRoot = Path.Combine(gameFolder, "Documents", "GTA San Andreas User Files");
        var unrelatedFile = Path.Combine(userFilesRoot, "unrelated.txt");
        Directory.CreateDirectory(packageFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(baseFile)!);
        Directory.CreateDirectory(userFilesRoot);
        await File.WriteAllTextAsync(Path.Combine(packageFolder, "addon.dat"), "package addon");
        await File.WriteAllTextAsync(baseFile, "base addon");
        await File.WriteAllTextAsync(unrelatedFile, "keep");

        try
        {
            var plan = UserFilesInstallService.CreateCopyPlan(
                new[]
                {
                    new ModUserFileInstallEntry { From = "JLNSJ" },
                    new ModUserFileInstallEntry { FromBase = "Scripts\\DYOM\\text.gxt", To = "MPACK" }
                },
                packageRoot,
                baseModsRoot,
                userFilesRoot);

            Assert.AreEqual(2, plan.Count);
            Assert.IsTrue(plan.Any(copy => copy.DestinationPath == Path.Combine(userFilesRoot, "JLNSJ", "addon.dat")));
            Assert.IsTrue(plan.Any(copy => copy.DestinationPath == Path.Combine(userFilesRoot, "MPACK", "text.gxt")));
            foreach (var copy in plan)
            {
                await FileCopyService.CopyFileAsync(copy.SourcePath, copy.DestinationPath);
            }

            ModLoaderService.RecordUserFilesInstallation(
                gameFolder,
                "Add-on package",
                "savesandmissions",
                plan.Select(copy => copy.DestinationPath),
                userFilesRoot);

            Assert.IsTrue(ModLoaderService.TryUninstallByModId(
                "Add-on package",
                "savesandmissions",
                installedDestination: null,
                gamePath: gameFolder));
            Assert.IsFalse(plan.Any(copy => File.Exists(copy.DestinationPath)));
            Assert.IsTrue(File.Exists(unrelatedFile));
        }
        finally
        {
            Directory.Delete(gameFolder, recursive: true);
        }
    }

    [TestMethod]
    public void SaveMissionNameReader_ReadsNameFromGtaSaveHeader()
    {
        var savePath = Path.Combine(Path.GetTempPath(), "ModsynSaveName-" + Guid.NewGuid().ToString("N") + ".b");
        var bytes = new byte[32];
        "BLOCK"u8.CopyTo(bytes);
        bytes[8] = (byte)'5';
        System.Text.Encoding.ASCII.GetBytes("Big Smoke").CopyTo(bytes, 9);

        try
        {
            File.WriteAllBytes(savePath, bytes);

            Assert.AreEqual("Big Smoke", GtaSaModManager.Services.SaveMissionNameReader.TryReadGtaSaveName(savePath));
        }
        finally
        {
            File.Delete(savePath);
        }
    }

    [TestMethod]
    public void SaveMissionNameReader_ReadsNameFromDyomMissionHeader()
    {
        var headers = new[]
        {
            (Header: new byte[] { 0xFC, 0xFF, 0xFF, 0xFF }, Name: "Kill The Johnsons Mother"),
            (Header: new byte[] { 0x06, 0x00, 0x00, 0x00 }, Name: "CMEPTELJHAR FOHKA"),
            (Header: new byte[] { 0xFA, 0xFF, 0xFF, 0xFF }, Name: "CMEPTELJHAR FOHKA")
        };

        foreach (var header in headers)
        {
            var missionPath = Path.Combine(Path.GetTempPath(), "ModsynMissionName-" + Guid.NewGuid().ToString("N") + ".dat");
            var bytes = new byte[64];
            header.Header.CopyTo(bytes, 0);
            System.Text.Encoding.ASCII.GetBytes(header.Name).CopyTo(bytes, 4);
            try
            {
                File.WriteAllBytes(missionPath, bytes);

                Assert.AreEqual(header.Name, GtaSaModManager.Services.SaveMissionNameReader.TryReadDyomMissionName(missionPath));
            }
            finally
            {
                File.Delete(missionPath);
            }
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