using GtaSaModManager.Controls.DeleteMods;
using GtaSaModManager.Models;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

/// <summary>
/// Step 5 "Delete Mods" window (port of Forms\DeleteSomeModUi\DeleteSomeModUi.html).
/// Used by the "Delete some mods" button of Step 1 and, later, by Mixed mods.
/// Images / GIFs / videos / README are always read from the MOD folder (SourcePackagePath), never from the game folder.
/// </summary>
public partial class MainForm
{
    private enum DeleteEntryKind
    {
        Recorded,          // has a record in <game>\.ModManager\installations.json
        UntrackedModLoader // a folder inside <game>\modloader without a record
    }

    private sealed record DeleteEntryInfo(
        DeleteEntryKind Kind,
        string ModId,
        string Type,
        string Destination,
        string SourcePackagePath);

    private sealed record MixedPartDeleteInfo(
        string ParentModId,
        string SourcePackagePath,
        InstallationManifestPart Part);

    private DeleteModsPanel? _deleteModsPanel;
    private bool _isDeleteModsStepActive;
    private WizardStep _deleteModsReturnStep = WizardStep.Step1;
    private string _deleteModsSavedReadme = string.Empty;
    private List<string> _deleteModsSavedImages = new();

    private Control CreateDeleteModsHost()
    {
        _deleteModsPanel = new DeleteModsPanel
        {
            Name = "DeleteModsPanel",
            Dock = DockStyle.Fill,
            Visible = true,
            RightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian
                ? RightToLeft.Yes
                : RightToLeft.No
        };
        _deleteModsPanel.BusyChanged += (_, _) => BeginInvoke(UpdateSidebarState);
        _deleteModsPanel.EntryRemoved += (_, _) => BeginInvoke(UpdateSidebarState);
        _deleteModsPanel.AllRemoved += (_, _) => BeginInvoke(CloseDeleteModsStep);
        var host = new Panel
        {
            Name = "DeleteModsHost",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(0x08, 0x09, 0x0B),
            Visible = false
        };
        host.Controls.Add(_deleteModsPanel);
        return host;
    }

    private DeleteModsTexts BuildDeleteModsTexts() => new()
    {
        Title = _localizationService.GetString("DeleteModsTitle", "Delete Mods"),
        IsRightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian,
        AreYouSure = _localizationService.GetString("DeleteModsAreYouSure", "Are you sure?"),
        Yes = _localizationService.GetString("DeleteModsYes", "Yes"),
        No = _localizationService.GetString("DeleteModsNo", "No"),
        NothingLeft = _localizationService.GetString("DeleteModsNothingLeft", "Nothing left to delete."),
        DeleteFailed = _localizationService.GetString("DeleteModsFailed", "The selected item could not be deleted."),
        NoPhotoNoText = _localizationService.GetString("DeleteModsNoPhotoNoText", "NO Photo and No Text for this Mod"),
        SoundOn = _localizationService.GetString("DeleteModsSoundOn", "🔊 Sound On"),
        SoundOff = _localizationService.GetString("DeleteModsSoundOff", "🔇 Sound Off")
    };

    /// <summary>Opens the Step 5 delete window with the given rows.</summary>
    private void ShowDeleteModsStep(
        IReadOnlyList<DeleteModEntry> entries,
        DeleteModHandler handler,
        WizardStep returnStep,
        bool clearSidebarMedia)
    {
        if (_deleteModsPanel == null || !_wizardPanels.TryGetValue(WizardStep.Step5, out var step5Panel))
        {
            return;
        }

        _deleteModsReturnStep = returnStep;
        if (clearSidebarMedia)
        {
            _deleteModsSavedReadme = _selectedReadmePath;
            _deleteModsSavedImages = _selectedImageFiles;
            _selectedReadmePath = string.Empty;
            _selectedImageFiles = new List<string>();
        }

        _isDeleteModsStepActive = true;
        _deleteModsPanel.Texts = BuildDeleteModsTexts();
        _deleteModsPanel.SetEntries(entries, handler);
        ApplyDeleteModsVisibility(step5Panel, show: true);
        GoToStep(WizardStep.Step5);
    }

