using GtaSaModManager.Modsyn.Conversion;
using GtaSaModManager.Modsyn.Parser;
using GtaSaModManager.Modsyn.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GtaSaModManager.Modsyn.Tests;

[TestClass]
public sealed class ModsynConfigurationConverterTests
{
    [TestMethod]
    public void Convert_MapsTypeDeletionInstallIgnoreAndBackupConfiguration()
    {
        var converted = Convert("""
            mod {
              type: RIP
              deleteThis: "old/file.dat"
              deleteThese: ["old/folder", "old/file.dat"]
              installThis: "models/example.dff"
              installThese: ["models/example.txd"]
              ignoreThis: "readme.txt"
              ignoreThese: ["logs/debug.txt"]
              backup: some
              backupThis: "data/original.dat"
              backupThese: ["models/original.dff"]
              dontBackupThis: "data/large.img"
              dontBackupThese: ["models/large.txd"]
            }
            """);

        Assert.AreEqual("Replacing", converted.Manifest.Type);
        CollectionAssert.AreEqual(new[] { "old\\file.dat", "old\\folder" }, converted.Manifest.DeleteThis);
        CollectionAssert.AreEqual(new[] { "models\\example.dff", "models\\example.txd" }, converted.Manifest.InstallFiles);
        CollectionAssert.AreEqual(new[] { "readme.txt", "logs\\debug.txt" }, converted.Manifest.IgnoreFiles);
        Assert.AreEqual(ModsynBackupMode.Some, converted.Backup.Mode);
        CollectionAssert.AreEqual(new[] { "data\\original.dat", "models\\original.dff" }, converted.Backup.IncludePaths.ToArray());
        CollectionAssert.AreEqual(new[] { "data\\large.img", "models\\large.txd" }, converted.Backup.ExcludePaths.ToArray());
    }

    [TestMethod]
    public void Convert_PreservesRequirementMergeAndAddressPriority()
    {
        var converted = Convert("""
            mod {
              requires: [
                {
                  checkThis: "outer/check.dat"
                  checkThese: ["outer/a" "outer/b"]
                  reqPath: "outer/package"
                  require: {
                    checkThis: "nested/check.dat"
                    checkThese: ["nested/only"]
                    reqAddress: "nested/package"
                  }
                }
                { reqAddress: "primary" reqPath: "fallback" }
              ]
            }
            """);

        Assert.AreEqual(2, converted.Requirements.Count);
        Assert.AreEqual("outer/check.dat", converted.Requirements[0].CheckThis);
        CollectionAssert.AreEqual(new[] { "outer/a", "outer/b" }, converted.Requirements[0].CheckThese.ToArray());
        Assert.AreEqual("outer/package", converted.Requirements[0].RequestAddress);
        Assert.AreEqual("primary", converted.Requirements[1].RequestAddress);
        Assert.AreEqual(2, converted.Manifest.Requires.Count);
        CollectionAssert.AreEqual(
          new[] { "outer\\check.dat", "outer\\a", "outer\\b" },
          converted.Manifest.Requires[0].CheckPaths);
        Assert.AreEqual("outer/package", converted.Manifest.Requires[0].ReqAddress);
    }

