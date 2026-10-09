using GtaSaModManager.Controls.DeleteMods;
using GtaSaModManager.Controls.Optionals;
using GtaSaModManager.Models;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

/// <summary>
/// optional / (optional)   : ONE extra package inside the mod folder, installed from Step 6.
/// optionals / (optionals) : a folder of several extra packages, picked one by one in a Step 5 window.
/// Optional packages are recorded in installations.json with ParentModId + OptionalKind so they can be removed later.
/// </summary>
public partial class MainForm
{
    private const string OptionalKindSingle = "optional";
    private const string OptionalKindMany = "optionals";

    private OptionalsPickerPanel? _optionalsPanel;
    private bool _isOptionalsStepActive;
    private bool _optionalInstallBusy;
    private string _optionalsParentName = string.Empty;
    private InstallationManifestEntry? _deleteOptionalBaseRecordAfterSelection;
    private bool _deleteOptionalRowsRemoved;

    // ------------------------------------------------------------------
    // Detection
    // ------------------------------------------------------------------

    private string? GetOptionalFolderForCurrentInstall() => ModPackageService.FindOptionalFolder(_selectedModPackageRoot);

    private string? GetOptionalsFolderForCurrentInstall() => ModPackageService.FindOptionalsFolder(_selectedModPackageRoot);

    private bool HasOptionalContentForCurrentInstall()
    {
        return GetOptionalFolderForCurrentInstall() != null || GetOptionalsFolderForCurrentInstall() != null;
    }

    // ------------------------------------------------------------------
    // Step 5 host for the "optionals" window
    // ------------------------------------------------------------------

    private Control CreateOptionalsHost()
    {
        _optionalsPanel = new OptionalsPickerPanel
        {
            Name = "OptionalsPanel",
            Dock = DockStyle.Fill
        };
        _optionalsPanel.InstallRequested += async (_, item) => await InstallOptionalsItemAsync(item);
        _optionalsPanel.FinishRequested += (_, _) => FinishOptionalsStep();
        var host = new Panel
        {
            Name = "OptionalsHost",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(0x08, 0x09, 0x0B),
            Visible = false
        };
        host.Controls.Add(_optionalsPanel);
        return host;
    }

