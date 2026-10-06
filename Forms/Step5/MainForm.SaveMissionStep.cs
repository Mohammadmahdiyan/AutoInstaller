using GtaSaModManager.Services;
using GtaSaModManager.Controls;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm
{
    private bool IsSaveMissionPackage => _selectedModManifest?.NormalizedType is "saveandmission" or "savesandmissions";

    private bool PrepareSaveMissionStep()
    {
        _saveMissionPackageFiles.Clear();
        _saveMissionInstalledFiles.Clear();
        _saveMissionFileIndex = 0;
        _selectedSaveMissionSlot = null;

        if (string.IsNullOrWhiteSpace(_selectedModPayloadPath) || !Directory.Exists(_selectedModPayloadPath))
        {
            return false;
        }

        _saveMissionPackageFiles.AddRange(Directory.GetFiles(_selectedModPayloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path, _selectedModPackageRoot))
            .Where(path => Path.GetExtension(path).Equals(".b", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(path).Equals(".dat", StringComparison.OrdinalIgnoreCase))
            .Select(path => new SaveMissionSourceFile(
                path,
                Path.GetExtension(path).Equals(".dat", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase));

        if (_saveMissionPackageFiles.Count == 0)
        {
            MessageBox.Show(
                _localizationService.GetString("ModSourceInvalid", "The selected package does not contain a .b save or .dat mission file."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        if (_selectedModManifest?.NormalizedType == "saveandmission" && _saveMissionPackageFiles.Count != 1)
        {
            MessageBox.Show(
                "SaveAndMission accepts exactly one .b save or .dat mission file. Use SavesAndMissions for multiple files.",
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        _isSaveMissionStepActive = true;
        RefreshSaveMissionStep();
        return true;
    }

    private void RefreshSaveMissionStep()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step5, out var stepPanel))
        {
            return;
        }

        var assetLayout = stepPanel.Controls.Find("AssetStepLayout", true).FirstOrDefault();
        var saveMissionPanel = stepPanel.Controls.Find("SaveMissionSlotPanel", true).FirstOrDefault();
        if (assetLayout != null)
        {
            assetLayout.Visible = !_isSaveMissionStepActive;
        }

        if (saveMissionPanel != null)
        {
            saveMissionPanel.Visible = _isSaveMissionStepActive;
            if (_isSaveMissionStepActive)
            {
                saveMissionPanel.BringToFront();
            }
        }

        if (!_isSaveMissionStepActive || (uint)_saveMissionFileIndex >= (uint)_saveMissionPackageFiles.Count)
        {
            UpdateSidebarState();
            return;
        }

        var sourceFile = _saveMissionPackageFiles[_saveMissionFileIndex];
        var userFilesRoot = GetGtaUserFilesDirectory();
        var slotPrefix = sourceFile.IsMission ? "DYOM" : "GTASAsf";
        var extension = sourceFile.IsMission ? ".dat" : ".b";
        if (stepPanel.Controls.Find("SaveMissionTitle", true).FirstOrDefault() is Label title)
        {
            title.Text = _localizationService.GetString("SelectItem", "Select Item");
        }

        if (stepPanel.Controls.Find("SaveMissionSourceLabel", true).FirstOrDefault() is Label sourceLabel)
        {
            var prompt = sourceFile.IsMission
                ? _localizationService.GetString("MissionSlotPrompt", "Select a mission slot")
                : _localizationService.GetString("SaveSlotPrompt", "Select a save slot");
            sourceLabel.Text = string.Format(
                _localizationService.GetString("SaveMissionFileProgress", "File {0} of {1}: {2}"),
                _saveMissionFileIndex + 1,
                _saveMissionPackageFiles.Count,
                Path.GetFileName(sourceFile.Path)) + Environment.NewLine + prompt;
        }

        if (stepPanel.Controls.Find("SaveMissionStatusButton", true).FirstOrDefault() is Button statusButton)
        {
            var shouldShowStatus = _selectedModManifest?.NormalizedType == "savesandmissions";
            statusButton.Visible = shouldShowStatus;
            var header = stepPanel.Controls.Find("SaveMissionHeader", true).FirstOrDefault() as FlowLayoutPanel;
            if (header != null)
            {
                if (shouldShowStatus && !ReferenceEquals(statusButton.Parent, header))
                {
                    header.Controls.Add(statusButton);
                }
                else if (!shouldShowStatus && ReferenceEquals(statusButton.Parent, header))
                {
                    header.Controls.Remove(statusButton);
                }
            }
        }

        for (var index = 0; index < _saveMissionSlotButtons.Count; index++)
        {
            var slot = index + 1;
            var existingFileName = slotPrefix + slot + extension;
            var existingPath = Path.Combine(userFilesRoot, existingFileName);
            var existingDisplayName = File.Exists(existingPath)
                ? sourceFile.IsMission
                    ? SaveMissionNameReader.TryReadDyomMissionName(existingPath) ?? existingFileName
                    : SaveMissionNameReader.TryReadGtaSaveName(existingPath) ?? existingFileName
                : string.Empty;
            _saveMissionSlotButtons[index].Text = File.Exists(existingPath)
                ? existingDisplayName
                : sourceFile.IsMission
                    ? string.Format(_localizationService.GetString("MissionSlotEmpty", "slot {0} is empty"), slot)
                    : string.Format(_localizationService.GetString("SaveSlotMissing", "SAVE FILE {0} NOT PRESENT"), slot);
            _saveMissionSlotButtons[index].Checked = _selectedSaveMissionSlot == slot;
        }

        if (stepPanel.Controls.Find("SaveMissionSelectedSlotBadge", true).FirstOrDefault() is Label selectedSlotBadge)
        {
            selectedSlotBadge.Text = _selectedSaveMissionSlot?.ToString() ?? "0";
        }

        UpdateSaveMissionActiveLabel(_selectedSaveMissionSlot);
        RefreshSaveMissionSlotAppearance();
        UpdateSidebarState();
    }

    private void UpdateSaveMissionActiveLabel(int? selectedSlot)
    {
        _saveMissionActiveLabel ??= _wizardPanels.TryGetValue(WizardStep.Step5, out var stepPanel)
            ? stepPanel.Controls.Find("SaveMissionActiveModLabel", true).FirstOrDefault() as Label
            : null;
        if (_saveMissionActiveLabel == null || _saveMissionActiveLabel.IsDisposed)
        {
            return;
        }

        _saveMissionActiveLabelTimer.Stop();
        if (!_isSaveMissionStepActive
            || !selectedSlot.HasValue
            || (uint)_saveMissionFileIndex >= (uint)_saveMissionPackageFiles.Count)
        {
            _saveMissionActiveLabel.Visible = false;
            return;
        }

        var sourceFile = _saveMissionPackageFiles[_saveMissionFileIndex];
        var title = sourceFile.IsMission
            ? SaveMissionNameReader.TryReadDyomMissionName(sourceFile.Path)
            : SaveMissionNameReader.TryReadGtaSaveName(sourceFile.Path);
        title ??= Path.GetFileNameWithoutExtension(sourceFile.Path);
        var folderName = !string.IsNullOrWhiteSpace(_selectedModPackageRoot)
            ? Path.GetFileName(Path.TrimEndingDirectorySeparator(_selectedModPackageRoot))
            : _selectedModName;
        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = _selectedModName;
        }

        _saveMissionActiveLabel.Text = "<= " + title + " (" + folderName + ")";
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        _saveMissionActiveLabelTargetColor = palette.Accent;
        _saveMissionActiveLabelStartColor = palette.TextSecondary;
        _saveMissionActiveLabel.Visible = true;
        _saveMissionActiveLabelTargetLocation = _saveMissionActiveLabel.Location;
        _saveMissionActiveLabelStartLocation = _saveMissionActiveLabelTargetLocation + new Size(0, 5);
        _saveMissionActiveLabel.Location = _saveMissionActiveLabelStartLocation;
        _saveMissionActiveLabel.ForeColor = _saveMissionActiveLabelStartColor;
        _saveMissionActiveLabelFrame = 0;
        _saveMissionActiveLabelTimer.Start();
    }

    private void AnimateSaveMissionActiveLabel()
    {
        if (_saveMissionActiveLabel == null || _saveMissionActiveLabel.IsDisposed || !_saveMissionActiveLabel.Visible)
        {
            _saveMissionActiveLabelTimer.Stop();
            return;
        }

        const int frameCount = 12;
        _saveMissionActiveLabelFrame++;
        var progress = Math.Min(1F, _saveMissionActiveLabelFrame / (float)frameCount);
        var eased = progress * progress * (3F - (2F * progress));
        _saveMissionActiveLabel.Location = new Point(
            _saveMissionActiveLabelTargetLocation.X,
            (int)Math.Round(_saveMissionActiveLabelStartLocation.Y
                + ((_saveMissionActiveLabelTargetLocation.Y - _saveMissionActiveLabelStartLocation.Y) * eased)));
        _saveMissionActiveLabel.ForeColor = Color.FromArgb(
            (int)Math.Round(_saveMissionActiveLabelStartColor.R + ((_saveMissionActiveLabelTargetColor.R - _saveMissionActiveLabelStartColor.R) * eased)),
            (int)Math.Round(_saveMissionActiveLabelStartColor.G + ((_saveMissionActiveLabelTargetColor.G - _saveMissionActiveLabelStartColor.G) * eased)),
            (int)Math.Round(_saveMissionActiveLabelStartColor.B + ((_saveMissionActiveLabelTargetColor.B - _saveMissionActiveLabelStartColor.B) * eased)));

        if (progress >= 1F)
        {
            _saveMissionActiveLabelTimer.Stop();
        }
    }

    private void RefreshSaveMissionSlotAppearance()
    {
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        foreach (var button in _saveMissionSlotButtons)
        {
            var isSelected = button.Tag is int slot && _selectedSaveMissionSlot == slot;
            button.BackColor = isSelected ? palette.AccentSoft : palette.SurfaceSecondary;
            button.ForeColor = palette.TextPrimary;
            button.FlatAppearance.BorderColor = isSelected ? palette.Accent : palette.Border;
            button.FlatAppearance.MouseOverBackColor = isSelected ? palette.AccentSoft : palette.Card;
            button.FlatAppearance.MouseDownBackColor = palette.Accent;
        }

        if (_wizardPanels.TryGetValue(WizardStep.Step5, out var stepPanel)
            && stepPanel.Controls.Find("SaveMissionSelectedSlotBadge", true).FirstOrDefault() is Label badge)
        {
            var hasSelection = _selectedSaveMissionSlot.HasValue;
            badge.BackColor = hasSelection ? palette.AccentSoft : palette.SurfaceSecondary;
            badge.ForeColor = hasSelection ? palette.Accent : palette.TextSecondary;
        }
    }

    private void ShowSaveMissionStatus()
    {
        var lines = _saveMissionPackageFiles.Select((file, index) =>
        {
            var installedName = index < _saveMissionInstalledFiles.Count
                ? Path.GetFileName(_saveMissionInstalledFiles[index])
                : _localizationService.GetString("Pending", "Pending");
            return Path.GetFileName(file.Path) + " -> " + installedName;
        });
        MessageBox.Show(
            string.Join(Environment.NewLine, lines),
            _localizationService.GetString("MultiAssetSummaryTitle", "Mod Status"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async Task InstallSelectedSaveMissionFileAsync()
    {
        if (!_selectedSaveMissionSlot.HasValue
            || (uint)_saveMissionFileIndex >= (uint)_saveMissionPackageFiles.Count)
        {
            return;
        }

        var sourceFile = _saveMissionPackageFiles[_saveMissionFileIndex];
        var slot = _selectedSaveMissionSlot.Value;
        var userFilesRoot = GetGtaUserFilesDirectory();
        var targetName = (sourceFile.IsMission ? "DYOM" : "GTASAsf")
            + slot
            + (sourceFile.IsMission ? ".dat" : ".b");
        var destination = Path.Combine(userFilesRoot, targetName);

        if (!_isInstallingOptionalPackage)
        {
            GoToStep(WizardStep.Step4);
        }

        if (sourceFile.IsMission && !await EnsureSaveMissionDyomDependencyAsync(userFilesRoot))
        {
            GoToStep(WizardStep.Step5);
            return;
        }

        if (_saveMissionFileIndex == 0)
        {
            DeleteManifestEntries(_selectedModManifest!);
        }

        if (File.Exists(destination))
        {
            var trashRoot = Path.Combine(userFilesRoot, ".trash");
            Directory.CreateDirectory(trashRoot);
            var archivedPath = Path.Combine(trashRoot, targetName);
            if (File.Exists(archivedPath))
            {
                archivedPath = Path.Combine(trashRoot, Path.GetFileNameWithoutExtension(targetName)
                    + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(targetName));
            }

            File.Move(destination, archivedPath);
        }

            var relativeName = Path.GetRelativePath(_selectedModPackageRoot, sourceFile.Path);
        BeginStep4Progress(
            _localizationService.GetString("Installing", "Installing") + " " + _selectedModName,
            new[] { (sourceFile.Path, relativeName) });
        try
        {
                await CopyStep4FileAsync(0, sourceFile.Path, destination, relativeName);
            _saveMissionInstalledFiles.Add(destination);
            ModLoaderService.RecordUserFilesInstallation(
                _selectedGamePath,
                _selectedModName,
                _selectedModManifest!.NormalizedType,
                _saveMissionInstalledFiles,
                userFilesRoot,
                mergeExistingFiles: true,
                sourcePackagePath: _selectedModPackageRoot);
            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
        }
        catch (Exception ex)
        {
            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedSaveMissionSlot = null;
        if (_selectedModManifest.NormalizedType == "savesandmissions"
            && _saveMissionFileIndex + 1 < _saveMissionPackageFiles.Count)
        {
            _saveMissionFileIndex++;
            GoToStep(WizardStep.Step5);
            RefreshSaveMissionStep();
            return;
        }

        _isSaveMissionStepActive = false;
        _lastActionWasDelete = false;
        RefreshModList();
        GoToStep(WizardStep.Step6);
    }

    private async Task<bool> EnsureSaveMissionDyomDependencyAsync(string userFilesRoot)
    {
        var destination = Path.Combine(userFilesRoot, "DYOM v8.2");
        if (Directory.Exists(destination))
        {
            return true;
        }

        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;
        var dependencyRoot = Path.Combine(baseModsFolder, "Scripts", "DYOM", "DYOM v8.2");
        if (!Directory.Exists(dependencyRoot))
        {
            MessageBox.Show(
                _localizationService.GetString("ModSourceInvalid", "DYOM dependency was not found in the Base Mods folder.")
                    + Environment.NewLine + dependencyRoot,
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        var dependencyFiles = Directory.GetFiles(dependencyRoot, "*", SearchOption.AllDirectories).ToList();
        var displayNames = dependencyFiles
            .Select(path => Path.Combine("DYOM v8.2", Path.GetRelativePath(dependencyRoot, path)))
            .ToList();
        BeginStep4Progress(
            _localizationService.GetString("Installing", "Installing") + " DYOM v8.2",
            dependencyFiles.Select((path, index) => (path, displayNames[index])).ToList());
        await CopyDirectoryWithStep4ProgressAsync(dependencyRoot, destination, dependencyFiles, 0, displayNames);
        CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
        return true;
    }
}