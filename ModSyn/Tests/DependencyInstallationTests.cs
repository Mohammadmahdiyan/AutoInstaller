using GtaSaModManager.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class DependencyInstallationTests
{
    [TestMethod]
    public async Task EnsureInstalledAsync_InstallsSelectedPutInGameFolderEssentialsPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "EssentialsDependency-" + Guid.NewGuid().ToString("N"));
        var gameFolder = Path.Combine(root, "game");
        var packageRoot = Path.Combine(root, "selected-essentials");
        var payloadRoot = Path.Combine(packageRoot, "Essentials");
        Directory.CreateDirectory(Path.Combine(payloadRoot, "cleo"));
        Directory.CreateDirectory(Path.Combine(payloadRoot, "modloader"));
        File.WriteAllText(Path.Combine(payloadRoot, "cleo.asi"), "cleo");
        File.WriteAllText(Path.Combine(payloadRoot, "modloader.asi"), "modloader");
        File.WriteAllText(Path.Combine(packageRoot, "config.modsyn"), "mod { type: PGF }");

        try
        {
            var result = await DependencyInstallationService.EnsureInstalledAsync(gameFolder, packageRoot);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsTrue(result.DependenciesWereMissing);
            Assert.AreEqual(2, result.InstalledEntries);
            Assert.IsTrue(File.Exists(Path.Combine(gameFolder, "cleo.asi")));
            Assert.IsTrue(File.Exists(Path.Combine(gameFolder, "modloader.asi")));
            Assert.IsTrue(Directory.Exists(Path.Combine(gameFolder, "cleo")));
            Assert.IsTrue(Directory.Exists(Path.Combine(gameFolder, "modloader")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
    [TestMethod]
    public void AreDependenciesInstalled_RequiresBothAsiFilesAndBothFolders()
    {
        var game = Path.Combine(Path.GetTempPath(), "EssentialsStrict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(game, "cleo"));
        Directory.CreateDirectory(Path.Combine(game, "modloader"));
        try
        {
            Assert.IsFalse(DependencyInstallationService.AreDependenciesInstalled(game));
            File.WriteAllText(Path.Combine(game, "cleo.asi"), "x");
            Assert.IsFalse(DependencyInstallationService.AreDependenciesInstalled(game));
            File.WriteAllText(Path.Combine(game, "modloader.asi"), "x");
            Assert.IsTrue(DependencyInstallationService.AreDependenciesInstalled(game));
        }
        finally
        {
            Directory.Delete(game, recursive: true);
        }
    }

    [TestMethod]
    public async Task EnsureInstalledAsync_ForceReinstall_ReplacesFilesButKeepsIniSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "EssentialsReinstall-" + Guid.NewGuid().ToString("N"));
        var gameFolder = Path.Combine(root, "game");
        var packageRoot = Path.Combine(root, "essentials");
        var payloadRoot = Path.Combine(packageRoot, "Essentials");
        Directory.CreateDirectory(Path.Combine(payloadRoot, "cleo"));
        Directory.CreateDirectory(Path.Combine(payloadRoot, "modloader"));
        File.WriteAllText(Path.Combine(payloadRoot, "cleo.asi"), "new cleo");
        File.WriteAllText(Path.Combine(payloadRoot, "modloader.asi"), "new modloader");
        File.WriteAllText(Path.Combine(payloadRoot, "modloader", "modloader.ini"), "default");
        File.WriteAllText(Path.Combine(packageRoot, "config.modsyn"), "mod { type: PGF }");
        Directory.CreateDirectory(Path.Combine(gameFolder, "cleo"));
        Directory.CreateDirectory(Path.Combine(gameFolder, "modloader"));
        File.WriteAllText(Path.Combine(gameFolder, "cleo.asi"), "old cleo");
        File.WriteAllText(Path.Combine(gameFolder, "modloader.asi"), "old modloader");
        File.WriteAllText(Path.Combine(gameFolder, "modloader", "modloader.ini"), "my settings");

        try
        {
            var result = await DependencyInstallationService.EnsureInstalledAsync(
                gameFolder, packageRoot, default, forceReinstall: true);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual("new cleo", File.ReadAllText(Path.Combine(gameFolder, "cleo.asi")));
            Assert.AreEqual("new modloader", File.ReadAllText(Path.Combine(gameFolder, "modloader.asi")));
            Assert.AreEqual("my settings", File.ReadAllText(Path.Combine(gameFolder, "modloader", "modloader.ini")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}