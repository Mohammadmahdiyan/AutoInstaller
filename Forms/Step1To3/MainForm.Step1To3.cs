using System;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    private Panel CreateWizardStep1()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoSize = true };
        var title = new Label { Text = _localizationService.GetString("Step1GameFolder", "Game folder"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var description = new Label { Text = _localizationService.GetString("GameFolderRequired", "Choose your GTA San Andreas folder."), AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 0, 0, 10) };
        var pathText = new TextBox { Name = "Step1GamePathTextBox", Width = 560, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var browse = new RoundedButton { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
        ApplyPathSelectorTextBoxStyle(pathText);
        ApplyBrowseButtonStyle(browse, Color.FromArgb(37, 99, 235));

        browse.Click += (_, _) =>
        {
            var initial = Directory.Exists(pathText.Text) ? pathText.Text : _settings.GamePath;
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"), initial);
            if (string.IsNullOrWhiteSpace(selected) || !GameService.IsValidGameFolder(selected))
            {
                MessageBox.Show(_localizationService.GetString("InvalidGameFolder", "This folder does not contain a valid GTA San Andreas installation."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedGamePath = selected;
            _settings.GamePath = selected;
            _settings.GameExecutableName = "gta_sa.exe";
            _settings.GameProfileId = BuildProfileId(selected, _settings.GameExecutableName);
            _settingsService.Save(_settings);
            pathText.Text = selected;
            LoadSettingsIntoUi();
            UpdateSidebarState();
            GameService.EnsureModLoaderFolder(selected);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        flow.Controls.Add(CreateBrowseInputGroup(pathText, browse, 560));

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        stack.Controls.Add(title);
        stack.Controls.Add(description);
        stack.Controls.Add(flow);

        panel.Controls.Add(stack);

        if (!string.IsNullOrWhiteSpace(_settings.GamePath) && Directory.Exists(_settings.GamePath) && GameService.IsValidGameFolder(_settings.GamePath))
        {
            pathText.Text = _settings.GamePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return panel;
    }

    private Panel CreateWizardStep2()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoSize = true };
        var title = new Label { Text = _localizationService.GetString("BaseModsFolderTitle", "Base Mods Folder"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var subtitle = new Label { Text = _localizationService.GetString("BaseModsFolderSubtitle", "Select the base mod folder for easy access"), AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 0, 0, 10) };

        var modBaseText = new TextBox { Name = "Step2ModLibraryPathTextBox", Width = 520, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var modBaseBrowse = new RoundedButton { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
        ApplyPathSelectorTextBoxStyle(modBaseText);
        ApplyBrowseButtonStyle(modBaseBrowse, Color.FromArgb(37, 99, 235));

        modBaseBrowse.Click += (_, _) =>
        {
            var initial = Directory.Exists(modBaseText.Text)
                ? modBaseText.Text.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : (!string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder)
                    ? _settings.ModSourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : null);
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"), initial);
            if (string.IsNullOrWhiteSpace(selected) || !Directory.Exists(selected))
            {
                MessageBox.Show(_localizationService.GetString("InvalidModLibraryFolder", "This folder is not a valid mods library folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedModSourcePath = selected;
            _settings.ModSourceFolder = selected;
            _settingsService.Save(_settings);
            modBaseText.Text = selected;
            if (ModLibraryPathTextBox != null)
            {
                ModLibraryPathTextBox.Text = selected;
            }

            RefreshModLibrary();
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        flow.Controls.Add(CreateBrowseInputGroup(modBaseText, modBaseBrowse, 520));

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        stack.Controls.Add(title);
        stack.Controls.Add(subtitle);
        stack.Controls.Add(flow);

        panel.Controls.Add(stack);

        if (!string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder))
        {
            modBaseText.Text = _settings.ModSourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return panel;
    }

    private Panel CreateWizardStep3()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoSize = false };
        var title = new Label { Text = _localizationService.GetString("Step3Mod", "Select mod"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var folderLabel = new Label { Text = _localizationService.GetString("ModFolder", "Mod Folder"), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };
        var folderText = new TextBox { Width = 520, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var browse = new RoundedButton { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
        var selectedName = new Label
        {
            Name = "Step3DetectedModStatus",
            Text = string.IsNullOrWhiteSpace(_selectedModName)
                ? string.Empty
                : string.Format(_localizationService.GetString("DetectedModStatus", "Detected mod: {0} ✓"), _selectedModName),
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Font = new Font("Segoe UI", 11F),
            Margin = new Padding(0, 12, 0, 0),
            Visible = _isDetectedModStatusVisible
        };
        ApplyPathSelectorTextBoxStyle(folderText);
        ApplyBrowseButtonStyle(browse, Color.FromArgb(37, 99, 235));

        browse.Click += async (_, _) =>
        {
            var initial = !string.IsNullOrWhiteSpace(_selectedModSourcePath) && Directory.Exists(_selectedModSourcePath)
                ? _selectedModSourcePath
                : (!string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder)
                    ? _settings.ModSourceFolder
                    : null);
            initial = NormalizeExistingDirectory(initial);
            var selected = PromptForModFolderSelection(_localizationService.GetString("SelectModFolder", "Select the mod folder"), initial);
            if (string.IsNullOrWhiteSpace(selected) || !Directory.Exists(selected))
            {
                _multiSourceModels.Clear();
                _multiIndex = 0;
                _selectedModName = string.Empty;
                _isDetectedModStatusVisible = false;
                _selectedModPayloadPath = string.Empty;
                _selectedReadmePath = string.Empty;
                _selectedImageFiles = new List<string>();
                folderText.Text = string.Empty;
                selectedName.Text = string.Empty;
                UpdateSidebarState();
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(selected).Any())
            {
                _multiSourceModels.Clear();
                _multiIndex = 0;
                _selectedModName = string.Empty;
                _isDetectedModStatusVisible = false;
                _selectedModPayloadPath = string.Empty;
                _selectedReadmePath = string.Empty;
                _selectedImageFiles = new List<string>();
                folderText.Text = string.Empty;
                selectedName.Text = string.Empty;
                MessageBox.Show(_localizationService.GetString("ModFolderEmpty", "The selected mod folder is empty or unreadable."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateSidebarState();
                return;
            }

            _selectedModName = ResolveDirectoryName(selected);
            _isDetectedModStatusVisible = true;
            _selectedModPackageRoot = selected;
            _selectedModManifest = ModPackageService.ResolveManifest(selected);
            _selectedModPayloadPath = ModPackageService.GetInstallPayloadDirectory(selected, _selectedModManifest);
            if (string.IsNullOrWhiteSpace(_selectedModPayloadPath) || !Directory.Exists(_selectedModPayloadPath))
            {
                _selectedModPayloadPath = selected;
            }
            _step5PreparedPayloadPath = string.Empty;
            _step5ExistingInstallationDestination = string.Empty;
            _step5PreparationAttempted = false;
            _step5DetectedAssetType = string.Empty;
            _step5UnknownAssetTypeCancelled = false;
            _pendingExistingAssetInstallAction = null;
            _step5OccupiedAssetFolders.Clear();
            _step5OccupiedScanKey = string.Empty;
            _step5OccupiedScanComplete = false;
            _occupiedAssetInstallWarningAcknowledged = false;
            _pendingAssetModelFilesToDelete.Clear();
            _lastActionWasDelete = false;
            _isCheckingPreviousAssetInstallation = false;
            _step5DetectedAssets.Clear();
            _step5SelectedAssetKeys.Clear();
            _multiSourceModels.Clear();
            _multiLastUsedAssetType = "Vehicle";
            var isMultiAssetPackage = _selectedModManifest?.IsMultiAssetPackage == true;
            if (isMultiAssetPackage)
            {
                _multiSourceModels.AddRange(BuildSourceModels(_selectedModPayloadPath));
            }

            _multiIndex = 0;
            System.Diagnostics.Debug.WriteLine(
                $"[MultiAssetDetection] isMulti={isMultiAssetPackage}; models={string.Join(", ", _multiSourceModels.Select(model => $"{model.BaseName}:{model.DetectedAssetType}"))}");
            _step5CategoryFilter = _localizationService.GetString("AssetAll", "All");
            _selectedAssetForInstall = null;
            _selectedReadmePath = string.Empty;
            _selectedImageFiles = new List<string>();
            folderText.Text = selected;
            _detectedModLabel = selectedName;
            selectedName.Text = string.Format(_localizationService.GetString("DetectedModStatus", "Detected mod: {0} ✓"), _selectedModName);
            selectedName.Visible = true;

            if (_sidebarDetectedModLabel != null && !_sidebarDetectedModLabel.IsDisposed)
            {
                _sidebarDetectedModLabel.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;
                _sidebarDetectedModLabel.Visible = true;
            }

            _detectedModTimer.Stop();
            _detectedModTimer.Start();

            if (_selectedModManifest?.IsSingleAssetPackage == true || _selectedModManifest?.IsMultiAssetPackage == true)
            {
                _isCheckingPreviousAssetInstallation = true;
                _sidebarNextButton.Enabled = false;
                _sidebarNextButton.Text = GetLoadingSpinnerGlyph(_loadingSpinnerAngle) + " " + _localizationService.GetString("CheckingPreviousInstallation", "Checking previous installation");
                _loadingSpinnerTimer.Start();
                UpdateSidebarState();
                _sidebarNextButton.Refresh();
                _sidebarNextButton.Update();

                try
                {
                    await Task.Yield();
                    var packageRoot = _selectedModPackageRoot;
                    var payloadPath = _selectedModPayloadPath;
                    var checkTask = Task.Run(() => (
                        ExistingInstallation: FindExistingAssetInstallation(packageRoot),
                        ReadmePath: FindReadmeFile(payloadPath),
                        ImageFiles: FindImageFiles(payloadPath)));
                    await Task.WhenAll(checkTask, Task.Delay(1000));
                    var checkResult = await checkTask;
                    _selectedReadmePath = checkResult.ReadmePath;
                    _selectedImageFiles = checkResult.ImageFiles;
                    RefreshStep3Images(panel, payloadPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        _localizationService.GetString("PreviousInstallationCheckFailed", "Could not check for a previous installation.") + Environment.NewLine + ex.Message,
                        _appName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                finally
                {
                    _isCheckingPreviousAssetInstallation = false;
                    _loadingSpinnerTimer.Stop();
                    UpdateSidebarState();
                }
            }
            else
            {
                _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
                _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
                UpdateSidebarState();
                RefreshStep3Images(panel, _selectedModPayloadPath);
            }
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        flow.Controls.Add(CreateBrowseInputGroup(folderText, browse, 520));
        var imageGallery = new Panel
        {
            Name = "Step3ImageGallery",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        stack.Controls.Add(title, 0, 0);
        stack.Controls.Add(folderLabel, 0, 1);
        stack.Controls.Add(flow, 0, 2);
        stack.Controls.Add(selectedName, 0, 3);
        stack.Controls.Add(imageGallery, 0, 4);

        panel.Controls.Add(stack);
        _detectedModLabel = selectedName;
        if (!string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
        {
            folderText.Text = _selectedModPackageRoot;
            RefreshStep3Images(panel, _selectedModPayloadPath);
        }
        return panel;
    }

    private void RefreshStep3Images(Control step3Panel, string modFolder, bool resetIndex = true)
    {
        var gallery = step3Panel.Controls.Find("Step3ImageGallery", true).FirstOrDefault() as Panel;
        if (gallery == null)
        {
            return;
        }

        foreach (var control in gallery.Controls.Cast<Control>().ToList())
        {
            control.Dispose();
        }

        gallery.Controls.Clear();
        var imageFiles = Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories)
            .Where(MediaPreviewControl.IsSupportedMediaPath)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resetIndex || imageFiles.Count <= 4)
        {
            _step4ImageIndex = 0;
        }

        if (imageFiles.Count == 0)
        {
            return;
        }

        var pageStart = Math.Clamp((_step4ImageIndex / 4) * 4, 0, Math.Max(0, imageFiles.Count - 1));
        var visibleImages = imageFiles
            .Skip(pageStart)
            .Take(4)
            .ToList();
        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        gallery.RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;
        var imageCount = visibleImages.Count;
        var columnCount = imageCount switch
        {
            1 or 2 => 1,
            _ => 2
        };
        var imageRowCount = imageCount switch
        {
            1 => 1,
            2 => 2,
            3 or 4 => 2,
            _ => 0
        };
        var hasNavigation = imageFiles.Count > 4;
        var imageGrid = new TableLayoutPanel
        {
            Name = "Step3ImageGrid",
            Dock = DockStyle.Fill,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
            ColumnCount = columnCount,
            RowCount = imageRowCount,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };

        for (var column = 0; column < columnCount; column++)
        {
            imageGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnCount));
        }

        for (var row = 0; row < imageRowCount; row++)
        {
            imageGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / Math.Max(1, imageRowCount)));
        }

        gallery.Controls.Add(imageGrid);

        for (var index = 0; index < visibleImages.Count; index++)
        {
            var imageFile = visibleImages[index];
            try
            {
                var preview = new MediaPreviewControl
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 247, 250),
                    Margin = new Padding(4),
                    Tag = imageFile,
                };
                preview.SizeChanged += (_, _) =>
                {
                    if (preview.Width > 0 && preview.Height > 0)
                    {
                        preview.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, preview.Width, preview.Height), 12));
                    }
                };
                if (!preview.LoadMedia(imageFile))
                {
                    preview.Dispose();
                    continue;
                }
                preview.RightClicked += (_, _) =>
                {
                    if (preview.IsVideo)
                    {
                        preview.SetSoundEnabled(false);
                    }

                    OpenFullImageViewer(imageFiles, imageFiles.FindIndex(path => string.Equals(path, imageFile, StringComparison.OrdinalIgnoreCase)), imageFile);
                };
                preview.DoubleClick += (_, _) =>
                {
                    if (preview.IsVideo)
                    {
                        preview.ToggleSound();
                    }
                };
                var toolTip = new ToolTip();
                toolTip.SetToolTip(preview, Path.GetFileName(imageFile));

                var row = index / columnCount;
                var column = index % columnCount;
                if (imageCount == 3 && index == 2)
                {
                    column = 0;
                    row = 1;
                }

                imageGrid.Controls.Add(preview, column, row);
            }
            catch
            {
                // Ignore image formats that Windows cannot decode.
            }
        }

        if (hasNavigation)
        {
            var previous = new RoundedButton
            {
                Text = isRtl ? "▶" : "◀",
                Width = 46,
                Height = 36,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                AccentColor = Color.FromArgb(37, 99, 235)
            };
            var next = new RoundedButton
            {
                Text = isRtl ? "◀" : "▶",
                Width = 46,
                Height = 36,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                AccentColor = Color.FromArgb(37, 99, 235)
            };
            var navigationToolTip = new ToolTip();
            navigationToolTip.SetToolTip(previous, _localizationService.GetString("Previous", "Previous"));
            navigationToolTip.SetToolTip(next, _localizationService.GetString("Next", "Next"));

            previous.Click += (_, _) =>
            {
                var currentPageStart = (_step4ImageIndex / 4) * 4;
                _step4ImageIndex = currentPageStart > 0
                    ? currentPageStart - 4
                    : ((imageFiles.Count - 1) / 4) * 4;
                RefreshStep3Images(step3Panel, modFolder, false);
            };
            next.Click += (_, _) =>
            {
                var currentPageStart = (_step4ImageIndex / 4) * 4;
                var nextPageStart = currentPageStart + 4;
                _step4ImageIndex = nextPageStart < imageFiles.Count ? nextPageStart : 0;
                RefreshStep3Images(step3Panel, modFolder, false);
            };

            previous.Anchor = isRtl
                ? AnchorStyles.Bottom | AnchorStyles.Left
                : AnchorStyles.Bottom | AnchorStyles.Right;
            next.Anchor = isRtl
                ? AnchorStyles.Bottom | AnchorStyles.Left
                : AnchorStyles.Bottom | AnchorStyles.Right;
            gallery.Controls.Add(previous);
            gallery.Controls.Add(next);
            var navigationLeft = isRtl
                ? 10
                : Math.Max(0, gallery.ClientSize.Width - previous.Width - next.Width - 20);
            var navigationTop = Math.Max(0, gallery.ClientSize.Height - previous.Height - 10);
            if (isRtl)
            {
                next.Location = new Point(navigationLeft, navigationTop);
                previous.Location = new Point(navigationLeft + next.Width + 10, navigationTop);
            }
            else
            {
                previous.Location = new Point(navigationLeft, navigationTop);
                next.Location = new Point(navigationLeft + previous.Width + 10, navigationTop);
            }
            previous.BringToFront();
            next.BringToFront();
        }
    }
}
