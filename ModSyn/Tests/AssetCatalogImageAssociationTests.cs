using GtaSaModManager.Models;
using GtaSaModManager.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Modsyn.Lexer.Tests;

[TestClass]
public sealed class AssetCatalogImageAssociationTests
{
    [TestMethod]
    public void ResolveNameFileImagePath_UsesMatchingImageForTheAsset()
    {
        var root = Path.Combine(Path.GetTempPath(), "ModsynImageAssociation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var firstImage = Path.Combine(root, "sweeper.png");
            var secondImage = Path.Combine(root, "carhelper.png");
            File.WriteAllBytes(firstImage, [0]);
            File.WriteAllBytes(secondImage, [1]);

            var asset = new GameAsset
            {
                NameFile = "sweeper",
                Name = "Sweeper",
                AssetType = "Vehicle"
            };

            var result = AssetCatalogService.ResolveNameFileImagePath(asset, new[] { secondImage, firstImage });

            Assert.AreEqual(firstImage, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ResolveNameFileImagePath_MatchesImageContainingNameFileWithoutModelExtension()
    {
        var root = Path.Combine(Path.GetTempPath(), "ModsynImageAssociation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var frontImage = Path.Combine(root, "admiral_front.jpg");
            var rearImage = Path.Combine(root, "admiral_rear.jpg");
            var unrelatedImage = Path.Combine(root, "aaa.jpg");
            File.WriteAllBytes(frontImage, [0]);
            File.WriteAllBytes(rearImage, [1]);
            File.WriteAllBytes(unrelatedImage, [2]);

            var asset = new GameAsset
            {
                NameFile = "admiral.dff",
                Name = "Admiral",
                AssetType = "Vehicle"
            };

            var result = AssetCatalogService.ResolveNameFileImagePath(
                asset,
                new[] { unrelatedImage, rearImage, frontImage });

            Assert.AreEqual(frontImage, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void FindNameFileImagePaths_ReturnsOnlyImagesForTheRequestedModel()
    {
        var root = Path.Combine(Path.GetTempPath(), "ModsynImageAssociation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var greenwooImage = Path.Combine(root, "greenwoo.jpg");
            var greenwooRearImage = Path.Combine(root, "greenwoo_rear.jpg");
            var hotringImage = Path.Combine(root, "hotring.jpg");
            File.WriteAllBytes(greenwooImage, [0]);
            File.WriteAllBytes(greenwooRearImage, [1]);
            File.WriteAllBytes(hotringImage, [2]);

            var result = AssetCatalogService.FindNameFileImagePaths(
                "greenwoo.dff",
                new[] { hotringImage, greenwooRearImage, greenwooImage });

            CollectionAssert.AreEqual(new[] { greenwooImage, greenwooRearImage }, result.ToArray());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ResolveNameFileImagePath_ReturnsNullWhenNoImageMatchesNameFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "ModsynImageAssociation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var image = Path.Combine(root, "preview.png");
            File.WriteAllBytes(image, [0]);

            var asset = new GameAsset
            {
                NameFile = "sweeper",
                Name = "Sweeper",
                AssetType = "Vehicle"
            };

            var result = AssetCatalogService.ResolveNameFileImagePath(asset, new[] { image });

            Assert.IsNull(result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