    [TestMethod]
    public void Convert_MapsReplacementObjectsAndStringShorthand()
    {
        var converted = Convert("""
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

        Assert.AreEqual("PutAndReplace", converted.Manifest.Type);
        Assert.AreEqual(2, converted.Replacements.Count);
        Assert.AreEqual("data\\handling.cfg", converted.Replacements[0].Source);
        Assert.AreEqual(converted.Replacements[0].Source, converted.Replacements[0].Target);
        Assert.AreEqual("custom\\weapon.dat", converted.Replacements[1].Source);
        Assert.AreEqual("data\\weapon.dat", converted.Replacements[1].Target);
    }

    [TestMethod]
    public void Convert_DefaultsTypeAndBackupWithoutChangingExistingDefaults()
    {
        var converted = Convert("mod { installThis: \"one.cs\" }");

        Assert.AreEqual("PutInModLoader", converted.Manifest.Type);
        Assert.AreEqual(ModsynBackupMode.All, converted.Backup.Mode);
        Assert.IsTrue(converted.Backup.Enabled);
        Assert.AreEqual(1, converted.Manifest.InstallFiles.Count);
        Assert.AreEqual(0, converted.Manifest.InstallFolders.Count);
    }

      [TestMethod]
      public void BackupConfiguration_UsesDefaultWhitelistAndExcludePrecedence()
      {
        var defaultBackup = Convert("mod { type: Replacing }").Backup;
        Assert.IsTrue(defaultBackup.ShouldBackup("data\\file.dat"));

          var allBackup = Convert("mod { type: Replacing backup: all }").Backup;
          Assert.IsTrue(allBackup.ShouldBackup("models\\car.dff"));

          var noBackup = Convert("mod { type: Replacing backup: none }").Backup;
          Assert.IsFalse(noBackup.ShouldBackup("data\\file.dat"));

        var filteredBackup = Convert("""
          mod {
            type: Replacing
              backup: some
            backupThis: "data"
            dontBackupThis: "data/cache"
          }
          """).Backup;
        Assert.IsTrue(filteredBackup.ShouldBackup("data/file.dat"));
        Assert.IsFalse(filteredBackup.ShouldBackup("data/cache/file.dat"));
        Assert.IsFalse(filteredBackup.ShouldBackup("models/car.dff"));

        var disabledBackup = Convert("mod { type: Replacing backup: none }").Backup;
        Assert.IsFalse(disabledBackup.ShouldBackup("data/file.dat"));
      }

    [TestMethod]
    public void Convert_ClassifiesExistingPackageDirectoriesForLegacySelectionFields()
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), "ModsynConversion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(packageRoot, "models"));
        try
        {
            var document = ModsynParser.Parse("mod { installThis: \"models\" ignoreThis: \"logs\" }");
            var converted = ModsynConfigurationConverter.Convert(document, packageRoot);

            CollectionAssert.AreEqual(new[] { "logs" }, converted.Manifest.IgnoreFiles);
            CollectionAssert.AreEqual(new[] { "models" }, converted.Manifest.InstallFolders);
        }
        finally
        {
            Directory.Delete(packageRoot, recursive: true);
        }
    }

    [TestMethod]
    public void BackupConfiguration_BackupThisMatchesNestedGameFilePath()
    {
      var backup = Convert("""
        mod {
          type: PutAndReplace
          backup: some
          backupThis: "data\\maps\\generic\\multiobj.ide"
        }
        """).Backup;

      Assert.IsTrue(backup.ShouldBackup("data\\maps\\generic\\multiobj.ide"));
      Assert.IsFalse(backup.ShouldBackup("data\\maps\\generic\\other.ide"));
    }

    [TestMethod]
    public void BackupConfiguration_NormalizesEscapedWindowsSeparators()
    {
      var backup = Convert("""
        mod {
          type: PutAndReplace
          backup: some
          backupThis: "data\\maps\\generic\\multiobj.ide"
        }
        """).Backup;

      Assert.IsTrue(backup.ShouldBackup("data\\maps\\generic\\multiobj.ide"));
      Assert.IsFalse(backup.ShouldBackup("data\\maps\\generic\\other.ide"));
    }

    [TestMethod]
    public void Convert_RejectsInvalidConfigurationInsteadOfPartiallyMapping()
    {
        var document = ModsynParser.Parse("mod { installThese: \"one.cs\" }");
        var validation = ModsynValidator.Validate(document);

        Assert.IsFalse(validation.IsValid);
        Assert.ThrowsException<ArgumentException>(() => ModsynConfigurationConverter.Convert(document, validation));
    }

    private static ModsynConvertedConfiguration Convert(string source)
    {
        var document = ModsynParser.Parse(source);
        var validation = ModsynValidator.Validate(document);
        Assert.IsTrue(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        return ModsynConfigurationConverter.Convert(document, validation);
    }
}