    private void ApplyDeleteModsVisibility(Control step5Panel, bool show)
    {
        var host = step5Panel.Controls.Find("DeleteModsHost", true).FirstOrDefault();
        var assetLayout = step5Panel.Controls.Find("AssetStepLayout", true).FirstOrDefault();
        var saveMissionPanel = step5Panel.Controls.Find("SaveMissionSlotPanel", true).FirstOrDefault();
        if (host == null)
        {
            return;
        }

        host.Visible = show;
        if (show)
        {
            host.BringToFront();
            var optionalsHost = step5Panel.Controls.Find("OptionalsHost", true).FirstOrDefault();
            if (optionalsHost != null) optionalsHost.Visible = false;
            if (assetLayout != null) assetLayout.Visible = false;
            if (saveMissionPanel != null) saveMissionPanel.Visible = false;
        }
        else if (assetLayout != null && !_isSaveMissionStepActive && !_isOptionalsStepActive)
        {
            assetLayout.Visible = true;
        }
    }

    /// <summary>Leaves the delete window (Previous button).</summary>
    private async void CloseDeleteModsStep()
    {
        if (_deleteModsPanel?.IsBusy == true)
        {
            return;
        }

        _isDeleteModsStepActive = false;
        if (_wizardPanels.TryGetValue(WizardStep.Step5, out var step5Panel))
        {
            ApplyDeleteModsVisibility(step5Panel, show: false);
        }

        _selectedReadmePath = _deleteModsSavedReadme;
        _selectedImageFiles = _deleteModsSavedImages;
        _deleteModsSavedReadme = string.Empty;
        _deleteModsSavedImages = new List<string>();

        if (_deleteOptionalBaseRecordAfterSelection is { } baseRecord)
        {
            _deleteOptionalBaseRecordAfterSelection = null;
            if (!_deleteOptionalRowsRemoved)
            {
                RefreshModList();
                NavigateToStep(WizardStep.Step6);
                return;
            }

            if (await DeleteInstallationRecordAsync(baseRecord))
            {
                _deleteOptionalRowsRemoved = false;
                FinishExistingModDeletion();
            }
            else
            {
                _deleteOptionalRowsRemoved = false;
                RefreshModList();
                NavigateToStep(WizardStep.Step6);
            }

            return;
        }

        if (_deleteOptionalRowsRemoved)
        {
            _deleteOptionalRowsRemoved = false;
            FinishOptionalChildrenDeletion();
            return;
        }

        RefreshModList();
        NavigateToStep(_deleteModsReturnStep);
    }

    // ------------------------------------------------------------------
    // Step 1: "Delete some mods"
    // ------------------------------------------------------------------

