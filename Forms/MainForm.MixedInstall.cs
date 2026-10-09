using GtaSaModManager.Models;
using GtaSaModManager.Modsyn.Conversion;
using GtaSaModManager.Modsyn.Validation;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm
{
    private bool _isMixedInstallActive;
    private ModManifest? _mixedParentManifest;
    private string _mixedParentName = string.Empty;
    private string _mixedParentRoot = string.Empty;
    private string _mixedParentPayload = string.Empty;
    private string _mixedParentReadme = string.Empty;
    private List<string> _mixedParentImages = new();
    private List<ModMixedPackagePart> _mixedParts = new();
    private readonly List<InstallationManifestPart> _mixedInstalledParts = new();
    private int _mixedPartIndex;

    private async Task BeginMixedPackageInstallAsync()
    {
        var parentManifest = _selectedModManifest!;
        if (parentManifest.MixedParts.Count < 2)
        {
            MessageBox.Show("A Mixed package requires at least two valid package folders.", _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = ModLoaderService.FindGameInstallationRecord(
            _selectedGamePath,
            "mixed",
            _selectedModName,
            _selectedModPackageRoot);
        if (existing != null)
        {
            var action = PromptForExistingPutInGameFolderAction(_selectedModName);
            if (action == DialogResult.No)
            {
                var deleteEntries = BuildMixedDeleteEntries(existing);
                ShowDeleteModsStep(deleteEntries, DeleteInstalledEntryAsync, WizardStep.Step6, clearSidebarMedia: false);
                return;
            }

            if (action != DialogResult.Yes)
            {
                return;
            }

            GoToStep(WizardStep.Step4);
            foreach (var entry in BuildMixedDeleteEntries(existing))
            {
                if (!await DeleteMixedPartAsync(entry, new Progress<int>()))
                {
                    MessageBox.Show(
                        _localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled."),
                        _appName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    GoToStep(WizardStep.Step3);
                    return;
                }
            }

            GoToStep(WizardStep.Step3);
        }

        if (!await EnsureRequiredPackagesBeforeInstallAsync()
            || !await EnsureDependenciesBeforeInstallAsync())
        {
            return;
        }

        _mixedParentManifest = parentManifest;
        _mixedParentName = _selectedModName;
        _mixedParentRoot = _selectedModPackageRoot;
        _mixedParentPayload = _selectedModPayloadPath;
        _mixedParentReadme = _selectedReadmePath;
        _mixedParentImages = _selectedImageFiles.ToList();
        _mixedParts = parentManifest.MixedParts.ToList();
        _mixedInstalledParts.Clear();
        _mixedPartIndex = 0;
        _isMixedInstallActive = true;
        await InstallNextMixedPartAsync();
    }

    private async Task InstallNextMixedPartAsync()
    {
        try
        {
            while (_mixedPartIndex < _mixedParts.Count)
            {
                var part = _mixedParts[_mixedPartIndex];
                var partRoot = GetMixedPartRoot(part);
                var partPayload = ModPackageService.GetInstallPayloadDirectory(partRoot, part.Manifest);
                if (string.IsNullOrWhiteSpace(partPayload) || !Directory.Exists(partPayload))
                {
                    partPayload = partRoot;
                }

                _selectedModManifest = part.Manifest;
                _selectedModPayloadPath = partPayload;
                _selectedModPackageRoot = _mixedParentRoot;
                _selectedModName = GetMixedPartInstallName(part);
                _selectedReadmePath = FindReadmeFile(partRoot);
                _selectedImageFiles = part.Manifest.SupportsAssetSelection
                    ? FindImageFiles(_mixedParentRoot)
                    : FindImageFiles(partRoot);

                if (part.Manifest.NormalizedType is "saveandmission" or "savesandmissions")
                {
                    _isSaveMissionStepActive = false;
                    if (!PrepareSaveMissionStep())
                    {
                        AbortMixedInstall();
                        return;
                    }

                    GoToStep(WizardStep.Step5);
                    return;
                }

                if (part.Manifest.NormalizedType == "missiondsl")
                {
                    var currentIndex = _mixedPartIndex;
                    GoToStep(WizardStep.Step4);
                        await InstallMissionDslPackageAsync(partPayload, _selectedModName, recordInstallation: false);
                    if (_isMixedInstallActive && _mixedPartIndex == currentIndex)
                    {
                        AbortMixedInstall();
                    }

                    return;
                }

                if (part.Manifest.SupportsAssetSelection)
                {
                    _selectedAssetForInstall = null;
                    _step5PreparedPayloadPath = string.Empty;
                    _step5PreparationAttempted = false;
                    _step5UnknownAssetTypeCancelled = false;
                    _multiSourceModels.Clear();
                    _multiIndex = 0;
                    if (part.Manifest.IsMultiAssetPackage)
                    {
                        _multiSourceModels.AddRange(BuildSourceModels(partPayload));
                        if (_multiSourceModels.Count == 0)
                        {
                            MessageBox.Show("No model files were found in Mixed folder: " + part.FolderName, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            AbortMixedInstall();
                            return;
                        }
                    }

                    GoToStep(WizardStep.Step5);
                    return;
                }

                var installedFiles = new List<string>();
                if (!await InstallMixedPartAsync(part, partRoot, installedFiles))
                {
                    if (installedFiles.Count > 0)
                    {
                        AddMixedInstalledPart(part, installedFiles);
                        RecordCurrentMixedInstallation();
                    }

                    AbortMixedInstall();
                    return;
                }

                await CompleteCurrentMixedPartAsync(installedFiles);
                return;
            }

            FinishMixedInstall();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AbortMixedInstall();
        }
    }

    private async Task InstallCurrentMixedPartAsync()
    {
        try
        {
            if ((uint)_mixedPartIndex >= (uint)_mixedParts.Count)
            {
                FinishMixedInstall();
                return;
            }

            var part = _mixedParts[_mixedPartIndex];
            if (part.Manifest.NormalizedType is "saveandmission" or "savesandmissions")
            {
                await InstallSelectedSaveMissionFileAsync();
                return;
            }

            if (part.Manifest.IsSingleAssetPackage && _selectedAssetForInstall is null)
            {
                MessageBox.Show(_localizationService.GetString("NoAssetSelected", "Please select an asset before starting the installation."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (part.Manifest.IsMultiAssetPackage && _multiSourceModels.Any(model => model.Status == SourceModelStatus.Pending))
            {
                return;
            }

            var files = new List<string>();
            if (!await InstallMixedPartAsync(part, GetMixedPartRoot(part), files))
            {
                if (files.Count > 0)
                {
                    AddMixedInstalledPart(part, files);
                    RecordCurrentMixedInstallation();
                }

                AbortMixedInstall();
                return;
            }

            await CompleteCurrentMixedPartAsync(files);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AbortMixedInstall();
        }
    }

    private async Task<bool> InstallMixedPartAsync(
        ModMixedPackagePart part,
        string partRoot,
        List<string> installedFiles)
    {
        var payloadPath = ModPackageService.GetInstallPayloadDirectory(partRoot, part.Manifest);
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            payloadPath = partRoot;
        }

        var installName = GetMixedPartInstallName(part);
        if (part.Manifest.IsModLoader
            && !string.Equals(Path.GetFullPath(payloadPath), Path.GetFullPath(partRoot), StringComparison.OrdinalIgnoreCase))
        {
            installName = Path.GetFileName(Path.TrimEndingDirectorySeparator(payloadPath));
        }

        _selectedModManifest = part.Manifest;
        _selectedModPayloadPath = payloadPath;
        _selectedModPackageRoot = _mixedParentRoot;
        _selectedModName = installName;
        var backup = CreateMixedBackupConfiguration(part);
        GoToStep(WizardStep.Step4);

        if (part.Manifest.IsReplacing)
        {
            DeleteManifestEntries(part.Manifest);
            return await InstallReplacingPackageAsync(
                payloadPath,
                _selectedModName,
                partRoot,
                backup,
                recordInstallation: false,
                installedFilesOutput: installedFiles);
        }

        if (part.Manifest.NormalizedType == "putincleo")
        {
            return await InstallPutInCleoPackageAsync(
                partRoot,
                _selectedModName,
                part.Manifest,
                payloadRootOverride: payloadPath,
                recordInstallation: false,
                installedFilesOutput: installedFiles);
        }

        if (part.Manifest.NormalizedType is not (
                "putinmodloader" or "putingamefolder" or "putandreplace" or "putandreplaces"
                or "vehicleandskinandweapon" or "vehiclesandskinsandweapons"))
        {
            MessageBox.Show("This package type is not supported inside Mixed: " + part.Manifest.Type, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return await InstallTypedPackageAsync(
            payloadPath,
            _selectedModName,
            partRoot,
            part.Manifest,
            backup,
            part.Replacements,
            recordInstallation: false,
            installedFilesOutput: installedFiles);
    }

    private async Task CompleteCurrentMixedPartAsync(List<string> installedFiles)
    {
        var part = _mixedParts[_mixedPartIndex];
        AddMixedInstalledPart(part, installedFiles);
        RecordCurrentMixedInstallation();
        _mixedPartIndex++;
        await InstallNextMixedPartAsync();
    }

    private void AddMixedInstalledPart(ModMixedPackagePart part, IEnumerable<string> installedFiles)
    {
        var partType = part.Manifest.NormalizedType;
        var partModId = GetMixedPartInstallName(part);
        var destination = partType switch
        {
            "putincleo" => Path.Combine(_selectedGamePath, "cleo"),
            "missiondsl" => Path.Combine(GetUserFilesRootPath(), "DSL"),
            "saveandmission" or "savesandmissions" => GetUserFilesRootPath(),
            "replacing" or "putingamefolder" or "putandreplace" or "putandreplaces" => _selectedGamePath,
            _ => Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), _selectedModName)
        };
        _mixedInstalledParts.RemoveAll(existing =>
            string.Equals(existing.ModId, partModId, StringComparison.OrdinalIgnoreCase));
        _mixedInstalledParts.Add(new InstallationManifestPart
        {
            Name = part.FolderName,
            Type = partType,
            ModId = partModId,
            InstalledDestination = destination,
            InstalledFiles = installedFiles.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    private void RecordCurrentMixedInstallation()
    {
        var destination = Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), ModPackageService.SanitizeFolderName(_mixedParentName));
        Directory.CreateDirectory(destination);
        ModLoaderService.RecordMixedInstallation(
            _selectedGamePath,
            _mixedParentName,
            _mixedParentRoot,
            destination,
            _mixedInstalledParts);
    }

    private void FinishMixedInstall()
    {
        if (_mixedInstalledParts.Count == 0)
        {
            AbortMixedInstall();
            return;
        }

        RestoreMixedParentContext();
        _isMixedInstallActive = false;
        _lastActionWasDelete = false;
        RefreshModList();
        GoToStep(WizardStep.Step6);
    }

    private void AbortMixedInstall()
    {
        if (_mixedInstalledParts.Count > 0)
        {
            RecordCurrentMixedInstallation();
        }

        RestoreMixedParentContext();
        _isMixedInstallActive = false;
        GoToStep(WizardStep.Step3);
    }

    private void RestoreMixedParentContext()
    {
        _selectedModManifest = _mixedParentManifest;
        _selectedModName = _mixedParentName;
        _selectedModPackageRoot = _mixedParentRoot;
        _selectedModPayloadPath = _mixedParentPayload;
        _selectedReadmePath = _mixedParentReadme;
        _selectedImageFiles = _mixedParentImages;
        _selectedAssetForInstall = null;
        _multiSourceModels.Clear();
        _isSaveMissionStepActive = false;
        _saveMissionPackageFiles.Clear();
        _saveMissionInstalledFiles.Clear();
        _saveMissionInstalledTargets.Clear();
        _saveMissionFileIndex = 0;
        _selectedSaveMissionSlot = null;
        _step5PreparedPayloadPath = string.Empty;
        _step5PreparationAttempted = false;
        _mixedParentManifest = null;
        _mixedParentName = string.Empty;
        _mixedParentRoot = string.Empty;
        _mixedParentPayload = string.Empty;
        _mixedParentReadme = string.Empty;
        _mixedParentImages = new List<string>();
        _mixedParts = new List<ModMixedPackagePart>();
        _mixedInstalledParts.Clear();
        _mixedPartIndex = 0;
    }

    private string GetMixedPartRoot(ModMixedPackagePart part)
    {
        var root = Path.GetFullPath(_mixedParentRoot);
        var path = Path.GetFullPath(Path.Combine(root, part.FolderName.Replace('\\', Path.DirectorySeparatorChar)));
        var prefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(path))
        {
            throw new InvalidDataException("Mixed part folder was not found in the package: " + part.FolderName);
        }

        return path;
    }

    private string GetMixedPartInstallName(ModMixedPackagePart part)
    {
        var safeParentName = ModPackageService.SanitizeFolderName(_mixedParentName);
        return Path.Combine(safeParentName, part.FolderName.Replace('\\', Path.DirectorySeparatorChar));
    }

    private static ModsynBackupConfiguration CreateMixedBackupConfiguration(ModMixedPackagePart part)
    {
        var mode = Enum.TryParse<ModsynBackupMode>(part.BackupMode, ignoreCase: true, out var parsedMode)
            ? parsedMode
            : ModsynBackupMode.All;
        return new ModsynBackupConfiguration(mode, part.BackupPaths, part.ExcludedBackupPaths);
    }
}