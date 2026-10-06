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

        var selectedPath = folderContentsDialog.FileName;
        var selectedDirectory = Directory.Exists(selectedPath)
            ? selectedPath
            : Path.GetDirectoryName(selectedPath);
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

            uninstall.Click += async (_, _) =>
            {
                if (MessageBox.Show(string.Format(_localizationService.GetString("ConfirmUninstallMod", "Are you sure you want to uninstall '{0}'?"), mod.Name), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                GoToStep(WizardStep.Step4);
                bool success;
                try
                {
                    success = await UninstallInstalledFolderWithProgressAsync(
                        mod.Name,
                        mod.FolderPath,
                        gamePath,
                        "putinmodloader");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        _localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled.") + " " + ex.Message,
                        _appName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

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

        var dyomDependencyRoot = Path.Combine(_settings.ModSourceFolder ?? _selectedGamePath, "Scripts", "DYOM", "DYOM v8.2");
        var dependencyFiles = new List<string>();
        if (isDyom)
        {
            if (!Directory.Exists(dyomDependencyRoot))
            {
                MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "DYOM dependency was not found in the Base Mods folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dependencyFiles = Directory.GetFiles(dyomDependencyRoot, "*", SearchOption.AllDirectories).ToList();
        }

        var progressFiles = dependencyFiles
            .Select(path => (path, Path.Combine("DYOM v8.2", Path.GetRelativePath(dyomDependencyRoot, path))))
            .Concat(packageFiles.Select(path => (path, Path.GetRelativePath(packageRoot, path))))
            .ToList();
        var progressTitle = _localizationService.GetString("Installing", "Installing") + " " + modName;
        BeginStep4Progress(progressTitle, progressFiles);

        if (isDyom)
        {
            var destination = Path.Combine(userFilesRoot, "DYOM v8.2");
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            var dependencyDisplayNames = dependencyFiles
                .Select(path => Path.Combine("DYOM v8.2", Path.GetRelativePath(dyomDependencyRoot, path)))
                .ToList();
            await CopyDirectoryWithStep4ProgressAsync(
                dyomDependencyRoot,
                destination,
                dependencyFiles,
                0,
                dependencyDisplayNames);
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

            var relativeName = Path.GetRelativePath(packageRoot, sourceFile);
            await CopyStep4FileAsync(
                dependencyFiles.Count + packageFiles.IndexOf(sourceFile),
                sourceFile,
                destinationPath,
                relativeName);
            installedSlots.Add(targetSlot);
        }

        CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
        var slotLabel = isDyom ? "DYOM" : "save";
        var installedCount = installedSlots.Count;
        var summary = installedCount == 1
            ? $"{slotLabel} slot {installedSlots[0]} installed successfully."
            : $"{installedCount} {slotLabel} slots installed successfully: {string.Join(", ", installedSlots)}.";

        MessageBox.Show(summary, _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task InstallMissionDslPackageAsync(
        string packageRoot,
        string modName,
        bool recordInstallation = true,
        List<string>? installedFilesOutput = null)
    {
        var userFilesRoot = GetGtaUserFilesDirectory();
        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;
        IReadOnlyList<UserFilesInstallCopy> userFileAdditions;
        try
        {
            userFileAdditions = UserFilesInstallService.CreateCopyPlan(
                _selectedModManifest?.AddToUserFile,
                Directory.Exists(_selectedModPackageRoot) ? _selectedModPackageRoot : packageRoot,
                baseModsFolder,
                userFilesRoot);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show("Could not resolve addToUserFile entries: " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var userFileSourcePaths = userFileAdditions
            .Select(copy => Path.GetFullPath(copy.SourcePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dslRoot = Path.Combine(userFilesRoot, "DSL");
        var installDyomDependency = !Directory.Exists(dslRoot);
        var dyomDependencyRoot = Path.Combine(baseModsFolder, "Scripts", "DYOM", "DYOM v8.1");
        if (installDyomDependency && !Directory.Exists(dyomDependencyRoot))
        {
            MessageBox.Show(
                "DYOM v8.1 dependency was not found in the Base Mods folder: " + dyomDependencyRoot,
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var packageFiles = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Where(file => !ModPackageService.IsMetadataOrNonInstallableFile(file, packageRoot))
            .Where(file => !userFileSourcePaths.Contains(Path.GetFullPath(file)))
            .ToList();
        if (packageFiles.Count == 0)
        {
            MessageBox.Show(
                _localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain installable files."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var dependencyFiles = installDyomDependency
            ? Directory.GetFiles(dyomDependencyRoot, "*", SearchOption.AllDirectories).ToList()
            : new List<string>();
        var progressFiles = dependencyFiles
            .Select(path => (path, Path.Combine("DYOM v8.1", Path.GetRelativePath(dyomDependencyRoot, path))))
            .Concat(packageFiles.Select(path => (path, Path.Combine("DSL", Path.GetRelativePath(packageRoot, path)))))
            .Concat(userFileAdditions.Select(copy => (
                copy.SourcePath,
                Path.GetRelativePath(userFilesRoot, copy.DestinationPath))))
            .ToList();
        var progressTitle = _localizationService.GetString("Installing", "Installing") + " " + modName;
        BeginStep4Progress(progressTitle, progressFiles);

        if (installDyomDependency)
        {
            var dependencyDestination = Path.Combine(userFilesRoot, "DYOM v8.1");
            if (Directory.Exists(dependencyDestination))
            {
                Directory.Delete(dependencyDestination, true);
            }

            var dependencyDisplayNames = dependencyFiles
                .Select(path => Path.Combine("DYOM v8.1", Path.GetRelativePath(dyomDependencyRoot, path)))
                .ToList();
            await CopyDirectoryWithStep4ProgressAsync(
                dyomDependencyRoot,
                dependencyDestination,
                dependencyFiles,
                0,
                dependencyDisplayNames);
        }

        if (Directory.Exists(dslRoot))
        {
            Directory.Delete(dslRoot, recursive: true);
        }

        Directory.CreateDirectory(dslRoot);
        var packageDisplayNames = packageFiles
            .Select(path => Path.Combine("DSL", Path.GetRelativePath(packageRoot, path)))
            .ToList();
        await CopyDirectoryWithStep4ProgressAsync(
            packageRoot,
            dslRoot,
            packageFiles,
            dependencyFiles.Count,
            packageDisplayNames);
        for (var index = 0; index < userFileAdditions.Count; index++)
        {
            var addition = userFileAdditions[index];
            await CopyStep4FileAsync(
                dependencyFiles.Count + packageFiles.Count + index,
                addition.SourcePath,
                addition.DestinationPath,
                Path.GetRelativePath(userFilesRoot, addition.DestinationPath));
        }

        var installedFiles = packageFiles
            .Select(path => Path.Combine(dslRoot, Path.GetRelativePath(packageRoot, path)))
            .Concat(userFileAdditions.Select(copy => copy.DestinationPath))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        installedFilesOutput?.AddRange(installedFiles);
        if (recordInstallation)
        {
            ModLoaderService.RecordUserFilesInstallation(
                _selectedGamePath,
                modName,
                "missiondsl",
                installedFiles);
        }
        CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
        if (_isMixedInstallActive)
        {
            await CompleteCurrentMixedPartAsync(installedFiles);
        }
        else
        {
            GoToStep(WizardStep.Step6);
        }
    }

    private async Task<bool> InstallPutInCleoPackageAsync(
        string packageRoot,
        string modName,
        GtaSaModManager.Models.ModManifest manifest,
        bool recordInstallation = true,
        List<string>? installedFilesOutput = null)
    {
        DeleteManifestEntries(manifest);
        var cleoRoot = Path.Combine(_selectedGamePath, "cleo");
        Directory.CreateDirectory(cleoRoot);

        List<SelectedInstallEntry> entries;
        try
        {
            entries = ModPackageService.ResolveInstallSelection(packageRoot, manifest);
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (entries.Count == 0)
        {
            MessageBox.Show(
                _localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain installable files."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        var destinations = entries
            .Select(entry => GetSafeGamePath(Path.Combine("cleo", entry.RelativeDestination)))
            .ToList();
        var existingDestinations = destinations.Where(File.Exists).ToList();
        if (existingDestinations.Count > 0
            && MessageBox.Show(
                string.Format(
                    "{0} file(s) already exist in the CLEO folder. Overwrite them?{1}{2}",
                    existingDestinations.Count,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, existingDestinations.Select(Path.GetFileName))),
                _appName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return false;
        }

        var progressTitle = _localizationService.GetString("Installing", "Installing") + " " + modName;
        BeginStep4Progress(progressTitle, entries
            .Select(entry => (entry.SourcePath, entry.RelativeDestination))
            .ToList());

        try
        {
            var installedFiles = new List<string>();
            for (var index = 0; index < entries.Count; index++)
            {
                var destination = destinations[index];
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await CopyStep4FileAsync(
                    index,
                    entries[index].SourcePath,
                    destination,
                    entries[index].RelativeDestination);
                installedFiles.Add(destination);
                installedFilesOutput?.Add(destination);
            }

            if (recordInstallation)
            {
                ModLoaderService.RecordGameInstallation(
                    _selectedGamePath,
                    "putincleo",
                    modName,
                    packageRoot,
                    cleoRoot,
                    installedFiles);
            }
            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
            RefreshModList();
            return true;
        }
        catch (Exception ex)
        {
            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(
                _localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message,
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private async Task<bool> InstallTypedPackageAsync(
        string payloadPath,
        string modName,
        string packageRoot,
        GtaSaModManager.Models.ModManifest manifest,
        GtaSaModManager.Modsyn.Conversion.ModsynBackupConfiguration? backupOverride = null,
        IReadOnlyList<GtaSaModManager.Models.ModReplacementEntry>? replacementsOverride = null,
        bool recordInstallation = true,
        List<string>? installedFilesOutput = null)
    {
        DeleteManifestEntries(manifest);

        var isPutInGameFolder = manifest.NormalizedType == "putingamefolder";
        var isDirectGameInstall = manifest.NormalizedType is "putingamefolder" or "putandreplace" or "putandreplaces";
        var installAsModLoader = !isDirectGameInstall
            && (manifest.IsModLoader
                || manifest.IsSingleAssetPackage
                || manifest.IsMultiAssetPackage
                || _selectedAssetForInstall != null);
        var targetRoot = installAsModLoader
            ? Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), modName)
            : manifest.NormalizedType switch
            {
                "putingamefolder" or "putandreplace" or "putandreplaces" => _selectedGamePath,
                _ => string.Empty
            };
        if (string.IsNullOrWhiteSpace(targetRoot))
        {
            return false;
        }

        var previousPutInGameFolderFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (manifest.NormalizedType == "putingamefolder")
        {
            var installationManifestPath = ModLoaderService.GetGameInstallationsManifestPath(_selectedGamePath);
            var previousInstallation = ModLoaderService.LoadInstallationManifest(installationManifestPath).Entries
                .LastOrDefault(entry => string.Equals(entry.Type, "putingamefolder", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(entry.ModId, modName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(entry.SourcePackagePath, packageRoot, StringComparison.OrdinalIgnoreCase)));
            if (previousInstallation != null)
            {
                previousPutInGameFolderFiles.UnionWith((previousInstallation.InstalledFiles ?? new List<string>())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(Path.GetFullPath));
            }
        }

        if (manifest.NormalizedType == "putinmodloader" && Directory.Exists(targetRoot))
        {
            var result = MessageBox.Show(string.Format(_localizationService.GetString("DuplicateModPrompt", "A mod named '{0}' already exists. Replace it?"), modName), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return false;
            }

            Directory.Delete(targetRoot, true);
        }

        var selectedAssetList = GetSelectedAssetListForInstall(manifest, payloadPath);
        if (manifest.IsSingleAssetPackage && selectedAssetList.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "No matching asset was detected in the package."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var isAssetPackage = manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons";
        var isMultiAssetModelInstall = manifest.IsMultiAssetPackage && _multiSourceModels.Count > 0;
        var isAssetSelectionInstall = !isDirectGameInstall
            && (isAssetPackage || _selectedAssetForInstall != null);
        var sourceModelName = DetectSourceModelName(payloadPath);
        var sourceModelFiles = GetSourceModelFilePaths(payloadPath, sourceModelName);
        var multiSourceModelFiles = isMultiAssetModelInstall
            ? _multiSourceModels
                .Where(model => model.Status is SourceModelStatus.Mapped or SourceModelStatus.KeepOriginal)
                .SelectMany(model => new[] { model.DffPath, model.TxdPath })
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageFiles = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path, packageRoot)
                || isAssetSelectionInstall && ModPackageService.IsMediaFile(path))
            .Where(path => !isAssetPackage
                || ModPackageService.IsMediaFile(path)
                || (isMultiAssetModelInstall ? multiSourceModelFiles.Contains(path) : sourceModelFiles.Contains(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedTarget = _selectedAssetForInstall ?? selectedAssetList.FirstOrDefault() ?? _assetCatalogService.LoadAssets()
            .FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase));

        if (packageFiles.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain installable files."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var replacementTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (manifest.NormalizedType == "putandreplace")
        {
            var replacements = replacementsOverride ?? ModPackageService.ReadReplacementEntries(packageRoot);
            foreach (var sourcePath in packageFiles)
            {
                var relativePath = Path.GetRelativePath(payloadPath, sourcePath);
                replacementTargets[sourcePath] = GetSafeGamePath(relativePath);
            }

            foreach (var replacement in replacements)
            {
                var sourcePath = GetSafePackagePath(payloadPath, replacement.Source);
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("Replacement source was not found: " + replacement.Source, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
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
                if (manifest.NormalizedType == "putandreplaces"
                    || manifest.NormalizedType == "putingamefolder" && File.Exists(destinationPath))
                {
                    replacementTargets[sourcePath] = destinationPath;
                }
            }
        }

        var backupConfiguration = backupOverride ?? ModPackageService.ResolveModsynConfiguration(packageRoot).Backup;
        var filesToBackup = BackupStorageService.SelectFilesToBackup(
            _selectedGamePath,
            replacementTargets.Values.Where(path => !previousPutInGameFolderFiles.Contains(Path.GetFullPath(path))),
            backupConfiguration);
        var filesToBackupSet = filesToBackup.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var backupPlan = filesToBackup.Count > 0
            ? BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToBackup)
            : new BackupStoragePlan { HasBackup = true };
        if (filesToBackup.Count > 0 && !backupPlan.HasBackup)
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
                    return false;
                }

                backupPlan = BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToBackup, alternativeRoot);
                if (!backupPlan.HasBackup)
                {
                    MessageBox.Show(backupPlan.ErrorMessage ?? "The selected location does not have enough free space.", _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else if (MessageBox.Show("No backup will be created. Continue?", _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return false;
            }
        }

        var progressTitle = _localizationService.GetString("Installing", "Installing") + " " + modName;
        BeginStep4Progress(progressTitle, packageFiles
            .Select(path => (path, Path.GetRelativePath(payloadPath, path)))
            .ToList());

        try
        {
            var records = new List<ReplaceInstallationRecord>();
            var installedDestinationFiles = new List<string>();
            for (var index = 0; index < packageFiles.Count; index++)
            {
                var sourcePath = packageFiles[index];
                var extension = Path.GetExtension(sourcePath);
                var originalName = Path.GetFileNameWithoutExtension(sourcePath);
                var isMediaFile = ModPackageService.IsMediaFile(sourcePath);
                var sourceModel = isMultiAssetModelInstall
                    ? _multiSourceModels.FirstOrDefault(model =>
                        string.Equals(model.DffPath, sourcePath, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(model.TxdPath, sourcePath, StringComparison.OrdinalIgnoreCase))
                    : null;
                var selectedAsset = sourceModel != null
                    ? sourceModel.Status == SourceModelStatus.Mapped ? sourceModel.TargetAsset : null
                    : !isMediaFile && selectedTarget is not null && !string.IsNullOrWhiteSpace(selectedTarget.NameFile)
                        ? selectedTarget
                        : selectedAssetList.FirstOrDefault(asset => string.Equals(asset.NameFile, originalName, StringComparison.OrdinalIgnoreCase));
                var destinationName = sourceModel == null
                    ? selectedAsset?.NameFile ?? originalName
                    : sourceModel.Status == SourceModelStatus.Mapped
                        ? sourceModel.TargetAsset?.NameFile ?? sourceModel.BaseName
                        : sourceModel.BaseName;
                var isSourceModelFile = sourceModel != null || sourceModelFiles.Contains(sourcePath);
                var relativePath = isAssetSelectionInstall && !isMediaFile && isSourceModelFile
                    ? destinationName + extension
                    : Path.GetRelativePath(payloadPath, sourcePath);
                var destinationPath = replacementTargets.TryGetValue(sourcePath, out var replacementTarget)
                    ? replacementTarget
                    : GetSafeGamePath(installAsModLoader
                        ? Path.Combine("modloader", modName, relativePath)
                        : relativePath);

                if (replacementTargets.ContainsKey(sourcePath)
                    && !previousPutInGameFolderFiles.Contains(Path.GetFullPath(destinationPath)))
                {
                    var backupFilePath = string.Empty;
                    if (File.Exists(destinationPath)
                        && backupPlan.HasBackup
                        && filesToBackupSet.Contains(Path.GetFullPath(destinationPath)))
                    {
                        backupFilePath = await ModPackageService.BackupOriginalFileForReplacementAsync(
                            _selectedGamePath,
                            destinationPath,
                            modName,
                            backupPlan.BackupRoot);
                    }

                    records.Add(new ReplaceInstallationRecord
                    {
                        BackupId = Guid.NewGuid().ToString("N"),
                        ModName = modName,
                        GameFolder = _selectedGamePath,
                        OriginalFilePath = destinationPath,
                        BackupFilePath = backupFilePath,
                        BackupDirectoryPath = filesToBackup.Count > 0 && backupPlan.HasBackup
                            ? Path.Combine(backupPlan.BackupRoot, ModPackageService.SanitizeFolderName(modName))
                            : string.Empty,
                        InstalledModFile = sourcePath,
                        InstallationType = manifest.NormalizedType,
                        ReplacementSucceeded = true,
                        OriginalBackupAvailable = File.Exists(backupFilePath)
                    });
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                await CopyStep4FileAsync(
                    index,
                    sourcePath,
                    destinationPath,
                    Path.GetRelativePath(payloadPath, sourcePath));
                installedDestinationFiles.Add(destinationPath);
                installedFilesOutput?.Add(destinationPath);
            }

            if (previousPutInGameFolderFiles.Count > 0)
            {
                var gameRootPrefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_selectedGamePath))
                    + Path.DirectorySeparatorChar;
                var currentInstallFiles = installedDestinationFiles
                    .Select(Path.GetFullPath)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var previousFile in previousPutInGameFolderFiles)
                {
                    if (!previousFile.StartsWith(gameRootPrefix, StringComparison.OrdinalIgnoreCase)
                        || currentInstallFiles.Contains(previousFile)
                        || !File.Exists(previousFile))
                    {
                        continue;
                    }

                    File.Delete(previousFile);
                }
            }

            foreach (var record in records)
            {
                ModPackageService.RecordReplacementInstallation(record);
            }

            var installedFiles = installedDestinationFiles
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recordInstallation && installAsModLoader)
            {
                var installType = manifest.IsMultiAssetPackage
                    ? manifest.NormalizedType
                    : manifest.IsSingleAssetPackage || _selectedAssetForInstall != null
                        ? "vehicleandskinandweapon"
                        : "putinmodloader";
                var packageModId = GetPackageModId(packageRoot);
                ModLoaderService.RecordInstallation(modName, packageRoot, targetRoot);
                ModLoaderService.RecordPackageInstallation(
                    installType,
                    packageModId,
                    packageRoot,
                    targetRoot,
                    installedFiles,
                    mergeExistingFiles: _pendingExistingAssetInstallAction == DialogResult.No);
            }
            else if (recordInstallation)
            {
                ModLoaderService.RecordGameInstallation(_selectedGamePath, manifest.NormalizedType, modName, packageRoot, targetRoot, installedFiles);
            }

            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
            RefreshModList();
            return true;
        }
        catch (Exception ex)
        {
            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private string GetPackageModId(string packageRoot)
    {
        var baseModsFolder = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
            ? _selectedModSourcePath
            : _settings.ModSourceFolder ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(baseModsFolder) && Directory.Exists(baseModsFolder))
        {
            var relativePath = Path.GetRelativePath(baseModsFolder, packageRoot);
            if (!Path.IsPathRooted(relativePath)
                && relativePath != "."
                && relativePath != ".."
                && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            {
                return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }

        return Path.GetFileName(packageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            ?? modNameFallback(packageRoot);

        static string modNameFallback(string path)
        {
            var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString("N") : name;
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

    private void DeleteManifestEntries(GtaSaModManager.Models.ModManifest manifest)
    {
        if (!manifest.HasDeleteThis)
        {
            return;
        }

        foreach (var relativePath in manifest.DeleteThis
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var targetPath = GetSafeGamePath(relativePath.Trim());
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                else if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                MessageBox.Show(
                    "Could not remove the file or folder requested by deleteThis: " + relativePath + Environment.NewLine + ex.Message,
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
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

    private async Task<bool> InstallReplacingPackageAsync(
        string payloadPath,
        string modName,
        string packageRoot,
        GtaSaModManager.Modsyn.Conversion.ModsynBackupConfiguration? backupOverride = null,
        bool recordInstallation = true,
        List<string>? installedFilesOutput = null)
    {
        var backupConfiguration = backupOverride ?? ModPackageService.ResolveModsynConfiguration(packageRoot).Backup;
        var filesToReplace = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !ModPackageService.IsMetadataOrNonInstallableFile(path, packageRoot))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (filesToReplace.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected replacement package does not contain any files to replace."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var replacementTargets = filesToReplace
            .Select(sourcePath => GetSafeGamePath(Path.GetRelativePath(payloadPath, sourcePath)))
            .ToList();
        var filesToBackup = BackupStorageService.SelectFilesToBackup(
            _selectedGamePath,
            replacementTargets,
            backupConfiguration);
        var filesToBackupSet = filesToBackup.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var backupPlan = filesToBackup.Count > 0
            ? BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToBackup)
            : new BackupStoragePlan { HasBackup = true };
        if (filesToBackup.Count > 0 && !backupPlan.HasBackup)
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
                    return false;
                }

                backupPlan = BackupStorageService.CreatePlan(_selectedGamePath, modName, filesToBackup, alternativeRoot);
                if (!backupPlan.HasBackup)
                {
                    MessageBox.Show(backupPlan.ErrorMessage ?? "The selected location does not have enough free space.", _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
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
                    return false;
                }
            }
        }

        var progressTitle = _localizationService.GetString("Installing", "Installing") + " " + modName + " (Replacing)";
        BeginStep4Progress(progressTitle, filesToReplace
            .Select(path => (path, Path.GetRelativePath(payloadPath, path)))
            .ToList());

        var records = new List<ReplaceInstallationRecord>();
        var installedFiles = new List<string>();

        try
        {
            for (var i = 0; i < filesToReplace.Count; i++)
            {
                var sourceFile = filesToReplace[i];
                var relativePath = Path.GetRelativePath(payloadPath, sourceFile)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var destinationFile = GetSafeGamePath(relativePath);
                var destinationDirectory = Path.GetDirectoryName(destinationFile);

                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var backupFilePath = string.Empty;
                if (File.Exists(destinationFile)
                    && backupPlan.HasBackup
                    && filesToBackupSet.Contains(Path.GetFullPath(destinationFile)))
                {
                    backupFilePath = await ModPackageService.BackupOriginalFileForReplacementAsync(
                        _selectedGamePath,
                        destinationFile,
                        modName,
                        backupPlan.BackupRoot);
                }

                await CopyStep4FileAsync(i, sourceFile, destinationFile, relativePath);
                installedFiles.Add(destinationFile);
                installedFilesOutput?.Add(destinationFile);

                var record = new ReplaceInstallationRecord
                {
                    BackupId = Guid.NewGuid().ToString("N"),
                    ModName = modName,
                    GameFolder = _selectedGamePath,
                    OriginalFilePath = destinationFile,
                    BackupFilePath = backupFilePath,
                    BackupDirectoryPath = filesToBackup.Count > 0 && backupPlan.HasBackup
                        ? Path.Combine(backupPlan.BackupRoot, ModPackageService.SanitizeFolderName(modName))
                        : string.Empty,
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

            }

            if (recordInstallation)
            {
                ModLoaderService.RecordGameInstallation(
                    _selectedGamePath,
                    "replacing",
                    modName,
                    packageRoot,
                    _selectedGamePath,
                    installedFiles);
            }

            CompleteStep4Progress(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."));
            if (!_isMixedInstallActive)
            {
                GoToStep(WizardStep.Step6);
            }
            return true;
        }
        catch (Exception ex)
        {
            FailStep4Progress(_localizationService.GetString("InstallationFailed", "The mod could not be installed."));
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.ModLibrary.cs
    // -------------------------------------------------------------------------
}
