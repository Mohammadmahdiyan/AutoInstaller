using GtaSaModManager.Services;
using GtaSaModManager.Controls;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm
{
    private bool IsSaveMissionPackage => _selectedModManifest?.NormalizedType is "saveandmission" or "savesandmissions";

    private async Task<bool> PrepareExistingSaveMissionInstallationAsync()
    {
        if (_selectedModManifest is null || !IsSaveMissionPackage)
        {
            return true;
        }

        var existingInstallation = ModLoaderService.FindGameInstallationRecord(
            _selectedGamePath,
            _selectedModManifest.NormalizedType,
            _selectedModName,
            _selectedModPackageRoot);
        if (existingInstallation is null)
        {
            return true;
        }

        var action = PromptForExistingPutInGameFolderAction(_selectedModName);
        if (action is not (DialogResult.Yes or DialogResult.No))
        {
            return false;
        }

        GoToStep(WizardStep.Step4);
        if (!await UninstallByModIdWithProgressAsync(
                existingInstallation.ModId,
                _selectedModManifest.NormalizedType,
                null,
                _selectedGamePath))
        {
            MessageBox.Show(
                _localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        _isSaveMissionStepActive = false;
        RefreshModList();
        if (action == DialogResult.No)
        {
            _lastActionWasDelete = true;
            if (!_isInstallingOptionalPackage)
            {
                GoToStep(WizardStep.Step6);
            }

            return false;
        }

        _lastActionWasDelete = false;
        return true;
    }

    private bool PrepareSaveMissionStep()
    {
        _saveMissionPackageFiles.Clear();
        _saveMissionInstalledFiles.Clear();
        _saveMissionInstalledTargets.Clear();
        _saveMissionFileIndex = 0;
        _selectedSaveMissionSlot = null;

        if (string.IsNullOrWhiteSpace(_selectedModPayloadPath) || !Directory.Exists(_selectedModPayloadPath))
        {
            return false;
        }

        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;
        IReadOnlyList<UserFilesInstallCopy> userFileAdditions;
        try
        {
            userFileAdditions = UserFilesInstallService.CreateCopyPlan(
                _selectedModManifest?.AddToUserFile,
                Directory.Exists(_selectedModPackageRoot) ? _selectedModPackageRoot : _selectedModPayloadPath,
                baseModsFolder,
                GetGtaUserFilesDirectory());
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show("Could not resolve addToUserFile entries: " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var userFileSourcePaths = userFileAdditions
            .Select(copy => Path.GetFullPath(copy.SourcePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _saveMissionPackageFiles.AddRange(Directory.GetFiles(_selectedModPayloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path, _selectedModPackageRoot))
            .Where(path => !userFileSourcePaths.Contains(Path.GetFullPath(path)))
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
            title.Text = !string.IsNullOrWhiteSpace(_selectedModPackageRoot)
                ? Path.GetFileName(Path.TrimEndingDirectorySeparator(_selectedModPackageRoot))
                : _selectedModName;
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
            var baseText = File.Exists(existingPath)
                ? existingDisplayName
                : sourceFile.IsMission
                    ? string.Format(_localizationService.GetString("MissionSlotEmpty", "slot {0} is empty"), slot)
                    : string.Format(_localizationService.GetString("SaveSlotMissing", "SAVE FILE {0} NOT PRESENT"), slot);
            _saveMissionSlotBaseTexts[index] = baseText;
            _saveMissionSlotButtons[index].Text = baseText;
            _saveMissionSlotButtons[index].Checked = _selectedSaveMissionSlot == slot;
        }

        if (stepPanel.Controls.Find("SaveMissionSelectedSlotBadge", true).FirstOrDefault() is Label selectedSlotBadge)
        {
            selectedSlotBadge.Text = _selectedSaveMissionSlot?.ToString() ?? "0";
        }

        RefreshSaveMissionSlotAppearance();
        UpdateSaveMissionSelectedSlotText(_selectedSaveMissionSlot);
        UpdateSidebarState();
    }

    private void UpdateSaveMissionSelectedSlotText(int? selectedSlot)
    {
        _saveMissionSlotAnimationTimer.Stop();
        if (_saveMissionSlotAnimationButton != null && !_saveMissionSlotAnimationButton.IsDisposed
            && _saveMissionSlotAnimationButton.Tag is int oldSlot
            && oldSlot >= 1 && oldSlot <= _saveMissionSlotBaseTexts.Count)
        {
            _saveMissionSlotAnimationButton.Text = _saveMissionSlotBaseTexts[oldSlot - 1];
        }

        if (!_isSaveMissionStepActive
            || !selectedSlot.HasValue
            || (uint)_saveMissionFileIndex >= (uint)_saveMissionPackageFiles.Count
            || selectedSlot.Value < 1
            || selectedSlot.Value > _saveMissionSlotButtons.Count)
        {
            _saveMissionSlotAnimationButton = null;
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

        var selectedIndex = selectedSlot.Value - 1;
        var selectedButton = _saveMissionSlotButtons[selectedIndex];
        selectedButton.Text = _saveMissionSlotBaseTexts[selectedIndex]
            + " <= " + title + " (" + folderName + ")";
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        _saveMissionSlotAnimationButton = selectedButton;
        _saveMissionSlotAnimationStartColor = palette.Accent;
        _saveMissionSlotAnimationTargetColor = Color.White;
        _saveMissionSlotAnimationStartBackColor = palette.AccentSoft;
        _saveMissionSlotAnimationTargetBackColor = palette.Accent;
        selectedButton.ForeColor = _saveMissionSlotAnimationStartColor;
        selectedButton.BackColor = _saveMissionSlotAnimationStartBackColor;
        _saveMissionSlotAnimationFrame = 0;
        _saveMissionSlotAnimationTimer.Start();
    }

    private void AnimateSaveMissionSlotSelection()
    {
        if (_saveMissionSlotAnimationButton == null || _saveMissionSlotAnimationButton.IsDisposed)
        {
            _saveMissionSlotAnimationTimer.Stop();
            return;
        }

        const int frameCount = 12;
        _saveMissionSlotAnimationFrame++;
        var progress = Math.Min(1F, _saveMissionSlotAnimationFrame / (float)frameCount);
        var eased = progress * progress * (3F - (2F * progress));
        _saveMissionSlotAnimationButton.ForeColor = Color.FromArgb(
            (int)Math.Round(_saveMissionSlotAnimationStartColor.R + ((_saveMissionSlotAnimationTargetColor.R - _saveMissionSlotAnimationStartColor.R) * eased)),
            (int)Math.Round(_saveMissionSlotAnimationStartColor.G + ((_saveMissionSlotAnimationTargetColor.G - _saveMissionSlotAnimationStartColor.G) * eased)),
            (int)Math.Round(_saveMissionSlotAnimationStartColor.B + ((_saveMissionSlotAnimationTargetColor.B - _saveMissionSlotAnimationStartColor.B) * eased)));
        _saveMissionSlotAnimationButton.BackColor = Color.FromArgb(
            (int)Math.Round(_saveMissionSlotAnimationStartBackColor.R + ((_saveMissionSlotAnimationTargetBackColor.R - _saveMissionSlotAnimationStartBackColor.R) * eased)),
            (int)Math.Round(_saveMissionSlotAnimationStartBackColor.G + ((_saveMissionSlotAnimationTargetBackColor.G - _saveMissionSlotAnimationStartBackColor.G) * eased)),
            (int)Math.Round(_saveMissionSlotAnimationStartBackColor.B + ((_saveMissionSlotAnimationTargetBackColor.B - _saveMissionSlotAnimationStartBackColor.B) * eased)));

        if (progress >= 1F)
        {
            _saveMissionSlotAnimationTimer.Stop();
        }
    }

    private void RefreshSaveMissionSlotAppearance()
    {
        foreach (var button in _saveMissionSlotButtons)
        {
            ApplySaveMissionSlotAppearance(button, isHovered: false);
        }

        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        if (_wizardPanels.TryGetValue(WizardStep.Step5, out var stepPanel)
            && stepPanel.Controls.Find("SaveMissionSelectedSlotBadge", true).FirstOrDefault() is Label badge)
        {
            var hasSelection = _selectedSaveMissionSlot.HasValue;
            badge.BackColor = hasSelection ? palette.AccentSoft : palette.SurfaceSecondary;
            badge.ForeColor = hasSelection ? palette.Accent : palette.TextSecondary;
        }
    }

    private void ApplySaveMissionSlotAppearance(RadioButton button, bool isHovered)
    {
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        var isSelected = button.Tag is int slot && _selectedSaveMissionSlot == slot;
        button.BackColor = isSelected
            ? isHovered ? palette.AccentHover : palette.Accent
            : isHovered ? palette.AccentSoft : palette.SurfaceSecondary;
        button.ForeColor = isSelected ? Color.White : palette.TextPrimary;
        button.FlatAppearance.BorderColor = isSelected || isHovered ? palette.Accent : palette.BorderSoft;
        button.FlatAppearance.MouseOverBackColor = isSelected ? palette.AccentHover : palette.AccentSoft;
        button.FlatAppearance.MouseDownBackColor = palette.AccentHover;
    }

    private void ShowSaveMissionStatus()
    {
        var lines = _saveMissionPackageFiles.Select(file =>
        {
            var sourceName = FormatSaveMissionStatusFile(file.Path, file.IsMission);
            var targetName = _saveMissionInstalledTargets.TryGetValue(file.Path, out var destination)
                ? FormatSaveMissionStatusFile(destination, file.IsMission)
                : _localizationService.GetString("Pending", "Pending");
            return sourceName + " => " + targetName;
        });
        MessageBox.Show(
            string.Join(Environment.NewLine, lines),
            _localizationService.GetString("MultiAssetSummaryTitle", "Mod Status"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string FormatSaveMissionStatusFile(string path, bool isMission)
    {
        var fileName = Path.GetFileName(path);
        var title = isMission
            ? SaveMissionNameReader.TryReadDyomMissionName(path)
            : SaveMissionNameReader.TryReadGtaSaveName(path);
        return string.IsNullOrWhiteSpace(title) ? fileName : fileName + " (" + title + ")";
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
        var isLastFile = _selectedModManifest?.NormalizedType == "saveandmission"
            || _saveMissionFileIndex + 1 >= _saveMissionPackageFiles.Count;
        IReadOnlyList<UserFilesInstallCopy> userFileAdditions = Array.Empty<UserFilesInstallCopy>();
        if (isLastFile)
        {
            var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
                ? _selectedModSourcePath
                : _settings.ModSourceFolder ?? string.Empty;
            try
            {
                userFileAdditions = UserFilesInstallService.CreateCopyPlan(
                    _selectedModManifest?.AddToUserFile,
                    Directory.Exists(_selectedModPackageRoot) ? _selectedModPackageRoot : _selectedModPayloadPath,
                    baseModsFolder,
                    userFilesRoot);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                MessageBox.Show("Could not resolve addToUserFile entries: " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

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
            var progressFiles = new List<(string SourcePath, string RelativeName)> { (sourceFile.Path, relativeName) };
            progressFiles.AddRange(userFileAdditions.Select(copy => (
                copy.SourcePath,
                Path.GetRelativePath(userFilesRoot, copy.DestinationPath))));
            BeginStep4Progress(_localizationService.GetString("Installing", "Installing") + " " + _selectedModName, progressFiles);
        try
        {
                await CopyStep4FileAsync(0, sourceFile.Path, destination, relativeName);
            _saveMissionInstalledFiles.Add(destination);
            _saveMissionInstalledTargets[sourceFile.Path] = destination;
                for (var index = 0; index < userFileAdditions.Count; index++)
                {
                    var addition = userFileAdditions[index];
                    await CopyStep4FileAsync(
                        index + 1,
                        addition.SourcePath,
                        addition.DestinationPath,
                        Path.GetRelativePath(userFilesRoot, addition.DestinationPath));
                    _saveMissionInstalledFiles.Add(addition.DestinationPath);
                }

            if (!_isMixedInstallActive)
            {
                ModLoaderService.RecordUserFilesInstallation(
                    _selectedGamePath,
                    _selectedModName,
                    _selectedModManifest!.NormalizedType,
                    _saveMissionInstalledFiles,
                    userFilesRoot,
                    mergeExistingFiles: true,
                    sourcePackagePath: _selectedModPackageRoot);
            }
            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
        }
        catch (Exception ex)
        {
            if (!_isMixedInstallActive && _saveMissionInstalledFiles.Count > 0)
            {
                ModLoaderService.RecordUserFilesInstallation(
                    _selectedGamePath,
                    _selectedModName,
                    _selectedModManifest!.NormalizedType,
                    _saveMissionInstalledFiles,
                    userFilesRoot,
                    mergeExistingFiles: true,
                    sourcePackagePath: _selectedModPackageRoot);
            }

            if (_isMixedInstallActive && _saveMissionInstalledFiles.Count > 0
                && (uint)_mixedPartIndex < (uint)_mixedParts.Count)
            {
                AddMixedInstalledPart(_mixedParts[_mixedPartIndex], _saveMissionInstalledFiles);
                RecordCurrentMixedInstallation();
            }

            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (_isMixedInstallActive)
            {
                GoToStep(WizardStep.Step5);
            }

            return;
        }

        _selectedSaveMissionSlot = null;
        if (_selectedModManifest?.NormalizedType == "savesandmissions"
            && _saveMissionFileIndex + 1 < _saveMissionPackageFiles.Count)
        {
            _saveMissionFileIndex++;
            GoToStep(WizardStep.Step5);
            RefreshSaveMissionStep();
            return;
        }

        _isSaveMissionStepActive = false;
        _lastActionWasDelete = false;
        if (_isMixedInstallActive)
        {
            await CompleteCurrentMixedPartAsync(_saveMissionInstalledFiles.ToList());
            return;
        }

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