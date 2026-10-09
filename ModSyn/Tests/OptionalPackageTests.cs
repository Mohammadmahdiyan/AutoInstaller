using GtaSaModManager.Models;
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