    private void RefreshStep1DeleteButton()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step1, out var panel))
        {
            return;
        }

        if (panel.Controls.Find("Step1DeleteSomeModsButton", true).FirstOrDefault() is Button button)
        {
            button.Text = _localizationService.GetString("DeleteSomeMods", "Delete some mods");
            button.Visible = GameService.IsValidGameFolder(_selectedGamePath);
        }
    }

    private async void OpenDeleteSomeModsFromStep1()
    {
        try
        {
            if (!GameService.IsValidGameFolder(_selectedGamePath))
            {
                return;
            }

            var entries = await Task.Run(() => BuildInstalledModDeleteEntries(_selectedGamePath));
            if (entries.Count == 0)
            {
                MessageBox.Show(
                    _localizationService.GetString("DeleteModsNoInstalled", "No installed mods were found."),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ShowDeleteModsStep(entries, DeleteInstalledEntryAsync, WizardStep.Step1, clearSidebarMedia: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private List<DeleteModEntry> BuildInstalledModDeleteEntries(string gamePath)
    {
        var entries = new List<DeleteModEntry>();
        var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gamePath);
        var manifest = ModLoaderService.LoadInstallationManifest(manifestPath);
        var coveredDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in manifest.Entries
                     .GroupBy(item => item.ModId, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.Last())
                     .OrderBy(item => item.ModId, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(record.Type, "mixed", StringComparison.OrdinalIgnoreCase)
                && record.MixedParts.Count > 0)
            {
                foreach (var part in record.MixedParts)
                {
                    entries.Add(CreateMixedDeleteEntry(record, part));
                }

                if (!string.IsNullOrWhiteSpace(record.InstalledDestination))
                {
                    coveredDestinations.Add(Path.TrimEndingDirectorySeparator(record.InstalledDestination));
                }

                continue;
            }

            var files = (record.InstalledFiles ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0 && IsStrictModLoaderSubfolder(gamePath, record.InstalledDestination)
                && Directory.Exists(record.InstalledDestination))
            {
                files = Directory.GetFiles(record.InstalledDestination, "*", SearchOption.AllDirectories).ToList();
            }

            if (!string.IsNullOrWhiteSpace(record.InstalledDestination))
            {
                coveredDestinations.Add(Path.TrimEndingDirectorySeparator(record.InstalledDestination));
            }

            entries.Add(CreateDeleteEntry(
                record.ModId,
                files,
                new DeleteEntryInfo(DeleteEntryKind.Recorded, record.ModId, record.Type, record.InstalledDestination, record.SourcePackagePath)));
        }

        var modLoaderRoot = GameService.GetModLoaderFolder(gamePath);
        if (Directory.Exists(modLoaderRoot))
        {
            foreach (var directory in Directory.GetDirectories(modLoaderRoot)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (coveredDestinations.Contains(Path.TrimEndingDirectorySeparator(directory)))
                {
                    continue;
                }

                var name = Path.GetFileName(directory);
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToList();
                // No mod folder is known for an untracked folder, so there is no media to show.
                entries.Add(CreateDeleteEntry(
                    name,
                    files,
                    new DeleteEntryInfo(DeleteEntryKind.UntrackedModLoader, name, "putinmodloader", directory, string.Empty)));
            }
        }

        return entries;
    }

    private DeleteModEntry CreateDeleteEntry(string name, List<string> files, DeleteEntryInfo info)
    {
        var entry = new DeleteModEntry { Name = name, Files = files, Tag = info };
        var source = info.SourcePackagePath;
        if (!string.IsNullOrWhiteSpace(source) && Directory.Exists(source))
        {
            entry.MediaFiles = FindImageFiles(source);
            var readme = FindReadmeFile(source);
            entry.Description = string.IsNullOrWhiteSpace(readme) ? string.Empty : TryReadTextFile(readme);
        }

        return entry;
    }

    private static List<DeleteModEntry> BuildMixedDeleteEntries(InstallationManifestEntry installation)
    {
        return installation.MixedParts
            .Select(part => CreateMixedDeleteEntry(installation, part))
            .ToList();
    }

    private static DeleteModEntry CreateMixedDeleteEntry(
        InstallationManifestEntry installation,
        InstallationManifestPart part)
    {
        var entry = new DeleteModEntry
        {
            Name = part.Name,
            Files = (part.InstalledFiles ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Tag = new MixedPartDeleteInfo(installation.ModId, installation.SourcePackagePath, part)
        };
        var sourceRoot = installation.SourcePackagePath;
        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            return entry;
        }

        var partRoot = Path.GetFullPath(Path.Combine(sourceRoot, part.Name.Replace('\\', Path.DirectorySeparatorChar)));
        var rootPrefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceRoot)) + Path.DirectorySeparatorChar;
        if (!partRoot.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(partRoot))
        {
            return entry;
        }

        entry.MediaFiles = FindImageFiles(partRoot);
        var readme = FindReadmeFile(partRoot);
        entry.Description = string.IsNullOrWhiteSpace(readme) ? string.Empty : SanitizeMarkdownReadme(File.ReadAllText(readme));
        return entry;
    }

    // ------------------------------------------------------------------
    // Deleting one row
    // ------------------------------------------------------------------

    private static string GetUserFilesRootPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "GTA San Andreas User Files");

    private static bool IsInsideRoot(string root, string path)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStrictModLoaderSubfolder(string gamePath, string destination)
    {
        return !string.IsNullOrWhiteSpace(destination)
            && IsInsideRoot(GameService.GetModLoaderFolder(gamePath), destination);
    }

    private async Task<bool> DeleteInstalledEntryAsync(DeleteModEntry entry, IProgress<int> progress)
    {
        if (entry.Tag is MixedPartDeleteInfo mixedPart)
        {
            return await DeleteMixedPartAsync(entry, progress);
        }

        if (entry.Tag is not DeleteEntryInfo info)
        {
            return false;
        }

        var gamePath = _selectedGamePath;
        var type = (info.Type ?? string.Empty).Trim().ToLowerInvariant();
        var done = 0;

        if (info.Kind == DeleteEntryKind.Recorded && type is "replacing" or "putandreplace" or "putandreplaces" or "putingamefolder")
        {
            // Originals must be restored from the backups; delegate to the restore logic.
            return await RestoreReplacementForDeleteAsync(info, entry, progress);
        }

        foreach (var file in entry.Files)
        {
            if (!IsInsideRoot(gamePath, file) && !IsInsideRoot(GetUserFilesRootPath(), file))
            {
                continue; // never touch anything outside the game folder / user files
            }

            if (File.Exists(file))
            {
                await Task.Run(() => File.Delete(file));
            }

            progress.Report(++done);
            await Task.Yield();
        }

        PruneEmptyParentDirectories(entry.Files, gamePath);

        if (type == "missiondsl")
        {
            ModLoaderService.ClearDirectoryContents(Path.Combine(GetUserFilesRootPath(), "DSL"));
        }

        var destination = info.Destination;
        if (IsStrictModLoaderSubfolder(gamePath, destination) && Directory.Exists(destination))
        {
            await Task.Run(() => Directory.Delete(destination, true));
        }
        else if (!string.IsNullOrWhiteSpace(destination) && Directory.Exists(destination))
        {
            PruneEmptyDirectories(destination, gamePath);
        }

        if (info.Kind == DeleteEntryKind.Recorded)
        {
            RemoveInstallationRecord(gamePath, info);
        }

        ModLoaderService.RemoveInstalledRecord(ModPackageService.NormalizeDisplayName(info.ModId), destination);
        return true;
    }

    private async Task<bool> DeleteMixedPartAsync(DeleteModEntry entry, IProgress<int> progress)
    {
        if (entry.Tag is not MixedPartDeleteInfo mixedInfo)
        {
            return false;
        }

        var part = mixedInfo.Part;
        var partInfo = new DeleteEntryInfo(
            DeleteEntryKind.Recorded,
            part.ModId,
            part.Type,
            part.InstalledDestination,
            mixedInfo.SourcePackagePath);
        var type = part.Type.Trim().ToLowerInvariant();
        if (type is "replacing" or "putandreplace" or "putandreplaces" or "putingamefolder")
        {
            if (!await RestoreReplacementForDeleteAsync(partInfo, entry, progress))
            {
                return false;
            }
        }
        else
        {
            var done = 0;
            foreach (var file in entry.Files)
            {
                if ((IsInsideRoot(_selectedGamePath, file) || IsInsideRoot(GetUserFilesRootPath(), file)) && File.Exists(file))
                {
                    await Task.Run(() => File.Delete(file));
                }

                progress.Report(++done);
                await Task.Yield();
            }

            if (Directory.Exists(part.InstalledDestination))
            {
                PruneEmptyDirectories(part.InstalledDestination, _selectedGamePath);
            }

            if (IsStrictModLoaderSubfolder(_selectedGamePath, part.InstalledDestination)
                && Path.GetDirectoryName(part.InstalledDestination) is { } partParentDirectory
                && Directory.Exists(partParentDirectory))
            {
                PruneEmptyDirectories(partParentDirectory, _selectedGamePath);
            }
        }

        if (!ModLoaderService.RemoveMixedInstallationPart(
                _selectedGamePath,
                mixedInfo.ParentModId,
                part.ModId))
        {
            return true;
        }

        var remainingParts = ModLoaderService.LoadInstallationManifest(
                ModLoaderService.GetGameInstallationsManifestPath(_selectedGamePath))
            .Entries.LastOrDefault(item =>
                string.Equals(item.ModId, mixedInfo.ParentModId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Type, "mixed", StringComparison.OrdinalIgnoreCase))?
            .MixedParts.Count ?? 0;
        if (remainingParts == 0)
        {
            ModLoaderService.RemoveInstalledRecord(mixedInfo.ParentModId);
        }

        return true;
    }

    /// <summary>
    /// Removes the folders that became empty after deleting <paramref name="files"/>, but never the game folder,
    /// the user files folder or their direct sub folders (modloader, cleo, ...).
    /// </summary>
    private static void PruneEmptyParentDirectories(IEnumerable<string> files, string gamePath)
    {
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gamePath));
        var userFilesRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(GetUserFilesRootPath()));
        foreach (var directory in files
                     .Select(file => Path.GetDirectoryName(Path.GetFullPath(file)))
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(path => path!.Length))
        {
            var current = directory;
            while (!string.IsNullOrWhiteSpace(current)
                   && (IsInsideRoot(gameRoot, current) || IsInsideRoot(userFilesRoot, current))
                   && Directory.Exists(current)
                   && !Directory.EnumerateFileSystemEntries(current).Any())
            {
                var parent = Path.GetDirectoryName(current);
                var parentIsRoot = string.Equals(parent, gameRoot, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parent, userFilesRoot, StringComparison.OrdinalIgnoreCase);
                if (parentIsRoot)
                {
                    break; // keep top level folders such as modloader or cleo
                }

                Directory.Delete(current);
                current = parent;
            }
        }
    }

    private static void PruneEmptyDirectories(string destination, string gamePath)
    {
        var fullDestination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gamePath));
        var userFiles = Path.TrimEndingDirectorySeparator(Path.GetFullPath(GetUserFilesRootPath()));
        // Never remove the game folder or the user files folder themselves.
        if (string.Equals(fullDestination, gameRoot, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullDestination, userFiles, StringComparison.OrdinalIgnoreCase)
            || !(IsInsideRoot(gameRoot, fullDestination) || IsInsideRoot(userFiles, fullDestination)))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(fullDestination, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length)
                     .Append(fullDestination))
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
    }

    private static void RemoveInstallationRecord(string gamePath, DeleteEntryInfo info)
    {
        var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(gamePath);
        var manifest = ModLoaderService.LoadInstallationManifest(manifestPath);
        manifest.Entries.RemoveAll(item =>
            string.Equals(item.ModId, info.ModId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Type, info.Type, StringComparison.OrdinalIgnoreCase));
        ModLoaderService.SaveInstallationManifest(manifestPath, manifest);
    }

    /// <summary>Same restore logic as the Step 4 uninstall, without driving the Step 4 UI.</summary>
    private async Task<bool> RestoreReplacementForDeleteAsync(DeleteEntryInfo info, DeleteModEntry entry, IProgress<int> progress)
    {
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_selectedGamePath));
        var gameRootPrefix = gameRoot + Path.DirectorySeparatorChar;
        var modName = info.ModId;
        var done = 0;
        var records = ModPackageService.LoadReplacementRecords()
            .Where(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(record.GameFolder)
                && string.Equals(Path.GetFullPath(record.GameFolder), gameRoot, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var backupDirectories = BackupStorageService.GetModBackupDirectories(gameRoot, modName, records);
        var backupOperations = backupDirectories
            .SelectMany(directory => Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(source => (Source: source, Destination: Path.GetFullPath(Path.Combine(gameRoot, Path.GetRelativePath(directory, source))))))
            .Where(operation => operation.Destination.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(operation => operation.Destination, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var restoredPaths = backupOperations.Select(operation => operation.Destination)
            .Concat(records.Where(record => !string.IsNullOrWhiteSpace(record.OriginalFilePath)).Select(record => Path.GetFullPath(record.OriginalFilePath)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 1) delete installed files that did not replace an original
        foreach (var file in entry.Files.Select(Path.GetFullPath).Where(path => !restoredPaths.Contains(path)))
        {
            if (file.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase) && File.Exists(file))
            {
                await Task.Run(() => File.Delete(file));
            }

            progress.Report(++done);
            await Task.Yield();
        }

        PruneEmptyParentDirectories(entry.Files, _selectedGamePath);

        // 2) copy the backed up originals back
        foreach (var operation in backupOperations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(operation.Destination)!);
            await Task.Run(() => File.Copy(operation.Source, operation.Destination, true));
            progress.Report(++done);
            await Task.Yield();
        }

        // 3) history records that have no backup directory entry
        foreach (var record in records.Where(record => !string.IsNullOrWhiteSpace(record.OriginalFilePath)
                     && record.OriginalFilePath.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase)
                     && !backupOperations.Any(operation => string.Equals(operation.Destination, Path.GetFullPath(record.OriginalFilePath), StringComparison.OrdinalIgnoreCase))))
        {
            if (File.Exists(record.BackupFilePath))
            {
                await Task.Run(() => File.Copy(record.BackupFilePath, record.OriginalFilePath, true));
            }
            else if (File.Exists(record.OriginalFilePath))
            {
                await Task.Run(() => File.Delete(record.OriginalFilePath));
            }

            progress.Report(++done);
            await Task.Yield();
        }

        foreach (var directory in backupDirectories.Where(Directory.Exists))
        {
            Directory.Delete(directory, recursive: true);
        }

        var remaining = ModPackageService.LoadReplacementRecords();
        remaining.RemoveAll(record => string.Equals(record.ModName, modName, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(record.GameFolder)
            && string.Equals(Path.GetFullPath(record.GameFolder), gameRoot, StringComparison.OrdinalIgnoreCase));
        ModPackageService.SaveReplacementRecords(remaining);

        if (!string.IsNullOrWhiteSpace(info.Destination) && Directory.Exists(info.Destination))
        {
            PruneEmptyDirectories(info.Destination, _selectedGamePath);
        }

        RemoveInstallationRecord(_selectedGamePath, info);
        return true;
    }
}
