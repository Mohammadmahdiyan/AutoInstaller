using System;
using System.Windows.Forms;
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
        flow.Controls.Add(pathText);
        flow.Controls.Add(browse);

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
        flow.Controls.Add(modBaseText);
        flow.Controls.Add(modBaseBrowse);

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
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoSize = true };
        var title = new Label { Text = _localizationService.GetString("Step3Mod", "Select mod"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var folderLabel = new Label { Text = _localizationService.GetString("ModFolder", "Mod Folder"), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };
        var folderText = new TextBox { Width = 520, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var browse = new RoundedButton { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
        var selectedName = new Label { AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 12, 0, 0) };
        ApplyPathSelectorTextBoxStyle(folderText);
        ApplyBrowseButtonStyle(browse, Color.FromArgb(37, 99, 235));

        browse.Click += (_, _) =>
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
                _selectedModName = string.Empty;
                _selectedModPayloadPath = string.Empty;
                folderText.Text = string.Empty;
                selectedName.Text = string.Empty;
                UpdateSidebarState();
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(selected).Any())
            {
                _selectedModName = string.Empty;
                _selectedModPayloadPath = string.Empty;
                folderText.Text = string.Empty;
                selectedName.Text = string.Empty;
                MessageBox.Show(_localizationService.GetString("ModFolderEmpty", "The selected mod folder is empty or unreadable."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateSidebarState();
                return;
            }

            _selectedModName = ResolveDirectoryName(selected);
            _selectedModPayloadPath = selected;
            _selectedModPackageRoot = selected;
            _selectedModManifest = ModPackageService.ResolveManifest(selected);
            _selectedAssetForInstall = null;
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
            RefreshStep3Images(panel, selected);
            UpdateSidebarState();
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
        flow.Controls.Add(folderText);
        flow.Controls.Add(browse);
        var imageGallery = new FlowLayoutPanel { Name = "Step3ImageGallery", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Margin = new Padding(0, 12, 0, 0) };

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
        stack.Controls.Add(folderLabel);
        stack.Controls.Add(flow);
        stack.Controls.Add(selectedName);
        stack.Controls.Add(imageGallery);

        panel.Controls.Add(stack);
        return panel;
    }

    private static void RefreshStep3Images(Control step3Panel, string modFolder)
    {
        var gallery = step3Panel.Controls.Find("Step3ImageGallery", true).FirstOrDefault() as FlowLayoutPanel;
        if (gallery == null)
        {
            return;
        }

        foreach (Control control in gallery.Controls)
        {
            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
            }

            control.Dispose();
        }

        gallery.Controls.Clear();
        var imageFiles = Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var imageFile in imageFiles)
        {
            try
            {
                var sourceImage = TryLoadBitmap(imageFile);
                if (sourceImage == null)
                {
                    continue;
                }

                var preview = new PictureBox
                {
                    Width = 150,
                    Height = 110,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 247, 250),
                    Image = sourceImage,
                    Margin = new Padding(0, 0, 10, 10)
                };
                var toolTip = new ToolTip();
                toolTip.SetToolTip(preview, Path.GetFileName(imageFile));
                gallery.Controls.Add(preview);
            }
            catch
            {
                // Ignore image formats that Windows cannot decode.
            }
        }
    }
}