    private void ApplyOptionalsVisibility(Control step5Panel, bool show)
    {
        var host = step5Panel.Controls.Find("OptionalsHost", true).FirstOrDefault();
        var deleteHost = step5Panel.Controls.Find("DeleteModsHost", true).FirstOrDefault();
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
            if (deleteHost != null) deleteHost.Visible = false;
            if (assetLayout != null) assetLayout.Visible = false;
            if (saveMissionPanel != null) saveMissionPanel.Visible = false;
        }
        else if (assetLayout != null && !_isSaveMissionStepActive && !_isDeleteModsStepActive)
        {
            assetLayout.Visible = true;
        }
    }

    private OptionalsTexts BuildOptionalsTexts() => new()
    {
        IsRightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian,
        Title = _localizationService.GetString("OptionalsTitle", "Optional mods"),
        Hint = _localizationService.GetString("OptionalsHint", "Click a mod to install it. Right-click it to see its details."),
        Install = _localizationService.GetString("Install", "Install"),
        Installed = _localizationService.GetString("OptionalsInstalled", "Installed"),
        Finish = _localizationService.GetString("OptionalsFinish", "Finish"),
        Empty = _localizationService.GetString("OptionalsEmpty", "There are no optional mods in this package.")
    };

    // ------------------------------------------------------------------
    // Step 6 buttons
    // ------------------------------------------------------------------

    private void RefreshStep6OptionalActions()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step6, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var host = panel.Controls.Find("OptionalInstallHost", true).OfType<FlowLayoutPanel>().FirstOrDefault();
        if (host == null)
        {
            return;
        }

        foreach (var control in host.Controls.Cast<Control>().ToList())
        {
            control.Dispose();
        }

        host.Controls.Clear();
        host.Visible = false;
        if (_lastActionWasDelete
            || ModLoaderService.FindBaseInstallationBySource(_selectedGamePath, _selectedModPackageRoot) is null)
        {
            return;
        }

        var optionalFolder = GetOptionalFolderForCurrentInstall();
        var optionalsFolder = GetOptionalsFolderForCurrentInstall();
        if (optionalFolder != null)
        {
            var optionalName = GetOptionalPackageDisplayName(optionalFolder, _selectedModName);
            var button = CreateOptionalActionButton(
                string.Format(
                    _localizationService.GetString("InstallOptionalButton", "Install optional: {0}"),
                    optionalName),
                optionalFolder);
            button.Click += async (_, _) => await InstallOptionalPackageAsync(optionalFolder);
            host.Controls.Add(button);
        }

        if (optionalsFolder != null && ModPackageService.GetOptionalsPackageFolders(optionalsFolder).Count > 0)
        {
            var button = CreateOptionalActionButton(
                _localizationService.GetString("InstallOptionalsButton", "Install optionals"),
                optionalsFolder);
            button.Click += async (_, _) => await ShowOptionalsStepAsync();
            host.Controls.Add(button);
        }

        host.Visible = host.Controls.Count > 0;
        host.AutoSize = true;
    }

    private static string GetOptionalPackageDisplayName(string optionalRoot, string parentModName)
    {
        if (!string.IsNullOrWhiteSpace(optionalRoot) && Directory.Exists(optionalRoot))
        {
            var manifest = ModPackageService.ResolveManifest(optionalRoot);
            var payloadPath = ModPackageService.GetInstallPayloadDirectory(optionalRoot, manifest);
            if (!string.IsNullOrWhiteSpace(payloadPath)
                && Directory.Exists(payloadPath)
                && !string.Equals(Path.GetFullPath(payloadPath), Path.GetFullPath(optionalRoot), StringComparison.OrdinalIgnoreCase))
            {
                var payloadName = Path.GetFileName(Path.TrimEndingDirectorySeparator(payloadPath));
                if (!string.IsNullOrWhiteSpace(payloadName))
                {
                    return ModPackageService.NormalizeDisplayName(payloadName);
                }
            }
        }

        return ModPackageService.NormalizeDisplayName(parentModName);
    }

    private Button CreateOptionalActionButton(string text, string toolTipPath)
    {
        var button = new Button
        {
            Text = text,
            Width = 210,
            Height = 42,
            Margin = new Padding(0, 0, 0, 8)
        };
        new ToolTip().SetToolTip(button, toolTipPath);
        return button;
    }

    // ------------------------------------------------------------------
    // optional (single package)
    // ------------------------------------------------------------------

    private async Task InstallOptionalPackageAsync(string optionalRoot)
    {
        if (_optionalInstallBusy)
        {
            return;
        }

        _optionalInstallBusy = true;
        try
        {
            var parentName = _selectedModName;
            var installName = parentName + " (optional)";
            await RunOptionalInstallAsync(optionalRoot, installName, parentName, OptionalKindSingle);
            _lastActionWasDelete = false;
            RefreshModList();
            GoToStep(WizardStep.Step6);
        }
        finally
        {
            _optionalInstallBusy = false;
        }
    }

    // ------------------------------------------------------------------
    // optionals (many packages, Step 5 window)
    // ------------------------------------------------------------------

    private async Task ShowOptionalsStepAsync()
    {
        var optionalsFolder = GetOptionalsFolderForCurrentInstall();
        if (optionalsFolder == null || _optionalsPanel == null
            || !_wizardPanels.TryGetValue(WizardStep.Step5, out var step5Panel))
        {
            return;
        }

        var parentName = _selectedModName;
        var gamePath = _selectedGamePath;
        var items = await Task.Run(() => BuildOptionalsItems(optionalsFolder, parentName, gamePath));
        _optionalsParentName = parentName;
        _optionalsPanel.Texts = BuildOptionalsTexts();
        _optionalsPanel.PreviewTexts = BuildDeleteModsTexts();
        _optionalsPanel.SetItems(items);
        _isOptionalsStepActive = true;
        ApplyOptionalsVisibility(step5Panel, show: true);
        GoToStep(WizardStep.Step5);
    }

    private List<OptionalItem> BuildOptionalsItems(string optionalsFolder, string parentModName, string gamePath)
    {
        var installed = ModLoaderService.FindOptionalInstallations(gamePath, parentModName, OptionalKindMany);
        var items = new List<OptionalItem>();
        foreach (var folder in ModPackageService.GetOptionalsPackageFolders(optionalsFolder))
        {
            var entry = new DeleteModEntry
            {
                Name = Path.GetFileName(folder),
                MediaFiles = FindImageFiles(folder)
            };
            var readme = FindReadmeFile(folder);
            entry.Description = string.IsNullOrWhiteSpace(readme) ? string.Empty : TryReadTextFile(readme);
            var fullFolder = Path.GetFullPath(folder);
            items.Add(new OptionalItem
            {
                Key = folder,
                Entry = entry,
                Installed = installed.Any(record =>
                    !string.IsNullOrWhiteSpace(record.SourcePackagePath)
                    && string.Equals(Path.GetFullPath(record.SourcePackagePath), fullFolder, StringComparison.OrdinalIgnoreCase))
            });
        }

        return items;
    }

    private async Task InstallOptionalsItemAsync(OptionalItem item)
    {
        if (_optionalInstallBusy || _optionalsPanel == null)
        {
            return;
        }

        _optionalInstallBusy = true;
        try
        {
            var installed = await RunOptionalInstallAsync(
                item.Key,
                item.Entry.Name,
                _optionalsParentName,
                OptionalKindMany);
            if (installed)
            {
                _optionalsPanel.SetInstalled(item.Key, true);
                RefreshModList();
            }

            if (_optionalsPanel.AllInstalled)
            {
                FinishOptionalsStep();
                return;
            }

            GoToStep(WizardStep.Step5);
        }
        finally
        {
            _optionalInstallBusy = false;
        }
    }

    private void FinishOptionalsStep()
    {
        _isOptionalsStepActive = false;
        if (_wizardPanels.TryGetValue(WizardStep.Step5, out var step5Panel))
        {
            ApplyOptionalsVisibility(step5Panel, show: false);
        }

        _lastActionWasDelete = false;
        GoToStep(WizardStep.Step6);
    }

    // ------------------------------------------------------------------
    // Shared installer for one optional package
    // ------------------------------------------------------------------

    /// <summary>
    /// Installs the package in <paramref name="optionalRoot"/> through the normal install pipeline (Step 4) and
    /// links its installation record to the base mod. Returns true when it was installed.
    /// </summary>
    private async Task<bool> RunOptionalInstallAsync(string optionalRoot, string installName, string parentName, string kind)
    {
        if (string.IsNullOrWhiteSpace(optionalRoot) || !Directory.Exists(optionalRoot))
        {
            return false;
        }

        var manifest = ModPackageService.ResolveManifest(optionalRoot);
        var assetReplacement = await TryInstallOptionalAssetReplacementAsync(
            optionalRoot,
            installName,
            parentName,
            kind,
            manifest);
        if (assetReplacement.HasValue)
        {
            return assetReplacement.Value;
        }

        if (manifest.SupportsAssetSelection
            || manifest.IsMixedPackage
            || manifest.NormalizedType is "saveandmission" or "savesandmissions")
        {
            MessageBox.Show(
                _localizationService.GetString(
                    "OptionalUnsupportedType",
                    "This optional package uses a type that needs its own selection steps (VSW/VSS, SAM/SMS or Mixed) and can't be installed as an optional yet."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        var previousName = _selectedModName;
        var previousPayload = _selectedModPayloadPath;
        var previousRoot = _selectedModPackageRoot;
        var previousManifest = _selectedModManifest;
        var previousAsset = _selectedAssetForInstall;
        var previousReadme = _selectedReadmePath;
        var previousImages = _selectedImageFiles;
        var gamePath = _selectedGamePath;

        _completionTimer.Stop();
        _completionTimerActive = false;
        _selectedModName = installName;
        _selectedModPackageRoot = optionalRoot;
        _selectedModManifest = manifest;
        _selectedModPayloadPath = ModPackageService.GetInstallPayloadDirectory(optionalRoot, manifest);
        if (string.IsNullOrWhiteSpace(_selectedModPayloadPath) || !Directory.Exists(_selectedModPayloadPath))
        {
            _selectedModPayloadPath = optionalRoot;
        }

        _selectedAssetForInstall = null;
        _isInstallingOptionalPackage = true;
        try
        {
            GoToStep(WizardStep.Step4);
            await InstallSelectedModAsync();
        }
        finally
        {
            _isInstallingOptionalPackage = false;
            _selectedModName = previousName;
            _selectedModPayloadPath = previousPayload;
            _selectedModPackageRoot = previousRoot;
            _selectedModManifest = previousManifest;
            _selectedAssetForInstall = previousAsset;
            _selectedReadmePath = previousReadme;
            _selectedImageFiles = previousImages;
        }

        var tagged = ModLoaderService.TagOptionalInstallation(gamePath, optionalRoot, parentName, kind);
        if (tagged)
        {
            await Task.Delay(900); // let the person see the Step 4 completion state
        }

        return tagged;
    }

    private async Task<bool?> TryInstallOptionalAssetReplacementAsync(
        string optionalRoot,
        string optionalName,
        string parentName,
        string optionalKind,
        ModManifest optionalManifest)
    {
        var optionalPayload = ModPackageService.GetInstallPayloadDirectory(optionalRoot, optionalManifest);
        if (string.IsNullOrWhiteSpace(optionalPayload) || !Directory.Exists(optionalPayload))
        {
            optionalPayload = optionalRoot;
        }

        var hasModelFiles = Directory.GetFiles(optionalPayload, "*", SearchOption.AllDirectories)
            .Any(IsModelFile);
        if (!hasModelFiles)
        {
            return null;
        }

        var parentUsesAssetSelection = _selectedModManifest?.SupportsAssetSelection == true
            || _selectedModManifest?.MixedParts.Any(part => part.Manifest.SupportsAssetSelection) == true;
        var parentRecord = ModLoaderService.FindBaseInstallationBySource(_selectedGamePath, _selectedModPackageRoot);
        if (parentRecord is null)
        {
            if (parentUsesAssetSelection)
            {
                MessageBox.Show(
                    _localizationService.GetString(
                        "OptionalAssetMappingMissing",
                        "The installed asset mapping is unavailable. Reinstall the parent mod before installing this Optional package."),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return null;
        }

        var targets = new List<(string Destination, List<string> Files, List<InstallationAssetMapping> Mappings)>();
        if (string.Equals(parentRecord.Type, "mixed", StringComparison.OrdinalIgnoreCase))
        {
            targets.AddRange(parentRecord.MixedParts
                .Where(part => part.AssetMappings is { Count: > 0 })
                .Select(part => (part.InstalledDestination, part.InstalledFiles, part.AssetMappings)));
        }
        else if (parentRecord.AssetMappings is { Count: > 0 })
        {
            targets.Add((parentRecord.InstalledDestination, parentRecord.InstalledFiles, parentRecord.AssetMappings));
        }

        if (targets.Count == 0)
        {
            if (parentUsesAssetSelection)
            {
                MessageBox.Show(
                    _localizationService.GetString(
                        "OptionalAssetMappingMissing",
                        "The installed asset mapping is unavailable. Reinstall the parent mod before installing this Optional package."),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return null;
        }

        var matchingTargets = new List<(
            string Destination,
            List<InstallationAssetMapping> Mappings,
            IReadOnlyList<OptionalAssetInstallFile> Files)>();
        foreach (var target in targets)
        {
            try
            {
                var files = OptionalAssetReplacementService.ResolveInstallFiles(
                    optionalPayload,
                    target.Destination,
                    target.Files,
                    target.Mappings);
                if (files.Count > 0)
                {
                    matchingTargets.Add((target.Destination, target.Mappings, files));
                }
            }
            catch (InvalidDataException)
            {
                // Another asset-bearing MIX part may be the target for this Optional package.
            }
        }

        if (matchingTargets.Count == 0)
        {
            if (parentUsesAssetSelection || optionalManifest.SupportsAssetSelection)
            {
                MessageBox.Show(
                    _localizationService.GetString(
                        "OptionalAssetNoMatch",
                        "The Optional package's model files do not match an asset installed by the parent mod."),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return null;
        }

        if (matchingTargets.Count > 1)
        {
            MessageBox.Show(
                _localizationService.GetString(
                    "OptionalAssetAmbiguous",
                    "The Optional package matches more than one installed asset. It cannot be replaced safely."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        var match = matchingTargets[0];
        var existingOptional = ModLoaderService.FindOptionalInstallations(_selectedGamePath, parentRecord.ModId, optionalKind)
            .LastOrDefault(entry => !string.IsNullOrWhiteSpace(entry.SourcePackagePath)
                && string.Equals(Path.GetFullPath(entry.SourcePackagePath), Path.GetFullPath(optionalRoot), StringComparison.OrdinalIgnoreCase));
        var existingAssetOptional = ModLoaderService.FindOptionalInstallations(_selectedGamePath, parentRecord.ModId, optionalKind)
            .LastOrDefault(entry => string.Equals(entry.Type, "optionalassetreplacement", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entry.SourcePackagePath)
                && string.Equals(Path.GetFullPath(entry.SourcePackagePath), Path.GetFullPath(optionalRoot), StringComparison.OrdinalIgnoreCase));

        var currentDestinations = match.Files
            .Select(file => Path.GetFullPath(file.DestinationPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingAssetOptionals = ModLoaderService.FindOptionalInstallations(_selectedGamePath, parentRecord.ModId)
            .Where(entry => string.Equals(entry.Type, "optionalassetreplacement", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var previous in existingAssetOptionals)
        {
            var isSamePackage = !string.IsNullOrWhiteSpace(previous.SourcePackagePath)
                && string.Equals(Path.GetFullPath(previous.SourcePackagePath), Path.GetFullPath(optionalRoot), StringComparison.OrdinalIgnoreCase)
                && string.Equals(previous.OptionalKind, optionalKind, StringComparison.OrdinalIgnoreCase);
            var previousDestinations = (previous.InstalledFiles ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var shouldRestorePrevious = OptionalAssetReplacementService.ShouldRestorePreviousInstallation(
                previous.SourcePackagePath,
                previous.OptionalKind,
                previousDestinations,
                optionalRoot,
                optionalKind,
                currentDestinations);
            if (!shouldRestorePrevious)
            {
                continue;
            }

            await OptionalAssetReplacementService.RestoreAsync(
                _selectedGamePath,
                previous.OptionalAssetBackups ?? new List<OptionalAssetBackup>());
            ModLoaderService.RemoveGameInstallationRecord(_selectedGamePath, previous.ModId);
            if (!string.IsNullOrWhiteSpace(existingAssetOptional?.ModId)
                && string.Equals(previous.ModId, existingAssetOptional.ModId, StringComparison.OrdinalIgnoreCase))
            {
                existingAssetOptional = null;
            }
        }

        var previousBackups = existingAssetOptional?.OptionalAssetBackups ?? new List<OptionalAssetBackup>();
        var gamePath = _selectedGamePath;
        GoToStep(WizardStep.Step4);
        BeginStep4Progress(
            string.Format(_localizationService.GetString("Installing", "Installing") + " {0}", optionalName),
            match.Files.Select(file => (file.SourcePath, Path.GetRelativePath(gamePath, file.DestinationPath))).ToList());

        try
        {
            var backups = await OptionalAssetReplacementService.InstallAsync(
                gamePath,
                parentRecord.ModId,
                match.Files,
                previousBackups);
            ModLoaderService.RecordOptionalAssetReplacementInstallation(
                gamePath,
                parentRecord.ModId,
                optionalRoot,
                optionalName,
                optionalKind,
                match.Destination,
                match.Files.Select(file => file.DestinationPath),
                match.Mappings,
                backups);

            for (var index = 0; index < match.Files.Count; index++)
            {
                AddCompletedStep4File(
                    Path.GetRelativePath(gamePath, match.Files[index].DestinationPath),
                    index + 1,
                    match.Files.Count);
            }

            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
            RemoveLegacyOptionalInstall(optionalRoot, parentRecord.ModId, optionalKind, match.Destination);
            RefreshModList();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(
                _localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message,
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            GoToStep(WizardStep.Step6);
            return false;
        }
    }

    private void RemoveLegacyOptionalInstall(
        string optionalRoot,
        string parentModId,
        string optionalKind,
        string assetDestination)
    {
        var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(_selectedGamePath);
        var manifest = ModLoaderService.LoadInstallationManifest(manifestPath);
        var fullOptionalRoot = Path.GetFullPath(optionalRoot);
        var legacyEntries = manifest.Entries
            .Where(entry => !string.Equals(entry.Type, "optionalassetreplacement", StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.ParentModId, parentModId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.OptionalKind, optionalKind, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entry.SourcePackagePath)
                && string.Equals(Path.GetFullPath(entry.SourcePackagePath), fullOptionalRoot, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var changed = false;
        foreach (var previous in legacyEntries)
        {
            if (string.IsNullOrWhiteSpace(previous.InstalledDestination)
                || !IsStrictModLoaderSubfolder(_selectedGamePath, previous.InstalledDestination)
                || string.Equals(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(previous.InstalledDestination)),
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(assetDestination)),
                    StringComparison.OrdinalIgnoreCase)
                || !Directory.Exists(previous.InstalledDestination))
            {
                continue;
            }

            var previousRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(previous.InstalledDestination))
                + Path.DirectorySeparatorChar;
            var removedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in previous.InstalledFiles ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(file);
                if (fullPath.StartsWith(previousRoot, StringComparison.OrdinalIgnoreCase)
                    && IsModelFile(fullPath)
                    && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    removedModels.Add(fullPath);
                }
            }

            if (removedModels.Count == 0)
            {
                continue;
            }

            previous.InstalledFiles = (previous.InstalledFiles ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path)
                    && !removedModels.Contains(Path.GetFullPath(path))
                    && File.Exists(path))
                .ToList();
            changed = true;
            if (previous.InstalledFiles.Count == 0)
            {
                manifest.Entries.Remove(previous);
                ModLoaderService.RemoveInstalledRecord(previous.ModId, previous.InstalledDestination);
            }

            PruneEmptyDirectories(previous.InstalledDestination, _selectedGamePath);
        }

        if (changed)
        {
            ModLoaderService.SaveInstallationManifest(manifestPath, manifest);
        }
    }

    // ------------------------------------------------------------------
    // Selecting an already installed mod that has an optional\ folder
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns true when the request was fully handled here (the caller must stop),
    /// false when the normal install should continue.
    /// </summary>
    private async Task<bool> TryHandleExistingModWithOptionalAsync()
    {
        if (_isInstallingOptionalPackage || _isMixedInstallActive || _currentStep != WizardStep.Step3)
        {
            return false;
        }

        var optionalFolder = GetOptionalFolderForCurrentInstall();
        var optionalsFolder = GetOptionalsFolderForCurrentInstall();
        if (optionalFolder == null && optionalsFolder == null)
        {
            return false;
        }

        var baseRecord = ModLoaderService.FindBaseInstallationBySource(_selectedGamePath, _selectedModPackageRoot);
        if (baseRecord == null)
        {
            return false;
        }

        var optionalRecords = ModLoaderService.FindOptionalInstallations(_selectedGamePath, baseRecord.ModId);
        var singleOptionalRecords = optionalRecords
            .Where(record => string.Equals(record.OptionalKind, OptionalKindSingle, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var multipleOptionalRecords = optionalRecords
            .Where(record => string.Equals(record.OptionalKind, OptionalKindMany, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var rtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        ExistingOptionalAction choice;
        using (var dialog = new ExistingOptionalActionDialog(
                   _appName,
                   string.Format(
                       _localizationService.GetString("ExistingOptionalMessage", "'{0}' is already installed. What do you want to do?"),
                       _selectedModName),
                   _localizationService.GetString("ExistingOptionalReinstall", "Reinstall"),
                   _localizationService.GetString("ExistingOptionalDelete", "Delete"),
                   _localizationService.GetString("ExistingOptionalCancel", "Cancel"),
                   _localizationService.GetString("ExistingOptionalDeleteOptional", "Delete optional"),
                   _localizationService.GetString("ExistingOptionalDeleteBoth", "Delete base mod and optional"),
                   optionalFolder != null,
                   singleOptionalRecords.Count > 0,
                   optionalsFolder != null,
                   multipleOptionalRecords.Count > 0,
                   _localizationService.GetString("ExistingOptionalsDeleteAll", "Delete all optionals"),
                   _localizationService.GetString("ExistingOptionalsDeleteSome", "Delete some optional"),
                   _localizationService.GetString("ExistingOptionalsDeleteBaseAndAll", "Delete base mod and all optionals"),
                   _localizationService.GetString("ExistingOptionalsDeleteBaseAndSome", "Delete base mod and some optional"),
                   rtl))
        {
            dialog.ShowDialog(this);
            choice = dialog.Choice;
        }

        switch (choice)
        {
            case ExistingOptionalAction.Reinstall:
                if (!await DeleteInstallationRecordAsync(baseRecord))
                {
                    return true;
                }

                _lastActionWasDelete = false;
                return false; // continue with the normal install

            case ExistingOptionalAction.DeleteBaseMod:
                await DeleteInstallationRecordAsync(baseRecord);
                FinishExistingModDeletion();
                return true;

            case ExistingOptionalAction.DeleteOptional:
                foreach (var record in singleOptionalRecords)
                {
                    await DeleteInstallationRecordAsync(record);
                }

                FinishExistingModDeletion();
                return true;

            case ExistingOptionalAction.DeleteBaseModAndOptional:
                foreach (var record in singleOptionalRecords)
                {
                    await DeleteInstallationRecordAsync(record);
                }

                await DeleteInstallationRecordAsync(baseRecord);
                FinishExistingModDeletion();
                return true;

            case ExistingOptionalAction.DeleteAllOptionals:
                if (await DeleteOptionalRecordsAsync(multipleOptionalRecords))
                {
                    FinishOptionalChildrenDeletion();
                }

                return true;

            case ExistingOptionalAction.DeleteSomeOptionals:
                ShowOptionalDeleteStep(multipleOptionalRecords, deleteBaseRecord: null);
                return true;

            case ExistingOptionalAction.DeleteBaseModAndAllOptionals:
                if (await DeleteOptionalRecordsAsync(multipleOptionalRecords)
                    && await DeleteInstallationRecordAsync(baseRecord))
                {
                    FinishExistingModDeletion();
                }

                return true;

            case ExistingOptionalAction.DeleteBaseModAndSomeOptionals:
                ShowOptionalDeleteStep(multipleOptionalRecords, baseRecord);
                return true;

            default:
                _lastActionWasDelete = false;
                GoToStep(WizardStep.Step6);
                return true;
        }
    }

    private void FinishExistingModDeletion()
    {
        _lastActionWasDelete = true;
        RefreshModList();
        GoToStep(WizardStep.Step6);
    }

    private void FinishOptionalChildrenDeletion()
    {
        _lastActionWasDelete = false;
        RefreshModList();
        RefreshStep6OptionalActions();
        GoToStep(WizardStep.Step6);
    }

    private async Task<bool> DeleteOptionalRecordsAsync(IReadOnlyList<InstallationManifestEntry> records)
    {
        var success = true;
        foreach (var record in records)
        {
            success = await DeleteInstallationRecordAsync(record) && success;
        }

        return success;
    }

    private void ShowOptionalDeleteStep(
        IReadOnlyList<InstallationManifestEntry> records,
        InstallationManifestEntry? deleteBaseRecord)
    {
        if (records.Count == 0)
        {
            return;
        }

        var entries = records.Select(record =>
        {
            var files = (record.InstalledFiles ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0 && IsStrictModLoaderSubfolder(_selectedGamePath, record.InstalledDestination)
                && Directory.Exists(record.InstalledDestination))
            {
                files = Directory.GetFiles(record.InstalledDestination, "*", SearchOption.AllDirectories).ToList();
            }

            var sourceName = string.IsNullOrWhiteSpace(record.SourcePackagePath)
                ? record.ModId
                : Path.GetFileName(Path.TrimEndingDirectorySeparator(record.SourcePackagePath));
            return CreateDeleteEntry(
                sourceName,
                files,
                new DeleteEntryInfo(
                    DeleteEntryKind.Recorded,
                    record.ModId,
                    record.Type,
                    record.InstalledDestination,
                    record.SourcePackagePath));
        }).ToList();

        _deleteOptionalBaseRecordAfterSelection = deleteBaseRecord;
        _deleteOptionalRowsRemoved = false;
        _lastActionWasDelete = false;
        ShowDeleteModsStep(
            entries,
            async (entry, progress) =>
            {
                var deleted = await DeleteInstalledEntryAsync(entry, progress);
                _deleteOptionalRowsRemoved |= deleted;
                return deleted;
            },
            WizardStep.Step6,
            clearSidebarMedia: true);
    }

    /// <summary>Removes everything one installation record installed (safe delete + backup restore).</summary>
    private async Task<bool> DeleteInstallationRecordAsync(InstallationManifestEntry record)
    {
        var files = (record.InstalledFiles ?? new List<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0 && IsStrictModLoaderSubfolder(_selectedGamePath, record.InstalledDestination)
            && Directory.Exists(record.InstalledDestination))
        {
            files = Directory.GetFiles(record.InstalledDestination, "*", SearchOption.AllDirectories).ToList();
        }

        var entry = CreateDeleteEntry(
            record.ModId,
            files,
            new DeleteEntryInfo(DeleteEntryKind.Recorded, record.ModId, record.Type, record.InstalledDestination, record.SourcePackagePath));
        UseWaitCursor = true;
        try
        {
            return await DeleteInstalledEntryAsync(entry, new Progress<int>());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        finally
        {
            UseWaitCursor = false;
        }
    }
}
