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
}