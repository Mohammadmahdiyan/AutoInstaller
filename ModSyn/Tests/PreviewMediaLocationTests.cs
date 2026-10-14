using GtaSaModManager.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class PreviewMediaLocationTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "PreviewMediaLocationTests", "Bullethole Pack");

    [TestMethod]
    public void ImageInPackageRoot_IsPreviewAndNotInstalled()
    {
        var path = Path.Combine(Root, "screen.png");
        Assert.IsTrue(ModPackageService.IsPreviewMediaLocation(path, Root));
        Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(path, Root));
    }

    [TestMethod]
    [DataRow("gallery")]
    [DataRow("images")]
    [DataRow("image")]
    [DataRow("screenshot")]
    [DataRow("Screenshots")]
    [DataRow("Preview")]
    [DataRow("Previews")]
    public void ImageInPreviewFolder_IsPreviewAndNotInstalled(string folder)
    {
        var path = Path.Combine(Root, folder, "a.jpg");
        Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(path, Root));
    }

    [TestMethod]
    public void ImageInsideInstalledFolder_IsInstalled()
    {
        var path = Path.Combine(Root, "Bullethole", "bullethole.png");
        Assert.IsFalse(ModPackageService.IsPreviewMediaLocation(path, Root));
        Assert.IsFalse(ModPackageService.IsMetadataOrNonInstallableFile(path, Root));
    }

    [TestMethod]
    public void ImageWithoutKnownPackageRoot_KeepsLegacyBehaviour()
    {
        Assert.IsTrue(ModPackageService.IsMetadataOrNonInstallableFile(Path.Combine(Root, "Bullethole", "bullethole.png")));
    }
}
