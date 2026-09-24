using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Models;
using GtaSaModManager.Services;
using ModManifestModel = GtaSaModManager.Models.ModManifest;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------
    private async void InstallModButton_Click(object sender, EventArgs e)
    {
        if (!GameService.IsValidGameFolder(_selectedGamePath))
        {
            MessageBox.Show(_localizationService.GetString("GameFolderNotConfiguredMessage", "Please select a valid GTA San Andreas folder first."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var openFileDialog = new OpenFileDialog
        {
            Title = _localizationService.GetString("SelectMod", "Select a mod folder or archive"),
            CheckFileExists = true,
            Filter = "Mod files|*.zip;*.7z;*.rar;*.*",
            ValidateNames = true
        };

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var selectedPath = openFileDialog.FileName;
        string? packageRoot = null;

        try
        {
            packageRoot = ModPackageService.ResolvePackageRoot(selectedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_localizationService.GetString("ExtractionFailed", "The mod archive could not be extracted.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot) || !ModPackageService.IsModPackageRoot(packageRoot))
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected mod package is not a valid ModLoader package."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var payloadPath = ModPackageService.GetPayloadDirectory(packageRoot);
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain a payload folder to install."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var manifest = ModPackageService.ResolveManifest(packageRoot);
        if (manifest is null)
        {
            MessageBox.Show(_localizationService.GetString("ModJsonInvalid", "mod.json is malformed or contains unexpected content. The mod name still uses the folder name."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var modName = ModPackageService.ResolveModName(packageRoot, Path.GetFileName(packageRoot));

        if (manifest.IsSingleAssetPackage || manifest.IsMultiAssetPackage)
        {
            payloadPath = packageRoot;
        }

        if (!await EnsureDependenciesBeforeInstallAsync())
        {
            return;
        }

        if (manifest.IsReplacing)
        {
            await InstallReplacingPackageAsync(payloadPath, modName, packageRoot);
            return;
        }

        if (manifest.NormalizedType is "putinmodloader" or "putincleo" or "putingamefolder" or "putandreplace" or "putandreplaces" or "vehicleandskinandweapon" or "vehiclesandskinsandweapons")
        {
            await InstallTypedPackageAsync(payloadPath, modName, packageRoot, manifest);
            return;
        }

        MessageBox.Show("This mod type is not implemented yet: " + manifest.Type, _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;

    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------
    private void OpenGameFolderButton_Click(object sender, EventArgs e)
    {
        if (!GameService.OpenGameFolder(_selectedGamePath))
        {
            MessageBox.Show(_localizationService.GetString("GameFolderNotConfiguredMessage", "Please select a valid GTA San Andreas folder first."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RunGameButton_Click(object sender, EventArgs e)
    {
        if (!GameService.LaunchGame(_selectedGamePath))
        {
            MessageBox.Show(_localizationService.GetString("GameLaunchFailed", "The game could not be launched."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ModLibraryChangeButton_Click(object sender, EventArgs e)
    {
        var selected = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"), _settings.ModSourceFolder);
        if (string.IsNullOrWhiteSpace(selected) || !Directory.Exists(selected))
        {
            MessageBox.Show(_localizationService.GetString("InvalidModLibraryFolder", "This folder is not a valid mods library folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedModSourcePath = selected;
        _settings.ModSourceFolder = selected;
        _settingsService.Save(_settings);
        if (ModLibraryPathTextBox != null)
        {
            ModLibraryPathTextBox.Text = selected;
        }

        RefreshModLibrary();
    }

    private void ModLibraryOpenButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedModSourcePath) || !Directory.Exists(_selectedModSourcePath))
        {
            MessageBox.Show(_localizationService.GetString("InvalidModLibraryFolder", "This folder is not a valid mods library folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = _selectedModSourcePath, UseShellExecute = true, Verb = "open" });
    }

    private void GamePathSelectButton_Click(object sender, EventArgs e)
    {
        var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"), _settings.GamePath);
        if (string.IsNullOrWhiteSpace(selected) || !GameService.IsValidGameFolder(selected))
        {
            MessageBox.Show(_localizationService.GetString("InvalidGameFolder", "This folder does not contain gta_sa.exe."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedGamePath = selected;
        _settings.GamePath = selected;
        _settings.GameExecutableName = "gta_sa.exe";
        _settings.GameProfileId = BuildProfileId(selected, _settings.GameExecutableName);
        _settingsService.Save(_settings);
        if (GamePathTextBox != null)
        {
            GamePathTextBox.Text = selected;
        }

        GameService.EnsureModLoaderFolder(selected);
        LoadSettingsIntoUi();
        RefreshModList();
        GoToStep(WizardStep.Step2);
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------
    private void GoToStep(WizardStep step)
    {
        _currentStep = step;

        foreach (var item in _wizardPanels)
        {
            item.Value.Visible = item.Key == step;
        }

        if (step == WizardStep.Step5 && !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
        {
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
        }

        UpdateSidebarState();

        // Ensure designer header panels are hidden while wizard is active to avoid overlapping UI
        try
        {
            if (GamePanel != null) GamePanel.Visible = false;
            if (ModLibraryPanel != null) ModLibraryPanel.Visible = false;
            if (ModLoaderPanel != null) ModLoaderPanel.Visible = false;
        }
        catch
        {
            // ignore if controls are not initialized yet
        }

        // Synchronize inputs from cache for the newly visible step
        SynchronizeStepInputs(step);

        if (step == WizardStep.Step5)
        {
            RefreshAssetStep();
        }

        if (step == WizardStep.Step4)
        {
            RefreshStep4Preview();
        }

        if (step is WizardStep.Step4 or WizardStep.Step6)
        {
            RefreshWizardImageGallery(step);
        }

        if (step == WizardStep.Step6)
        {
            var hasOptionalPackages = GetOptionalPackageRootsForCurrentInstall().Count > 0;
            _completionSecondsLeft = hasOptionalPackages ? 15 : 6;
            _completionTimerActive = true;
            _completionTimer.Start();
            if (_wizardPanels[WizardStep.Step6].Controls.OfType<Label>().FirstOrDefault(x => x.Name == "CountdownLabel") is { } countdown)
            {
                countdown.Text = _localizationService.GetString("CompletedIn", "Completed") + " " + _completionSecondsLeft + "s";
            }
            RefreshStep6OptionalActions();
        }
        else
        {
            _completionTimer.Stop();
            _completionTimerActive = false;
        }
    }

    private void NavigateToStep(WizardStep step)
    {
        GoToStep(step);
    }

    private static string? DetectInstalledExecutable(string gameFolder)
    {
        if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
        {
            return null;
        }

        foreach (var executable in new[] { "gta_sa.exe", "GTA 5 FARSI.exe" })
        {
            if (File.Exists(Path.Combine(gameFolder, executable)))
            {
                return executable;
            }
        }

        return null;
    }

    private void SyncCachedGameDataFromFilesystem()
    {
        var cachedFolder = _settings.GamePath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cachedFolder) || !Directory.Exists(cachedFolder))
        {
            return;
        }

        var detectedExecutable = DetectInstalledExecutable(cachedFolder);
        if (string.IsNullOrWhiteSpace(detectedExecutable))
        {
            return;
        }

        var expectedProfile = BuildProfileId(cachedFolder, detectedExecutable);
        var needsSave = !string.Equals(_settings.GameExecutableName, detectedExecutable, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_settings.GameProfileId, expectedProfile, StringComparison.OrdinalIgnoreCase);

        if (needsSave)
        {
            _settings.GameExecutableName = detectedExecutable;
            _settings.GameProfileId = expectedProfile;
            _settingsService.Save(_settings);
        }
    }

    private bool IsCachedGameFolderValid()
    {
        var cachedFolder = _settings.GamePath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cachedFolder) || !Directory.Exists(cachedFolder))
        {
            return false;
        }

        return DetectInstalledExecutable(cachedFolder) is not null;
    }

    private bool IsCachedGameProfileValid()
    {
        if (!IsCachedGameFolderValid())
        {
            return false;
        }

        var cachedFolder = _settings.GamePath ?? string.Empty;
        var cachedExecutable = !string.IsNullOrWhiteSpace(_settings.GameExecutableName)
            && File.Exists(Path.Combine(cachedFolder, _settings.GameExecutableName))
            ? _settings.GameExecutableName
            : DetectInstalledExecutable(cachedFolder);

        if (string.IsNullOrWhiteSpace(cachedExecutable))
        {
            return false;
        }

        var expectedProfile = BuildProfileId(cachedFolder, cachedExecutable);
        var profileMatches = !string.IsNullOrWhiteSpace(_settings.GameProfileId) &&
            string.Equals(_settings.GameProfileId, expectedProfile, StringComparison.OrdinalIgnoreCase);

        return profileMatches && File.Exists(Path.Combine(cachedFolder, cachedExecutable));
    }

    private WizardStep DetermineFirstRequiredStep()
    {
        if (!IsCachedGameFolderValid())
        {
            return WizardStep.Step1;
        }

        if (!IsCachedGameProfileValid())
        {
            return WizardStep.Step2;
        }

        return WizardStep.Step3;
    }

    private void AdvanceToValidStep()
    {
        _selectedGamePath = _settings.GamePath ?? string.Empty;
        _currentStep = DetermineFirstRequiredStep();
        NavigateToStep(_currentStep);
    }

    private async Task<bool> EnsureDependenciesBeforeInstallAsync()
    {
        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;
        var result = await DependencyInstallationService.EnsureInstalledAsync(_selectedGamePath, baseModsFolder);
        if (result.Success)
        {
            if (string.IsNullOrWhiteSpace(_selectedReadmePath))
            {
                var dependencyReadmeRoot = Path.Combine(baseModsFolder, "Scripts", "A1-MyReqFiles");
                _selectedReadmePath = FindReadmeFile(dependencyReadmeRoot);
            }

            return true;
        }

        MessageBox.Show(result.ErrorMessage, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private async Task<bool> EnsureRequiredPackagesBeforeInstallAsync()
    {
        var manifest = _selectedModManifest ?? ModPackageService.ResolveManifest(_selectedModPayloadPath);
        if (manifest == null || !manifest.HasRequirements)
        {
            return true;
        }

        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;

        foreach (var requirement in manifest.GetRequirements())
        {
            if (RequirementEntryIsSatisfied(requirement, _selectedGamePath))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(requirement.ReqAddress))
            {
                MessageBox.Show(
                    "This mod requires files/folders that are missing from the game installation: " +
                    string.Join(", ", requirement.FilesToCheck.Concat(requirement.FoldersToCheck)),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var requiredPackagePath = ResolveRequirementPackagePath(requirement.ReqAddress, baseModsFolder);
            if (string.IsNullOrWhiteSpace(requiredPackagePath) || !Directory.Exists(requiredPackagePath))
            {
                MessageBox.Show(
                    "Required package was not found for this mod: " + requirement.ReqAddress,
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var installTargetRoot = GameService.GetModLoaderFolder(_selectedGamePath);
            var targetPackageName = Path.GetFileName(requiredPackagePath);
            var targetDirectory = Path.Combine(installTargetRoot, targetPackageName);
            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, true);
            }

            await CopyPayloadWithProgressAsync(requiredPackagePath, targetDirectory);
            if (!RequirementEntryIsSatisfied(requirement, _selectedGamePath))
            {
                MessageBox.Show(
                    "The required package for this mod was installed, but the expected files/folders are still missing.",
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }

    private static bool RequirementEntryIsSatisfied(ModRequirementEntry requirement, string gameFolder)
    {
        if (string.IsNullOrWhiteSpace(gameFolder) || !Directory.Exists(gameFolder))
        {
            return false;
        }

        foreach (var path in requirement.FilesToCheck)
        {
            var candidate = path;
            if (!Path.IsPathRooted(candidate))
            {
                candidate = Path.Combine(gameFolder, candidate);
            }

            if (!File.Exists(candidate))
            {
                return false;
            }
        }

        foreach (var path in requirement.FoldersToCheck)
        {
            var candidate = path;
            if (!Path.IsPathRooted(candidate))
            {
                candidate = Path.Combine(gameFolder, candidate);
            }

            if (!Directory.Exists(candidate))
            {
                return false;
            }
        }

        return true;
    }

    private static string? ResolveRequirementPackagePath(string reqAddress, string baseModsFolder)
    {
        if (string.IsNullOrWhiteSpace(reqAddress))
        {
            return null;
        }

        var trimmed = reqAddress.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return Directory.Exists(trimmed) ? trimmed : null;
        }

        var direct = Path.Combine(baseModsFolder, trimmed);
        if (Directory.Exists(direct))
        {
            return direct;
        }

        var relativeWithinFolder = Path.Combine(baseModsFolder, "Scripts", trimmed);
        if (Directory.Exists(relativeWithinFolder))
        {
            return relativeWithinFolder;
        }

        var packageCandidate = Path.Combine(baseModsFolder, "Scripts", "A1-MyReqFiles", trimmed);
        if (Directory.Exists(packageCandidate))
        {
            return packageCandidate;
        }

        return Directory.Exists(trimmed) ? trimmed : null;
    }

    private async Task InstallSelectedModAsync()
    {
        try
        {
            if (!GameService.IsValidGameFolder(_selectedGamePath))
            {
                MessageBox.Show(_localizationService.GetString("GameFolderNotConfiguredMessage", "Please select a valid GTA San Andreas folder first."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedModName) || string.IsNullOrWhiteSpace(_selectedModPayloadPath) || !Directory.Exists(_selectedModPayloadPath))
            {
                MessageBox.Show(_localizationService.GetString("SelectModFirst", "Please select a valid mod folder first."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!await EnsureRequiredPackagesBeforeInstallAsync())
            {
                return;
            }

            if (!Directory.Exists(_selectedModPayloadPath) || Directory.EnumerateFileSystemEntries(_selectedModPayloadPath).Any() == false)
            {
                MessageBox.Show(_localizationService.GetString("ModFolderEmpty", "The selected mod folder is empty or unreadable."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!await EnsureDependenciesBeforeInstallAsync())
            {
                return;
            }

            var modJsonPath = Path.Combine(_selectedModPayloadPath, "mod.json");
            if (File.Exists(modJsonPath))
            {
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(modJsonPath));
                    var root = jsonDoc.RootElement;
                    var hasAnyProperties = root.ValueKind == System.Text.Json.JsonValueKind.Object && root.EnumerateObject().Any();
                    if (root.ValueKind != System.Text.Json.JsonValueKind.Object || !hasAnyProperties)
                    {
                        MessageBox.Show(_localizationService.GetString("ModJsonInvalid", "mod.json is malformed or contains unexpected content. The mod name still uses the folder name."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(_localizationService.GetString("ModJsonInvalid", "mod.json is malformed or contains unexpected content. The mod name still uses the folder name."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            _selectedModPackageRoot = string.IsNullOrWhiteSpace(_selectedModPackageRoot) ? _selectedModPayloadPath : _selectedModPackageRoot;
            _selectedModManifest ??= ModPackageService.ResolveManifest(_selectedModPackageRoot);

            if (_selectedModManifest.NormalizedType == "savesandmissions")
            {
                await InstallSaveOrDyomPackageAsync(_selectedModPayloadPath, _selectedModName, _selectedModManifest);
                return;
            }

            if (_selectedModManifest.NormalizedType == "missiondsl")
            {
                await InstallMissionDslPackageAsync(_selectedModPayloadPath, _selectedModName);
                return;
            }

            if (_selectedModManifest.IsSingleAssetPackage || _selectedModManifest.IsMultiAssetPackage)
            {
                var preservedSelection = _selectedAssetForInstall;

                if (_selectedAssetForInstall == null && !_selectedModManifest.IsMultiAssetPackage)
                {
                    _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
                    _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
                    PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
                }

                if (_selectedAssetForInstall == null && !_selectedModManifest.IsMultiAssetPackage)
                {
                    if (preservedSelection != null)
                    {
                        _selectedAssetForInstall = preservedSelection;
                    }

                    MessageBox.Show(_localizationService.GetString("NoAssetSelected", "Please select an asset before starting the installation."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
                _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
                PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);

                if (_selectedAssetForInstall == null && preservedSelection != null)
                {
                    _selectedAssetForInstall = preservedSelection;
                }

                if (_selectedAssetForInstall == null && !_selectedModManifest.IsMultiAssetPackage)
                {
                    MessageBox.Show(_localizationService.GetString("NoAssetSelected", "Please select an asset before starting the installation."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GoToStep(WizardStep.Step4);
                try
                {
                    await InstallTypedPackageAsync(_selectedModPayloadPath, _selectedModName, _selectedModPackageRoot, _selectedModManifest);
                    await ShowStep4LoadingTransitionAsync(GetCurrentStep5AssetType());
                    GoToStep(WizardStep.Step6);
                    return;
                }
                catch (Exception ex)
                {
                    var logPath = Path.Combine(AppContext.BaseDirectory, "mod-install-debug.log");
                    try
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now:O}] Step5 install branch failed:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
                    }
                    catch
                    {
                    }

                    MessageBox.Show($"Installation failed.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}More details were written to:{Environment.NewLine}{logPath}", _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var modLoaderFolder = GameService.GetModLoaderFolder(_selectedGamePath);
            var targetDir = Path.Combine(modLoaderFolder, _selectedModName);

            if (Directory.Exists(targetDir))
            {
                var result = MessageBox.Show(string.Format(_localizationService.GetString("DuplicateModPrompt", "A mod named '{0}' already exists. Replace it?"), _selectedModName), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    return;
                }

                Directory.Delete(targetDir, true);
            }

            _installPaths = Directory.GetFiles(_selectedModPayloadPath, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
            _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);

            GoToStep(WizardStep.Step4);
            await CopyPayloadWithProgressAsync(_selectedModPayloadPath, targetDir);
            ModLoaderService.RecordInstallation(_selectedModName, _selectedModPayloadPath, targetDir);
            await ShowStep4LoadingTransitionAsync(GetCurrentStep5AssetType());
            GoToStep(WizardStep.Step5);
            _selectedReadmePath = FindReadmeFile(targetDir);
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "mod-install-debug.log");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:O}] InstallSelectedModAsync failed:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }

            MessageBox.Show($"Installation failed.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}More details were written to:{Environment.NewLine}{logPath}", _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CompleteAssetPreparationStepAsync()
    {
        if (_wizardPanels.TryGetValue(WizardStep.Step4, out var panel))
        {
            var progressBar = panel.Controls.OfType<ProgressBar>().FirstOrDefault();
            var statusLabel = panel.Controls.OfType<Label>().FirstOrDefault(label => label.Name == "ProgressStatus");
            if (progressBar != null)
            {
                progressBar.Value = 100;
            }

            if (statusLabel != null)
            {
                statusLabel.Text = _localizationService.GetString("Installing", "Installing") + " " + _selectedModName + " - 100%";
            }
        }

        await Task.Delay(150);
    }

    private void RefreshWizardImageGallery(WizardStep step)
    {
        if (step == WizardStep.Step4)
        {
            return;
        }

        if (!_wizardPanels.TryGetValue(step, out var panel))
        {
            return;
        }

        var galleryName = step == WizardStep.Step4 ? "InstallImageGallery" : "CompletionImageGallery";
        var gallery = panel.Controls.Find(galleryName, false).FirstOrDefault() as FlowLayoutPanel;
        if (gallery == null)
        {
            return;
        }

        var validImages = _selectedImageFiles
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        gallery.Visible = validImages.Count > 0;

        foreach (Control control in gallery.Controls)
        {
            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
            }

            control.Dispose();
        }

        gallery.Controls.Clear();

        if (step == WizardStep.Step6 && validImages.Count > 2)
        {
            var slideHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(255, 255, 255), Padding = new Padding(8) };
            var preview = new MediaPreviewControl { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 247, 250), Cursor = Cursors.Hand };
            var controlsPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false };
            var prev = new Button { Text = "◀", Width = 70, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
            var next = new Button { Text = "▶", Width = 70, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
            var index = 0;

            void RenderCurrentImage()
            {
                if (validImages.Count == 0)
                {
                    return;
                }

                var imagePath = validImages[index];
                preview.LoadMedia(imagePath);
            }

            prev.Click += (_, _) =>
            {
                index = (index - 1 + validImages.Count) % validImages.Count;
                RenderCurrentImage();
            };

            next.Click += (_, _) =>
            {
                index = (index + 1) % validImages.Count;
                RenderCurrentImage();
            };

            preview.RightClicked += (_, _) => OpenFullImageViewer(validImages, index, validImages[index]);
            controlsPanel.Controls.Add(prev);
            controlsPanel.Controls.Add(next);
            slideHost.Controls.Add(preview);
            slideHost.Controls.Add(controlsPanel);
            gallery.Controls.Add(slideHost);

            preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            controlsPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            RenderCurrentImage();
            return;
        }

        foreach (var imageFile in validImages)
        {
            try
            {
                var imageList = validImages;
                var preview = new MediaPreviewControl
                {
                    Width = 280,
                    Height = 220,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 247, 250),
                    Margin = new Padding(0, 0, 14, 14),
                    Cursor = Cursors.Hand
                };
                if (!preview.LoadMedia(imageFile))
                {
                    preview.Dispose();
                    continue;
                }

                preview.RightClicked += (_, _) =>
                {
                    var selectedIndex = imageList.FindIndex(path => string.Equals(path, imageFile, StringComparison.OrdinalIgnoreCase));
                    if (selectedIndex < 0)
                    {
                        selectedIndex = 0;
                    }

                    OpenFullImageViewer(imageList, selectedIndex, imageFile);
                };

                gallery.Controls.Add(preview);
            }
            catch
            {
                // Ignore image formats that Windows cannot decode.
            }
        }
    }

    private static bool IsMetadataOrNonInstallableFile(string path)
    {
        return ModPackageService.IsMetadataOrNonInstallableFile(path);
    }

    private async Task CopyPayloadWithProgressAsync(string sourceDir, string targetDir)
    {
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Where(path => !IsMetadataOrNonInstallableFile(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var total = files.Count;
        _step4FileProgress.Clear();

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var relative = Path.GetRelativePath(sourceDir, file).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var destination = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);

            var percent = Math.Min(100, (int)((i + 1) * 100d / Math.Max(1, total)));
            if (!_step4FileProgress.TryGetValue(relative, out var values))
            {
                values = new List<int>();
                _step4FileProgress[relative] = values;
            }

            if (!values.Contains(percent))
            {
                values.Add(percent);
            }

            if (_wizardPanels.TryGetValue(WizardStep.Step4, out var panel))
            {
                var progressPanel = panel.Controls.OfType<InstallProgressPanel>().FirstOrDefault();
                if (progressPanel != null)
                {
                    progressPanel.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + _selectedModName + " - " + percent + "%");
                    progressPanel.UpdateProgress(percent, Path.GetFileName(file));
                }

                var root = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4PreviewRoot");
                var listBox = root?.Controls.OfType<ListBox>().FirstOrDefault(control => control.Name == "Step4FileList");
                if (listBox != null)
                {
                    listBox.Items.Clear();
                    foreach (var item in files.Select(path => Path.GetRelativePath(sourceDir, path).Replace('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (_step4FileProgress.TryGetValue(item, out var statusValues) && statusValues.Count > 0)
                        {
                            var latest = statusValues[^1];
                            listBox.Items.Add(latest >= 100
                                ? item
                                : $"{item} ({string.Join(" ", statusValues.Select(value => $"{value}%"))})");
                        }
                        else
                        {
                            listBox.Items.Add(item);
                        }
                    }
                }
            }

            await Task.Delay(30);
        }
    }



    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step4/MainForm.Step4InstallLogic.cs
    // -------------------------------------------------------------------------
    
}
