using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GtaSaModManager.Models;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.ModLibrary.cs
    // -------------------------------------------------------------------------
    private void EnsureValidatedApplicationSetup()
    {
        if (!GameService.IsValidGameFolder(_settings.GamePath))
        {
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"), _settings.GamePath);
            if (GameService.IsValidGameFolder(selected))
            {
                _settings.GamePath = selected;
                _settings.GameExecutableName = "gta_sa.exe";
                GameService.EnsureModLoaderFolder(selected);
                _settingsService.Save(_settings);
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.ModSourceFolder) || !Directory.Exists(_settings.ModSourceFolder))
        {
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"), _settings.ModSourceFolder);
            if (!string.IsNullOrWhiteSpace(selected) && Directory.Exists(selected))
            {
                _settings.ModSourceFolder = selected;
                _settingsService.Save(_settings);
            }
        }
    }

    private string PromptForFolderSelection(string description, string? initialFolder = null)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = description,
            RootFolder = Environment.SpecialFolder.MyComputer,
            ShowNewFolderButton = true
        };
        var exactInitialFolder = NormalizeExistingDirectory(initialFolder);
        if (!string.IsNullOrWhiteSpace(exactInitialFolder))
        {
            try
            {
                folderDialog.SelectedPath = exactInitialFolder;
            }
            catch
            {
                // ignore any platform-specific errors setting SelectedPath
            }
        }

        return folderDialog.ShowDialog(this) == DialogResult.OK ? folderDialog.SelectedPath : string.Empty;
    }

    private string PromptForModFolderSelection(string description, string? initialFolder)
    {
        using var folderContentsDialog = new OpenFileDialog
        {
            Title = description,
            InitialDirectory = NormalizeExistingDirectory(initialFolder) ?? string.Empty,
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select this folder",
            Filter = "Folders|*.folder"
        };

        if (folderContentsDialog.ShowDialog(this) != DialogResult.OK)
        {
            return string.Empty;
        }

        var selectedDirectory = Path.GetDirectoryName(folderContentsDialog.FileName);
        return NormalizeExistingDirectory(selectedDirectory) ?? string.Empty;
    }

    private static string? NormalizeExistingDirectory(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return null;
        }

        return new DirectoryInfo(directoryPath).FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private void LoadSettingsIntoUi()
    {
        _selectedGamePath = IsCachedGameFolderValid() ? _settings.GamePath ?? string.Empty : string.Empty;
        _selectedModSourcePath = !string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder)
            ? _settings.ModSourceFolder
            : string.Empty;

        if (GamePathTextBox != null)
        {
            GamePathTextBox.Text = _selectedGamePath;
        }

        if (ModLibraryPathTextBox != null)
        {
            ModLibraryPathTextBox.Text = _selectedModSourcePath;
        }

        if (!string.IsNullOrWhiteSpace(_selectedGamePath) && GameService.IsValidGameFolder(_selectedGamePath))
        {
            GameService.EnsureModLoaderFolder(_selectedGamePath);
            if (GameStatusValueLabel != null)
            {
                GameStatusValueLabel.Text = _localizationService.GetString("Configured", "Configured");
            }

            if (GameFolderNotConfiguredLabel != null)
            {
                GameFolderNotConfiguredLabel.Visible = false;
            }
        }
        else
        {
            if (GameStatusValueLabel != null)
            {
                GameStatusValueLabel.Text = _localizationService.GetString("NotConfigured", "Not configured");
            }

            if (GameFolderNotConfiguredLabel != null)
            {
                GameFolderNotConfiguredLabel.Visible = true;
            }
        }

        if (OpenGameFolderButton != null)
        {
            OpenGameFolderButton.Enabled = GameService.IsValidGameFolder(_selectedGamePath);
        }

        if (RunGameButton != null)
        {
            RunGameButton.Enabled = GameService.IsValidGameFolder(_selectedGamePath);
        }

        if (ModLibraryOpenButton != null)
        {
            ModLibraryOpenButton.Enabled = Directory.Exists(_selectedModSourcePath);
        }
    }

    private void RefreshModList()
    {
        _ = RefreshModListAsync();
    }

    private async Task RefreshModListAsync()
    {
        if (ModLoaderFlowPanel == null)
        {
            return;
        }

        var gamePath = _selectedGamePath;
        var isValidGameFolder = !string.IsNullOrWhiteSpace(gamePath) && GameService.IsValidGameFolder(gamePath);
        var mods = isValidGameFolder
            ? await Task.Run(() => ModLoaderService.GetInstalledMods(gamePath))
            : new List<ModInfo>();

        if (ModLoaderFlowPanel.IsDisposed)
        {
            return;
        }

        ModLoaderFlowPanel.Controls.Clear();
        if (!isValidGameFolder)
        {
            var empty = new Label
            {
                Text = _localizationService.GetString("NoModsInstalled", "No ModLoader mods installed yet."),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.Gray
            };

            ModLoaderFlowPanel.Controls.Add(empty);
            return;
        }

        foreach (var mod in mods)
        {
            var card = new Panel { Width = 260, Height = 270, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10), Margin = new Padding(10) };
            var preview = new PictureBox { Width = 220, Height = 110, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            var name = new Label { Text = mod.Name, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var status = new Label { Text = mod.Status, AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            var path = new Label { Text = mod.FolderPath, AutoSize = true, MaximumSize = new Size(220, 60), Font = new Font("Segoe UI", 8.5F) };
            var readme = new Button { Text = _localizationService.GetString("ViewReadme", "View README"), Width = 120, Height = 32, Visible = !string.IsNullOrWhiteSpace(mod.ReadmePath) };
            var uninstall = new Button { Text = _localizationService.GetString("UninstallMod", "Uninstall"), Width = 90, Height = 32 };

            if (File.Exists(mod.PreviewPath))
            {
                preview.Image = TryLoadBitmap(mod.PreviewPath);
            }

            readme.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(mod.ReadmePath) && File.Exists(mod.ReadmePath))
                {
                    ShowReadmeDialog(mod.ReadmePath);
                }
            };

            uninstall.Click += (_, _) =>
            {
                if (MessageBox.Show(string.Format(_localizationService.GetString("ConfirmUninstallMod", "Are you sure you want to uninstall '{0}'?"), mod.Name), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                var success = ModLoaderService.TryUninstallInstalledMod(mod.Name, mod.FolderPath, gamePath, "putinmodloader");
                if (!success)
                {
                    MessageBox.Show(_localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RefreshModList();
            };

            card.Controls.Add(preview);
            card.Controls.Add(name);
            card.Controls.Add(status);
            card.Controls.Add(path);
            card.Controls.Add(readme);
            card.Controls.Add(uninstall);
            preview.Location = new Point(10, 10);
            name.Location = new Point(10, 128);
            status.Location = new Point(10, 158);
            path.Location = new Point(10, 180);
            readme.Location = new Point(10, 210);
            uninstall.Location = new Point(140, 210);
            ModLoaderFlowPanel.Controls.Add(card);
        }
    }

    private void RefreshModLibrary()
    {
        _ = RefreshModLibraryAsync();
    }

    private async Task RefreshModLibraryAsync()
    {
        if (ModLibraryListBox == null)
        {
            return;
        }

        var sourcePath = _selectedModSourcePath;
        var entries = await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                return new List<string>();
            }

            return ModPackageService.DiscoverModPackages(sourcePath)
                .Select(item => item.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

        if (ModLibraryListBox.IsDisposed)
        {
            return;
        }

        ModLibraryListBox.Items.Clear();
        if (entries.Count == 0)
        {
            ModLibraryListBox.Items.Add(_localizationService.GetString("NoModPackagesInLibrary", "No mod packages found in this library yet."));
            return;
        }

        foreach (var entry in entries)
        {
            ModLibraryListBox.Items.Add(entry);
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.ModLibrary.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.ModLibrary.cs
    // -------------------------------------------------------------------------
    private static string GetGtaUserFilesDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var userFilesPath = Path.Combine(documentsPath, "GTA San Andreas User Files");
        Directory.CreateDirectory(userFilesPath);
        return userFilesPath;
    }

    private static bool IsDyomPackage(string packageRoot)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
        {
            return false;
        }

        return Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Any(file => Regex.IsMatch(Path.GetFileName(file), "^DYOM\\d+\\.dat$", RegexOptions.IgnoreCase));
    }

    private static bool IsSaveSlotFile(string fileName, bool isDyom)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return isDyom
            ? Regex.IsMatch(fileName, "^DYOM\\d+\\.dat$", RegexOptions.IgnoreCase)
            : Regex.IsMatch(fileName, "^GTASAsf\\d+\\.b$", RegexOptions.IgnoreCase);
    }

    private static HashSet<int> GetOccupiedSlots(string userFilesRoot, bool isDyom)
    {
        if (string.IsNullOrWhiteSpace(userFilesRoot) || !Directory.Exists(userFilesRoot))
        {
            return new HashSet<int>();
        }

        var occupied = new HashSet<int>();
        foreach (var file in Directory.GetFiles(userFilesRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            var match = Regex.Match(fileName, isDyom ? "^DYOM(\\d+)\\.dat$" : "^GTASAsf(\\d+)\\.b$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var slot))
            {
                occupied.Add(slot);
            }
        }

        return occupied;
    }

    private static string GetNextAvailableSlotTargetName(string userFilesRoot, bool isDyom, HashSet<int>? reservedSlots = null)
    {
        var occupied = GetOccupiedSlots(userFilesRoot, isDyom);
        if (reservedSlots != null)
        {
            foreach (var reservedSlot in reservedSlots)
            {
                occupied.Add(reservedSlot);
            }
        }

        for (var slot = 1; slot <= 8; slot++)
        {
            if (!occupied.Contains(slot))
            {
                return isDyom ? $"DYOM{slot}.dat" : $"GTASAsf{slot}.b";
            }
        }

        return isDyom ? "DYOM1.dat" : "GTASAsf1.b";
    }

    private async Task InstallSaveOrDyomPackageAsync(string packageRoot, string modName, GtaSaModManager.Models.ModManifest manifest)
    {
        var userFilesRoot = GetGtaUserFilesDirectory();
        var isDyom = IsDyomPackage(packageRoot);
        var packageFiles = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Where(path => IsSaveSlotFile(Path.GetFileName(path), isDyom))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (packageFiles.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected package does not contain a valid save or DYOM file."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (isDyom)
        {
            var dyomDependencyRoot = Path.Combine(_settings.ModSourceFolder ?? _selectedGamePath, "Scripts", "DYOM", "DYOM v8.2");
            if (Directory.Exists(dyomDependencyRoot))
            {
                var destination = Path.Combine(userFilesRoot, "DYOM v8.2");
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }

                await ModPackageService.CopyDirectoryAsync(dyomDependencyRoot, destination, null);
            }
            else
            {
                MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "DYOM dependency was not found in the Base Mods folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        var trashRoot = Path.Combine(userFilesRoot, ".trash");
        Directory.CreateDirectory(trashRoot);
        var trashInfo = new DirectoryInfo(trashRoot);
        trashInfo.Attributes |= FileAttributes.Hidden;

        var installedSlots = new List<int>();
        foreach (var sourceFile in packageFiles)
        {
            var fileName = Path.GetFileName(sourceFile);
            var destinationName = GetNextAvailableSlotTargetName(userFilesRoot, isDyom, new HashSet<int>(installedSlots));
            var destinationPath = Path.Combine(userFilesRoot, destinationName);
            var targetSlot = int.TryParse(Regex.Match(destinationName, isDyom ? "^DYOM(\\d+)\\.dat$" : "^GTASAsf(\\d+)\\.b$", RegexOptions.IgnoreCase).Groups[1].Value, out var slot)
                ? slot
                : 1;

            if (File.Exists(destinationPath))
            {
                var archivedPath = Path.Combine(trashRoot, fileName);
                if (File.Exists(archivedPath))
                {
                    File.Delete(archivedPath);
                }

                File.Move(destinationPath, archivedPath);
            }

            File.Copy(sourceFile, destinationPath, true);
            installedSlots.Add(targetSlot);
        }

        var slotLabel = isDyom ? "DYOM" : "save";
        var installedCount = installedSlots.Count;
        var summary = installedCount == 1
            ? $"{slotLabel} slot {installedSlots[0]} installed successfully."
            : $"{installedCount} {slotLabel} slots installed successfully: {string.Join(", ", installedSlots)}.";

        MessageBox.Show(summary, _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task InstallMissionDslPackageAsync(string packageRoot, string modName)
    {
        var userFilesRoot = GetGtaUserFilesDirectory();
        var dyomDependencyRoot = Path.Combine(_settings.ModSourceFolder ?? _selectedGamePath, "Scripts", "DYOM", "DYOM v8.2");
        if (!Directory.Exists(dyomDependencyRoot))
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "DYOM dependency was not found in the Base Mods folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var destinationRoot = Path.Combine(userFilesRoot, "DYOM v8.2");
        if (Directory.Exists(destinationRoot))
        {
            Directory.Delete(destinationRoot, true);
        }

        await ModPackageService.CopyDirectoryAsync(dyomDependencyRoot, destinationRoot, null);

        var dslRoot = Path.Combine(userFilesRoot, "DSL");
        if (Directory.Exists(dslRoot))
        {
            foreach (var file in Directory.GetFiles(dslRoot, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }

            foreach (var directory in Directory.GetDirectories(dslRoot, "*", SearchOption.AllDirectories).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
            {
                Directory.Delete(directory, true);
            }
        }

        Directory.CreateDirectory(dslRoot);
        await ModPackageService.CopyDirectoryAsync(packageRoot, dslRoot, null, excludeMetadataFiles: true);
        GoToStep(WizardStep.Step6);
    }

    private async Task InstallTypedPackageAsync(string payloadPath, string modName, string packageRoot, GtaSaModManager.Models.ModManifest manifest)
    {
        var targetRoot = manifest.NormalizedType switch
        {
            "putinmodloader" or "vehicleandskinandweapon" or "vehiclesandskinsandweapons" => Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), modName),
            "putincleo" or "putingamefolder" or "putandreplace" or "putandreplaces" => _selectedGamePath,
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(targetRoot))
        {
            return;
        }

        if (manifest.NormalizedType == "putinmodloader" && Directory.Exists(targetRoot))
        {
            var result = MessageBox.Show(string.Format(_localizationService.GetString("DuplicateModPrompt", "A mod named '{0}' already exists. Replace it?"), modName), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            Directory.Delete(targetRoot, true);
        }

        var selectedAssetList = GetSelectedAssetListForInstall(manifest, payloadPath);
        if ((manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons") && selectedAssetList.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "No matching asset was detected in the package."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var sourceModelName = DetectSourceModelName(payloadPath);
        var packageFiles = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".dff" or ".txd"
                && string.Equals(Path.GetFileNameWithoutExtension(path), sourceModelName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedTarget = _selectedAssetForInstall ?? selectedAssetList.FirstOrDefault() ?? _assetCatalogService.LoadAssets()
            .FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase));

        if (packageFiles.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain installable files."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var replacementTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (manifest.NormalizedType == "putandreplace")
        {
            foreach (var replacement in ModPackageService.ReadReplacementEntries(packageRoot))
            {
                var sourcePath = GetSafePackagePath(payloadPath, replacement.Source);
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("Replacement source was not found: " + replacement.Source, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                replacementTargets[sourcePath] = GetSafeGamePath(replacement.Target);
            }
        }
        else if (manifest.NormalizedType is "putandreplaces" or "putingamefolder")
        {
            foreach (var sourcePath in packageFiles)
            {
                var relativePath = Path.GetRelativePath(payloadPath, sourcePath);
                var destinationPath = GetSafeGamePath(relativePath);
                if (manifest.NormalizedType == "putandreplaces" && File.Exists(destinationPath)
                    || manifest.NormalizedType == "putingamefolder" && File.Exists(destinationPath))
                {
                    replacementTargets[sourcePath] = destinationPath;
                }
            }
        }

        var backupPlan = replacementTargets.Count > 0
            ? BackupStorageService.CreatePlan(_selectedGamePath, modName, replacementTargets.Values)
            : new BackupStoragePlan { HasBackup = true };
        if (replacementTargets.Count > 0 && !backupPlan.HasBackup)
        {
            var chooseAlternative = MessageBox.Show(
                "The game drive and drive C do not have enough space for the backup.\n\nWould you like to choose another location?",
                _appName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (chooseAlternative == DialogResult.Yes)
            {
                var alternativeRoot = PromptForFolderSelection("Choose another backup location", _selectedGamePath);
                if (string.IsNullOrWhiteSpace(alternativeRoot))
                {
                    return;
                }

                backupPlan = BackupStorageService.CreatePlan(_selectedGamePath, modName, replacementTargets.Values, alternativeRoot);
                if (!backupPlan.HasBackup)
                {
                    MessageBox.Show(backupPlan.ErrorMessage ?? "The selected location does not have enough free space.", _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (MessageBox.Show("No backup will be created. Continue?", _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }
        }

        var step4Panel = _wizardPanels.TryGetValue(WizardStep.Step4, out var step4RootPanel)
            ? step4RootPanel
            : null;
        var previewRoot = step4Panel?.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4PreviewRoot");
        var progressPanel = new InstallProgressPanel(_localizationService, modName);
        progressPanel.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName);

        if (previewRoot != null)
        {
            previewRoot.Controls.Clear();
            previewRoot.Controls.Add(progressPanel);
            progressPanel.Dock = DockStyle.Fill;
            progressPanel.BringToFront();
        }

        try
        {
            var records = new List<ReplaceInstallationRecord>();
            for (var index = 0; index < packageFiles.Count; index++)
            {
                var sourcePath = packageFiles[index];
                var extension = Path.GetExtension(sourcePath);
                var originalName = Path.GetFileNameWithoutExtension(sourcePath);
                var selectedAsset = selectedTarget is not null && !string.IsNullOrWhiteSpace(selectedTarget.NameFile)
                    ? selectedTarget
                    : selectedAssetList.FirstOrDefault(asset => string.Equals(asset.NameFile, originalName, StringComparison.OrdinalIgnoreCase));
                var destinationName = selectedAsset?.NameFile ?? originalName;
                var relativePath = manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons"
                    ? destinationName + extension
                    : Path.GetRelativePath(payloadPath, sourcePath);
                var destinationPath = replacementTargets.TryGetValue(sourcePath, out var replacementTarget)
                    ? replacementTarget
                    : GetSafeGamePath(manifest.NormalizedType is "putinmodloader" or "vehicleandskinandweapon" or "vehiclesandskinsandweapons"
                        ? Path.Combine("modloader", modName, relativePath)
                        : relativePath);

                if (replacementTargets.ContainsKey(sourcePath) && File.Exists(destinationPath) && backupPlan.HasBackup)
                {
                    var backupFilePath = ModPackageService.BackupOriginalFileForReplacement(_selectedGamePath, destinationPath, modName, backupPlan.BackupRoot);
                    records.Add(new ReplaceInstallationRecord
                    {
                        BackupId = Guid.NewGuid().ToString("N"),
                        ModName = modName,
                        GameFolder = _selectedGamePath,
                        OriginalFilePath = destinationPath,
                        BackupFilePath = backupFilePath,
                        InstalledModFile = sourcePath,
                        InstallationType = manifest.NormalizedType,
                        ReplacementSucceeded = true,
                        OriginalBackupAvailable = File.Exists(backupFilePath)
                    });
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath, true);
                var percent = (int)((index + 1) * 100d / Math.Max(1, packageFiles.Count));
                progressPanel.UpdateProgress(percent, sourcePath);
                await Task.Yield();
            }

            foreach (var record in records)
            {
                ModPackageService.RecordReplacementInstallation(record);
            }

            if (manifest.NormalizedType == "putinmodloader")
            {
                ModLoaderService.RecordInstallation(modName, packageRoot, targetRoot);
            }
            else if (manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons")
            {
                ModLoaderService.RecordInstallation(modName, packageRoot, targetRoot);
            }

            progressPanel.Complete();
            RefreshModList();
        }
        catch (Exception ex)
        {
            progressPanel.Fail();
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (previewRoot != null && progressPanel.Parent == previewRoot)
            {
                previewRoot.Controls.Remove(progressPanel);
                progressPanel.Dispose();
            }
        }
    }

    private string GetSafePackagePath(string packageRoot, string relativePath)
    {
        return GetSafePath(packageRoot, relativePath, "Package path");
    }

    private string GetSafeGamePath(string relativePath)
    {
        return GetSafePath(_selectedGamePath, relativePath, "Game path");
    }

    private static string GetSafePath(string root, string relativePath, string label)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException(label + " must be a relative path.");
        }

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(label + " escapes its root folder.");
        }

        return fullPath;
    }

    private async Task InstallReplacingPackageAsync(string payloadPath, string modName, string packageRoot)
    {
        var filesToReplace = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (filesToReplace.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected replacement package does not contain any files to replace."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var backupPlan = BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToReplace);
        if (!backupPlan.HasBackup)
        {
            var chooseAlternative = MessageBox.Show(
                "The game drive and drive C do not have enough space for the backup.\n\nWould you like to choose another location?",
                _appName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (chooseAlternative == DialogResult.Yes)
            {
                var alternativeRoot = PromptForFolderSelection("Choose another backup location", _selectedGamePath);
                if (string.IsNullOrWhiteSpace(alternativeRoot))
                {
                    return;
                }

                backupPlan = BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToReplace, alternativeRoot);
                if (!backupPlan.HasBackup)
                {
                    MessageBox.Show(backupPlan.ErrorMessage ?? "The selected location does not have enough free space.", _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                var installWithoutBackup = MessageBox.Show(
                    "No backup will be created. Continue replacing the original files?",
                    _appName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (installWithoutBackup != DialogResult.Yes)
                {
                    return;
                }
            }
        }

        var step4Panel = _wizardPanels.TryGetValue(WizardStep.Step4, out var step4RootPanel)
            ? step4RootPanel
            : null;
        var previewRoot = step4Panel?.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4PreviewRoot");
        var progressPanel = new InstallProgressPanel(_localizationService, modName);
        progressPanel.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName + " (Replacing)");

        if (previewRoot != null)
        {
            previewRoot.Controls.Clear();
            previewRoot.Controls.Add(progressPanel);
            progressPanel.Dock = DockStyle.Fill;
            progressPanel.BringToFront();
        }

        var records = new List<ReplaceInstallationRecord>();

        try
        {
            for (var i = 0; i < filesToReplace.Count; i++)
            {
                var sourceFile = filesToReplace[i];
                var relativePath = Path.GetRelativePath(payloadPath, sourceFile)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var destinationFile = Path.Combine(_selectedGamePath, relativePath);
                var destinationDirectory = Path.GetDirectoryName(destinationFile);

                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var backupFilePath = string.Empty;
                if (File.Exists(destinationFile) && backupPlan.HasBackup)
                {
                    backupFilePath = ModPackageService.BackupOriginalFileForReplacement(_selectedGamePath, destinationFile, modName, backupPlan.BackupRoot);
                }

                File.Copy(sourceFile, destinationFile, true);

                var record = new ReplaceInstallationRecord
                {
                    BackupId = Guid.NewGuid().ToString("N"),
                    ModName = modName,
                    GameFolder = _selectedGamePath,
                    OriginalFilePath = destinationFile,
                    BackupFilePath = backupFilePath,
                    InstalledModFile = sourceFile,
                    InstallationDate = DateTime.UtcNow,
                    InstallationType = "Replacing",
                    ReplacementSucceeded = true,
                    OriginalBackupAvailable = !string.IsNullOrWhiteSpace(backupFilePath) && File.Exists(backupFilePath),
                    Restored = false,
                    StatusMessage = "Original file backed up and replaced."
                };

                records.Add(record);
                ModPackageService.RecordReplacementInstallation(record);

                var percent = (int)((i + 1) * 100d / Math.Max(1, filesToReplace.Count));
                progressPanel.UpdateProgress(percent, sourceFile);
                await Task.Delay(30);
            }

            progressPanel.Complete();
            GoToStep(WizardStep.Step6);
        }
        catch (Exception ex)
        {
            progressPanel.Fail();
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (previewRoot != null && progressPanel.Parent == previewRoot)
            {
                previewRoot.Controls.Remove(progressPanel);
                progressPanel.Dispose();
            }
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.ModLibrary.cs
    // -------------------------------------------------------------------------
}
