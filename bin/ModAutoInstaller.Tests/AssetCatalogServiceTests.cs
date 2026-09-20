using System.Text;
using GtaSaModManager.Services;

namespace ModAutoInstaller.Tests;

public class AssetCatalogServiceTests
{
    [Fact]
    public void DetectPackageAssets_UsesNameFileMatchesOnly()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "mod-manager-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(Path.Combine(tempRoot, "bfori.dff"), "dff");
            File.WriteAllText(Path.Combine(tempRoot, "bfori.txd"), "txd");
            File.WriteAllText(Path.Combine(tempRoot, "unknown.dff"), "dff");

            var service = new AssetCatalogService();
            var matches = service.FindAssetsInPackage(tempRoot);

            Assert.Contains(matches, asset => string.Equals(asset.NameFile, "bfori", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(matches, asset => string.Equals(asset.NameFile, "unknown", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void BuildInstallPlan_RenamesAssetFilesToNameFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "mod-manager-tests-plan", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sourceDff = Path.Combine(tempRoot, "custom_name.dff");
            var sourceTxd = Path.Combine(tempRoot, "custom_name.txd");
            File.WriteAllText(sourceDff, "dff");
            File.WriteAllText(sourceTxd, "txd");

            var catalog = new AssetCatalogService();
            var asset = catalog.LoadAssets().FirstOrDefault(item => string.Equals(item.NameFile, "bfori", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(asset);

            var targetName = asset!.NameFile;
            var targetDff = Path.Combine(tempRoot, targetName + ".dff");
            var targetTxd = Path.Combine(tempRoot, targetName + ".txd");

            Assert.NotEqual(Path.GetFileName(sourceDff), Path.GetFileName(targetDff));
            Assert.NotEqual(Path.GetFileName(sourceTxd), Path.GetFileName(targetTxd));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }
}
