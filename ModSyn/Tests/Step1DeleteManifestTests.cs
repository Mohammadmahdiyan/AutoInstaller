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
}
