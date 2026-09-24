using System;
using System.Diagnostics;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // از اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
    private void ConfigureUi()
    {
        Text = _appName;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1100, 760);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        if (MainPanel != null)
        {
            MainPanel.AutoScroll = true;
        }

        if (GameStatusLabel != null)
        {
            GameStatusLabel.Text = _localizationService.GetString("GameStatus", "Game Status");
        }

        if (GamePathLabel != null)
        {
            GamePathLabel.Text = _localizationService.GetString("GamePath", "Game Path");
        }

        if (GameFolderNotConfiguredLabel != null)
        {
            GameFolderNotConfiguredLabel.Text = _localizationService.GetString("GameFolderNotConfigured", "Game folder not configured");
        }

        if (ModLibraryTitleLabel != null)
        {
            ModLibraryTitleLabel.Text = _localizationService.GetString("ModLibraryTitle", "Mod Library");
        }

        if (ModLibraryPathLabel != null)
        {
            ModLibraryPathLabel.Text = _localizationService.GetString("ModLibraryPath", "Base Mods Folder");
        }

        if (ModLibraryChangeButton != null)
        {
            ModLibraryChangeButton.Text = _localizationService.GetString("Change", "Change");
        }

        if (ModLibraryOpenButton != null)
        {
            ModLibraryOpenButton.Text = _localizationService.GetString("OpenFolder", "Open Folder");
        }

        if (OpenGameFolderButton != null)
        {
            OpenGameFolderButton.Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder");
        }

        if (RunGameButton != null)
        {
            RunGameButton.Text = _localizationService.GetString("RunGame", "Run Game");
        }

        if (InstallModButton != null)
        {
            InstallModButton.Text = _localizationService.GetString("InstallMod", "Install Mod");
        }

        if (ModLoaderTitleLabel != null)
        {
            ModLoaderTitleLabel.Text = _localizationService.GetString("ModLoaderMods", "ModLoader Mods");
        }

        if (ThemeLabel != null)
        {
            ThemeLabel.Text = _localizationService.GetString("Theme", "Theme");
        }

        if (LanguageLabel != null)
        {
            LanguageLabel.Text = _localizationService.GetString("Language", "Language");
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------

    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
    private void InitializeSidebar()
    {
        if (MainPanel == null)
        {
            return;
        }

        if (HeaderPanel != null)
        {
            HeaderPanel.Visible = false;
        }

        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 260,
            Padding = new Padding(16),
            BorderStyle = BorderStyle.None,
            BackColor = Color.Transparent
        };

        _sidebarStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 13,
            AutoSize = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };

        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidebarStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _sidebarPreviousButton = new Button { Text = _localizationService.GetString("Previous", "Previous"), Width = 190, Height = 36, Enabled = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarNextButton = new Button { Text = _localizationService.GetString("Next", "Next"), Width = 190, Height = 36, Enabled = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarLanguageComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarThemeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarDetectedModLabel = new Label
        {
            Name = "SidebarDetectedModLabel",
            AutoSize = true,
            Visible = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Text = _localizationService.GetString("DetectedMod", "Detected mod")
        };
        _sidebarSelectedModelPanel = new Panel
        {
            Name = "SidebarSelectedModelPanel",
            Visible = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        var selectedModelLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        selectedModelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectedModelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
        selectedModelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        selectedModelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));

        _sidebarSourceModelImage = new PictureBox { Dock = DockStyle.Fill, Height = 80, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 247, 250), Margin = new Padding(0, 0, 0, 6), Visible = false };
        _sidebarSelectedAssetImage = new PictureBox { Dock = DockStyle.Fill, Height = 80, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 247, 250), Margin = new Padding(0, 6, 0, 0), Visible = false };
        _sidebarSelectedModelArrowLabel = new Label { Text = "→", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235), Margin = new Padding(0, 4, 0, 4), Visible = false };
        _sidebarSelectedAssetNameLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Visible = false, Text = string.Empty };
        _sidebarSelectedAssetIdLabel = new Label { Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI", 7.5F), ForeColor = Color.FromArgb(71, 85, 105), Visible = false, Text = string.Empty };

        selectedModelLayout.Controls.Add(_sidebarSourceModelImage, 0, 0);
        selectedModelLayout.Controls.Add(_sidebarSelectedModelArrowLabel, 0, 1);
        selectedModelLayout.Controls.Add(_sidebarSelectedAssetImage, 0, 2);
        selectedModelLayout.Controls.Add(_sidebarSelectedAssetNameLabel, 0, 3);
        selectedModelLayout.Controls.Add(_sidebarSelectedAssetIdLabel, 0, 4);

        _sidebarSelectedModelPanel.Controls.Add(selectedModelLayout);
        _sidebarStep3GalleryPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Visible = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        _sidebarStep4Image = new MediaPreviewControl { Width = 190, Height = 110, Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250), Visible = false, Margin = new Padding(0, 0, 0, 10) };
        _sidebarStep4Image.RightClicked += (_, _) => SidebarImageClick(_sidebarStep4Image, EventArgs.Empty);
        _sidebarImageNavPanel = new Panel { Dock = DockStyle.Fill, Visible = false, Height = 38, Margin = new Padding(0, 0, 0, 8), BackColor = Color.Transparent };
        _sidebarImagePrevButton = new Button { Name = "SidebarImagePrevButton", Text = "◀", Width = 36, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
        _sidebarImageNextButton = new Button { Name = "SidebarImageNextButton", Text = "▶", Width = 36, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
        _sidebarImagePrevButton.FlatAppearance.BorderSize = 0;
        _sidebarImageNextButton.FlatAppearance.BorderSize = 0;
        _sidebarImagePrevButton.Click += (_, _) =>
        {
            if (_currentStep == WizardStep.Step3)
            {
                ShowNextStep4Image(false);
            }
            else if (_currentStep == WizardStep.Step4)
            {
                ShowNextStep4Image(false);
            }
            else if (_currentStep == WizardStep.Step5)
            {
                ShowNextStep5Image(false);
            }
        };
        _sidebarImageNextButton.Click += (_, _) =>
        {
            if (_currentStep == WizardStep.Step3)
            {
                ShowNextStep4Image(false);
            }
            else if (_currentStep == WizardStep.Step4)
            {
                ShowNextStep4Image(false);
            }
            else if (_currentStep == WizardStep.Step5)
            {
                ShowNextStep5Image(false);
            }
        };
        _sidebarImageNavPanel.Controls.Add(_sidebarImagePrevButton);
        _sidebarImageNavPanel.Controls.Add(_sidebarImageNextButton);
        _sidebarReadmeTextBox = new TextBox
        {
            Name = "SidebarReadmeTextBox",
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(250, 251, 253),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 9.5F),
            Visible = false,
            Margin = new Padding(0),
            Padding = new Padding(8, 7, 8, 7),
            Height = 120,
            WordWrap = true
        };
        _sidebarReadmeHost = new Panel
        {
            Name = "SidebarReadmeHost",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(250, 251, 253),
            Padding = new Padding(1),
            Margin = new Padding(0, 0, 0, 10)
        };
        _sidebarReadmeHost.Paint += (_, e) =>
        {
            using var borderPen = new Pen(Color.FromArgb(90, 148, 163, 184));
            e.Graphics.DrawRectangle(borderPen, 0, 0, _sidebarReadmeHost.Width - 1, _sidebarReadmeHost.Height - 1);
        };
        _sidebarReadmeHost.Controls.Add(_sidebarReadmeTextBox);
        _sidebarReadmeButton = new Button
        {
            Text = _localizationService.GetString("OpenReadmeFile", "Open README").Replace(".txt", string.Empty, StringComparison.OrdinalIgnoreCase),
            Width = 190,
            Height = 42,
            AutoSize = false,
            Enabled = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Visible = false
        };

        _sidebarLanguageComboBox.Items.AddRange(new object[] { "English", "فارسی" });
        _sidebarThemeComboBox.Items.AddRange(new object[] { "System", "Light Blue", "Light Purple", "Light Green", "Light Orange", "Dark Blue", "Dark Purple", "Dark Green", "Dark Red" });

        _sidebarPreviousButton.Click += (_, _) => HandleSidebarPrevious();
        _sidebarNextButton.Click += (_, _) => HandleSidebarNext();
        _sidebarReadmeButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _selectedReadmePath,
                    UseShellExecute = true
                });
            }
        };
        _sidebarLanguageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
        _sidebarThemeComboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;

        _sidebarStack.Controls.Add(_sidebarPreviousButton, 0, 0);
        _sidebarStack.Controls.Add(_sidebarNextButton, 0, 1);
        _sidebarStack.Controls.Add(new Label { Text = _localizationService.GetString("Language", "Language"), AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) }, 0, 2);
        _sidebarStack.Controls.Add(_sidebarLanguageComboBox, 0, 3);
        _sidebarStack.Controls.Add(new Label { Text = _localizationService.GetString("Theme", "Theme"), AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) }, 0, 4);
        _sidebarStack.Controls.Add(_sidebarThemeComboBox, 0, 5);
        _sidebarStack.Controls.Add(_sidebarDetectedModLabel, 0, 6);
        _sidebarStack.Controls.Add(_sidebarSelectedModelPanel, 0, 7);
        _sidebarStack.Controls.Add(_sidebarStep3GalleryPanel, 0, 8);
        _sidebarStack.Controls.Add(_sidebarStep4Image, 0, 9);
        _sidebarStack.Controls.Add(_sidebarImageNavPanel, 0, 10);
        _sidebarStack.Controls.Add(_sidebarReadmeHost, 0, 11);
        _sidebarStack.Controls.Add(_sidebarReadmeButton, 0, 12);

        _sidebarPanel.Controls.Add(_sidebarStack);
        MainPanel.Controls.Add(_sidebarPanel);
        _sidebarPanel.BringToFront();

        ApplySidebarDirection();

        _step4ImageTimer.Interval = 1800;
        _step4ImageTimer.Tick += (_, _) => ShowNextStep4Image();
        _step5ImageTimer.Interval = 3200;
        _step5ImageTimer.Tick += (_, _) => ShowNextStep5Image();
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
    private int GetSidebarReadmeHeight(string? readmeText, bool hasVisibleImage)
    {
        var availableWidth = Math.Max(150, _sidebarPanel.Width - 36);
        var normalizedText = string.IsNullOrWhiteSpace(readmeText) ? "README" : readmeText;

        using var graphics = _sidebarReadmeTextBox.CreateGraphics();
        var size = TextRenderer.MeasureText(
            graphics,
            normalizedText,
            _sidebarReadmeTextBox.Font,
            new Size(availableWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding);

        var remainingHeight = Math.Max(150, _sidebarPanel.Height - 280);
        var imageSpace = hasVisibleImage ? Math.Max(90, remainingHeight / 2) : 0;
        var maxAllowed = Math.Max(90, remainingHeight - imageSpace);
        var requiredHeight = size.Height + 18;
        var targetHeight = Math.Clamp(requiredHeight, 80, maxAllowed);

        _sidebarReadmeTextBox.ScrollBars = requiredHeight > maxAllowed ? ScrollBars.Vertical : ScrollBars.None;
        return targetHeight;
    }

    private int GetSidebarImageHeight(bool hasVisibleReadme)
    {
        var remainingHeight = Math.Max(150, _sidebarPanel.Height - 280);
        if (hasVisibleReadme)
        {
            var readmeHeight = Math.Min(Math.Max(90, remainingHeight / 2), 190);
            return Math.Max(120, remainingHeight - readmeHeight);
        }

        return Math.Max(140, remainingHeight);
    }

    private void UpdateSidebarState()
    {
        if (_sidebarPreviousButton == null || _sidebarNextButton == null || _sidebarReadmeButton == null)
        {
            return;
        }

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        if (MainPanel != null)
        {
            MainPanel.RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;
        }

        _sidebarPreviousButton.Visible = _currentStep != WizardStep.Step1;
        _sidebarNextButton.Visible = true;
        _sidebarPreviousButton.Enabled = false;
        _sidebarNextButton.Enabled = false;

        var isStep3Preview = _currentStep == WizardStep.Step3;
        var isStep4Preview = _currentStep == WizardStep.Step4;
        var isStep5Preview = _currentStep == WizardStep.Step5;

        var hasSidebarReadme = !string.IsNullOrWhiteSpace(_selectedReadmePath)
            && File.Exists(_selectedReadmePath);
        var sidebarImageFiles = isStep3Preview
            ? new List<string>()
            : isStep4Preview
                ? _selectedImageFiles.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : GetStep5SidebarImageFiles();
        var hasSidebarImage = sidebarImageFiles.Count > 0;
        var hasStep5ModImage = isStep5Preview
            && !string.IsNullOrWhiteSpace(_selectedModPayloadPath)
            && FindImageFiles(_selectedModPayloadPath).Any(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        var readmeText = hasSidebarReadme ? TryReadTextFile(_selectedReadmePath) : string.Empty;
        var shouldDisplaySidebarReadme = hasSidebarReadme && !(hasStep5ModImage && isStep5Preview);
        var hasDetectedMod = !string.IsNullOrWhiteSpace(_selectedModName) && (isStep3Preview || isStep4Preview || isStep5Preview);
        var hasSelectedAssetModel = isStep5Preview && _selectedAssetForInstall != null;

        if (_sidebarStack != null && _sidebarStack.Controls.Contains(_sidebarReadmeHost) && _sidebarStack.Controls.Contains(_sidebarStep4Image))
        {
            if (isStep3Preview)
            {
                _sidebarStack.SetCellPosition(_sidebarSelectedModelPanel, new TableLayoutPanelCellPosition(0, 7));
                _sidebarStack.SetCellPosition(_sidebarReadmeHost, new TableLayoutPanelCellPosition(0, 7));
                _sidebarStack.SetCellPosition(_sidebarReadmeButton, new TableLayoutPanelCellPosition(0, 8));
                _sidebarStack.SetCellPosition(_sidebarStep3GalleryPanel, new TableLayoutPanelCellPosition(0, 9));
                _sidebarStack.SetCellPosition(_sidebarStep4Image, new TableLayoutPanelCellPosition(0, 10));
                _sidebarStack.SetCellPosition(_sidebarImageNavPanel, new TableLayoutPanelCellPosition(0, 11));
                _sidebarSelectedModelPanel.Visible = false;
                _sidebarStep3GalleryPanel.Visible = false;
                _sidebarStep4Image.Visible = false;
                _sidebarImageNavPanel.Visible = false;
            }
            else
            {
                _sidebarStack.SetCellPosition(_sidebarStep4Image, new TableLayoutPanelCellPosition(0, 8));
                _sidebarStack.SetCellPosition(_sidebarImageNavPanel, new TableLayoutPanelCellPosition(0, 9));
                _sidebarStack.SetCellPosition(_sidebarReadmeHost, new TableLayoutPanelCellPosition(0, 10));
                _sidebarStack.SetCellPosition(_sidebarReadmeButton, new TableLayoutPanelCellPosition(0, 11));
            }

            for (var row = 0; row < _sidebarStack.RowStyles.Count; row++)
            {
                _sidebarStack.RowStyles[row] = new RowStyle(SizeType.AutoSize);
            }

            if (isStep3Preview && hasSidebarReadme)
            {
                _sidebarStack.RowStyles[7] = new RowStyle(SizeType.Percent, 100F);
            }
            else if (!isStep3Preview && hasSidebarImage)
            {
                _sidebarStack.RowStyles[8] = new RowStyle(SizeType.Percent, 100F);
            }
        }

        _sidebarDetectedModLabel.Visible = hasDetectedMod;
        _sidebarDetectedModLabel.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;

        if (hasSelectedAssetModel)
        {
            var selectedAsset = _selectedAssetForInstall;
            if (selectedAsset is null)
            {
                _sidebarSelectedModelPanel.Visible = false;
                _sidebarSourceModelImage.Visible = false;
                _sidebarSelectedAssetImage.Visible = false;
                _sidebarSelectedModelArrowLabel.Visible = false;
                _sidebarSelectedAssetNameLabel.Visible = false;
                _sidebarSelectedAssetIdLabel.Visible = false;
                _sidebarSourceModelImage.Image?.Dispose();
                _sidebarSourceModelImage.Image = null;
                _sidebarSelectedAssetImage.Image?.Dispose();
                _sidebarSelectedAssetImage.Image = null;
            }
            else
            {
                var sourceFiles = FindImageFiles(_selectedModPayloadPath);
                var sourceImagePath = sourceFiles.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
                var selectedAssetImagePath = _assetCatalogService.ResolveImagePath(selectedAsset);

                _sidebarSelectedModelPanel.Visible = true;
                _sidebarSelectedModelArrowLabel.Visible = true;
                _sidebarSelectedModelArrowLabel.Text = isRtl ? "←" : "→";
                _sidebarSelectedAssetNameLabel.Visible = true;
                _sidebarSelectedAssetNameLabel.Text = selectedAsset.NameFile ?? _selectedModName;
                _sidebarSelectedAssetIdLabel.Visible = !string.IsNullOrWhiteSpace(selectedAsset.Id);
                _sidebarSelectedAssetIdLabel.Text = string.IsNullOrWhiteSpace(selectedAsset.Id) ? string.Empty : "ID: " + selectedAsset.Id;

                var hasSourceImage = !string.IsNullOrWhiteSpace(sourceImagePath) && File.Exists(sourceImagePath);
                var hasSelectedAssetImage = !string.IsNullOrWhiteSpace(selectedAssetImagePath) && File.Exists(selectedAssetImagePath);

                _sidebarSourceModelImage.Visible = hasSourceImage;
                _sidebarSelectedAssetImage.Visible = hasSelectedAssetImage;

                if (hasSourceImage && !string.IsNullOrWhiteSpace(sourceImagePath) && File.Exists(sourceImagePath))
                {
                    _sidebarSourceModelImage.Image?.Dispose();
                    _sidebarSourceModelImage.Image = TryLoadBitmap(sourceImagePath);
                }
                else
                {
                    _sidebarSourceModelImage.Image?.Dispose();
                    _sidebarSourceModelImage.Image = null;
                }

                if (hasSelectedAssetImage && !string.IsNullOrWhiteSpace(selectedAssetImagePath) && File.Exists(selectedAssetImagePath))
                {
                    _sidebarSelectedAssetImage.Image?.Dispose();
                    _sidebarSelectedAssetImage.Image = TryLoadBitmap(selectedAssetImagePath);
                }
                else
                {
                    _sidebarSelectedAssetImage.Image?.Dispose();
                    _sidebarSelectedAssetImage.Image = null;
                }
            }
        }
        else
        {
            _sidebarSelectedModelPanel.Visible = false;
            _sidebarSourceModelImage.Visible = false;
            _sidebarSelectedAssetImage.Visible = false;
            _sidebarSelectedModelArrowLabel.Visible = false;
            _sidebarSelectedAssetNameLabel.Visible = false;
            _sidebarSelectedAssetIdLabel.Visible = false;
            _sidebarSourceModelImage.Image?.Dispose();
            _sidebarSourceModelImage.Image = null;
            _sidebarSelectedAssetImage.Image?.Dispose();
            _sidebarSelectedAssetImage.Image = null;
        }

        _sidebarReadmeButton.Visible = shouldDisplaySidebarReadme;
        _sidebarReadmeButton.Text = _localizationService.GetString("OpenReadmeFile", "Open README").Replace(".txt", string.Empty, StringComparison.OrdinalIgnoreCase);
        _sidebarReadmeButton.Enabled = shouldDisplaySidebarReadme;

        _sidebarReadmeHost.Visible = shouldDisplaySidebarReadme;
        _sidebarReadmeHost.Enabled = shouldDisplaySidebarReadme;
        _sidebarReadmeTextBox.Visible = shouldDisplaySidebarReadme;
        _sidebarReadmeTextBox.Enabled = shouldDisplaySidebarReadme;
        _sidebarReadmeTextBox.Text = string.IsNullOrWhiteSpace(readmeText)
            ? _localizationService.GetString("ReadmeFallback", "README")
            : readmeText;
        _sidebarReadmeHost.Height = shouldDisplaySidebarReadme ? GetSidebarReadmeHeight(_sidebarReadmeTextBox.Text, hasSidebarImage) : 0;

        _sidebarStep3GalleryPanel.Visible = isStep3Preview && hasSidebarImage;
        _sidebarStep3GalleryPanel.Controls.Clear();
        _sidebarStep3GalleryPanel.ColumnStyles.Clear();
        _sidebarStep3GalleryPanel.RowStyles.Clear();
        _sidebarStep3GalleryPanel.ColumnCount = 1;
        _sidebarStep3GalleryPanel.RowCount = 1;

        if (isStep3Preview && hasSidebarImage)
        {
            var imageCount = sidebarImageFiles.Count;
            var visibleImages = imageCount > 4
                ? sidebarImageFiles
                    .Skip(_step4ImageIndex % Math.Max(1, imageCount))
                    .Concat(sidebarImageFiles.Take(_step4ImageIndex % Math.Max(1, imageCount)))
                    .Take(4)
                    .ToList()
                : sidebarImageFiles;

            var columnCount = imageCount <= 1 ? 1 : imageCount <= 2 ? 1 : 2;
            var rowCount = imageCount <= 1 ? 1 : imageCount <= 2 ? 2 : 2;
            _sidebarStep3GalleryPanel.ColumnCount = columnCount;
            _sidebarStep3GalleryPanel.RowCount = rowCount;

            for (var c = 0; c < columnCount; c++)
            {
                _sidebarStep3GalleryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            }

            for (var r = 0; r < rowCount; r++)
            {
                _sidebarStep3GalleryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            }

            for (var i = 0; i < visibleImages.Count; i++)
            {
                var imagePath = visibleImages[i];
                var box = new MediaPreviewControl
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 247, 250),
                    Margin = new Padding(0, 0, 4, 4),
                    Visible = true
                };
                if (!box.LoadMedia(imagePath))
                {
                    box.Dispose();
                    continue;
                }
                box.RightClicked += (_, _) =>
                {
                    var selectedIndex = sidebarImageFiles.FindIndex(path => string.Equals(path, imagePath, StringComparison.OrdinalIgnoreCase));
                    OpenFullImageViewer(sidebarImageFiles, Math.Max(0, selectedIndex), imagePath);
                };

                var rowIndex = i / columnCount;
                var columnIndex = i % columnCount;
                if (imageCount == 3 && i == 2)
                {
                    columnIndex = isRtl ? 0 : 1;
                    rowIndex = 1;
                }

                _sidebarStep3GalleryPanel.Controls.Add(box, columnIndex, rowIndex);
            }

            _sidebarStep3GalleryPanel.Height = GetSidebarImageHeight(shouldDisplaySidebarReadme);
            _sidebarStep3GalleryPanel.Margin = new Padding(0, 0, 0, shouldDisplaySidebarReadme ? 8 : 10);
        }

        _sidebarStep4Image.Visible = hasSidebarImage && !isStep3Preview;
        _sidebarStep4Image.Enabled = hasSidebarImage && !isStep3Preview;
        _sidebarStep4Image.Height = hasSidebarImage && !isStep3Preview ? GetSidebarImageHeight(hasSidebarReadme) + (sidebarImageFiles.Count > 1 ? 10 : 0) : 0;
        _sidebarStep4Image.Margin = new Padding(0, 0, 0, hasSidebarReadme ? 8 : 10);

        var shouldShowImageNavigation = hasSidebarImage && sidebarImageFiles.Count > 4 && !isStep3Preview;
        _sidebarImageNavPanel.Visible = shouldShowImageNavigation;
        _sidebarImagePrevButton.Visible = shouldShowImageNavigation;
        _sidebarImageNextButton.Visible = shouldShowImageNavigation;
        _sidebarImagePrevButton.Enabled = shouldShowImageNavigation;
        _sidebarImageNextButton.Enabled = shouldShowImageNavigation;

        if (_currentStep == WizardStep.Step3 && hasSidebarImage)
        {
            _step4ImageTimer.Stop();
            _step5ImageTimer.Stop();
            _sidebarStep3GalleryPanel.Visible = true;
            _sidebarImageNavPanel.Visible = sidebarImageFiles.Count > 4;
            _sidebarImagePrevButton.Visible = sidebarImageFiles.Count > 4;
            _sidebarImageNextButton.Visible = sidebarImageFiles.Count > 4;
            _sidebarImagePrevButton.Enabled = sidebarImageFiles.Count > 4;
            _sidebarImageNextButton.Enabled = sidebarImageFiles.Count > 4;
        }
        else if (_currentStep == WizardStep.Step4 && hasSidebarImage)
        {
            _step5ImageTimer.Stop();
            ShowNextStep4Image(true);
            _step4ImageTimer.Start();
        }
        else if (_currentStep == WizardStep.Step5 && hasSidebarImage)
        {
            _step4ImageTimer.Stop();
            ShowNextStep5Image(true);
            _step5ImageTimer.Start();
        }
        else
        {
            _step4ImageTimer.Stop();
            _step5ImageTimer.Stop();
            _sidebarStep4Image.ClearMedia();
            _sidebarStep4Image.Visible = false;
            _sidebarImageNavPanel.Visible = false;
            _sidebarStep3GalleryPanel.Visible = false;
        }

        switch (_currentStep)
        {
            case WizardStep.Step1:
                _sidebarPreviousButton.Enabled = false;
                _sidebarNextButton.Enabled = GameService.IsValidGameFolder(_selectedGamePath);
                _sidebarNextButton.Text = _localizationService.GetString("Next", "Next");
                break;
            case WizardStep.Step2:
                _sidebarPreviousButton.Enabled = true;
                _sidebarNextButton.Enabled = true;
                _sidebarNextButton.Text = _localizationService.GetString("Next", "Next");
                break;
            case WizardStep.Step3:
                _sidebarPreviousButton.Enabled = true;
                _sidebarNextButton.Enabled = !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath);
                _sidebarNextButton.Text = _localizationService.GetString("InstallMod", "Install Mod");
                break;
            case WizardStep.Step4:
                _sidebarPreviousButton.Enabled = false;
                _sidebarNextButton.Enabled = _returnedToInstallStepFromCompletion || _selectedAssetForInstall != null || _selectedModManifest?.IsSingleAssetPackage == true || _selectedModManifest?.IsMultiAssetPackage == true;
                _sidebarNextButton.Text = _returnedToInstallStepFromCompletion
                    ? _localizationService.GetString("Next", "Next")
                    : _localizationService.GetString("Installing", "Installing");
                break;
            case WizardStep.Step5:
                _sidebarPreviousButton.Enabled = true;
                _sidebarNextButton.Enabled = _selectedAssetForInstall != null;
                _sidebarNextButton.Text = _selectedModManifest?.IsMultiAssetPackage == true
                    ? _localizationService.GetString("Next", "Next")
                    : _localizationService.GetString("InstallMod", "Install Mod");
                break;
            case WizardStep.Step6:
                _sidebarPreviousButton.Enabled = true;
                _sidebarNextButton.Enabled = false;
                _sidebarNextButton.Text = _localizationService.GetString("Next", "Next");
                break;
        }

        _sidebarPreviousButton.ForeColor = _sidebarPreviousButton.Enabled ? Color.White : Color.FromArgb(148, 163, 184);
        _sidebarNextButton.ForeColor = _sidebarNextButton.Enabled ? Color.White : Color.FromArgb(148, 163, 184);
        _sidebarReadmeButton.ForeColor = _sidebarReadmeButton.Enabled ? Color.White : Color.FromArgb(148, 163, 184);
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
    private void ShowNextStep4Image(bool reset = false)
    {
        if (_sidebarStep4Image == null || _sidebarStep4Image.IsDisposed)
        {
            return;
        }

        var imageFiles = _selectedImageFiles.Where(File.Exists).ToList();
        if (imageFiles.Count == 0)
        {
            _sidebarStep4Image.Visible = false;
            _sidebarStep4Image.Tag = new List<string>();
            return;
        }

        if (reset)
        {
            _step4ImageIndex = 0;
        }
        else
        {
            _step4ImageIndex = (_step4ImageIndex + 1) % imageFiles.Count;
        }

        try
        {
            var imagePath = imageFiles[_step4ImageIndex];
            var loaded = _sidebarStep4Image.LoadMedia(imagePath);
            _sidebarStep4Image.Tag = imageFiles[_step4ImageIndex];
            _sidebarStep4Image.Visible = loaded;

        }
        catch
        {
            _sidebarStep4Image.Visible = false;
        }
    }

    private void ShowNextStep5Image(bool reset = false)
    {
        if (_sidebarStep4Image == null || _sidebarStep4Image.IsDisposed)
        {
            return;
        }

        var imageFiles = GetStep5SidebarImageFiles();
        if (imageFiles.Count == 0)
        {
            _sidebarStep4Image.Visible = false;
            _sidebarStep4Image.Tag = new List<string>();
            return;
        }

        if (reset)
        {
            _step4ImageIndex = 0;
        }
        else
        {
            _step4ImageIndex = (_step4ImageIndex + 1) % imageFiles.Count;
        }

        try
        {
            var imagePath = imageFiles[_step4ImageIndex];
            var loaded = _sidebarStep4Image.LoadMedia(imagePath);
            _sidebarStep4Image.Tag = imageFiles[_step4ImageIndex];
            _sidebarStep4Image.Visible = loaded;

        }
        catch
        {
            _sidebarStep4Image.Visible = false;
        }
    }

    private void SidebarImageClick(object? sender, EventArgs e)
    {
        if (_sidebarStep4Image == null || _sidebarStep4Image.IsDisposed)
        {
            return;
        }

        var imageFiles = _currentStep == WizardStep.Step4
            ? _selectedImageFiles.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : GetStep5SidebarImageFiles();

        if (imageFiles.Count == 0)
        {
            return;
        }

        var selectedIndex = Math.Clamp(_step4ImageIndex, 0, imageFiles.Count - 1);
        OpenFullImageViewer(imageFiles, selectedIndex, _selectedModName);
    }

    private void SidebarImageMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        SidebarImageClick(sender, e);
    }

    private void HandleSidebarPrevious()
    {
        if (_currentStep == WizardStep.Step2)
        {
            NavigateToStep(WizardStep.Step1);
            return;
        }

        if (_currentStep == WizardStep.Step3)
        {
            NavigateToStep(WizardStep.Step2);
            return;
        }

        if (_currentStep == WizardStep.Step5)
        {
            _returnedToInstallStepFromCompletion = true;
            NavigateToStep(WizardStep.Step4);
            return;
        }

        if (_currentStep == WizardStep.Step6)
        {
            if (_selectedModManifest?.IsSingleAssetPackage == true || _selectedModManifest?.IsMultiAssetPackage == true)
            {
                _returnedToInstallStepFromCompletion = true;
                NavigateToStep(WizardStep.Step5);
            }
            else
            {
                _returnedToInstallStepFromCompletion = true;
                NavigateToStep(WizardStep.Step4);
            }
            return;
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
    private async void HandleSidebarNext()
    {
        try
        {
            switch (_currentStep)
            {
                case WizardStep.Step1:
                    if (GameService.IsValidGameFolder(_selectedGamePath))
                    {
                        NavigateToStep(WizardStep.Step2);
                    }
                    break;
                case WizardStep.Step2:
                    NavigateToStep(WizardStep.Step3);
                    break;
                case WizardStep.Step3:
                    if (!string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
                    {
                        await InstallSelectedModAsync();
                    }
                    break;
                case WizardStep.Step5:
                    if (_selectedAssetForInstall != null)
                    {
                        await InstallSelectedModAsync();
                    }
                    break;
                case WizardStep.Step4:
                    if (_returnedToInstallStepFromCompletion)
                    {
                        _returnedToInstallStepFromCompletion = false;
                        NavigateToStep(WizardStep.Step5);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "mod-install-debug.log");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:O}] HandleSidebarNext failed:{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }

            MessageBox.Show($"An unexpected UI error occurred while advancing steps.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}More details were written to:{Environment.NewLine}{logPath}", _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Sidebar.cs
    // -------------------------------------------------------------------------
}
