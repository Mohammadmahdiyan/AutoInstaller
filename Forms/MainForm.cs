using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GtaSaModManager.Models;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();
    private readonly AssetCatalogService _assetCatalogService = new();
    private readonly Dictionary<WizardStep, Panel> _wizardPanels = new();
    private readonly System.Windows.Forms.Timer _completionTimer = new();
    private Panel _wizardHost = null!;
    private Panel _sidebarPanel = null!;
    private Button _sidebarPreviousButton = null!;
    private Button _sidebarNextButton = null!;
    private ComboBox _sidebarLanguageComboBox = null!;
    private ComboBox _sidebarThemeComboBox = null!;
    private Button _sidebarReadmeButton = null!;
    private AppSettings _settings;
    private readonly string _appName = "Mod Manager";
    private string _selectedGamePath = string.Empty;
    private string _selectedModSourcePath = string.Empty;
    private string _selectedModName = string.Empty;
    private string _selectedModPayloadPath = string.Empty;
    private string _selectedModPackageRoot = string.Empty;
    private ModManifest? _selectedModManifest;
    private GameAsset? _selectedAssetForInstall;
    private string _selectedReadmePath = string.Empty;
    private List<string> _selectedImageFiles = new();
    private List<string> _installPaths = new();
    private WizardStep _currentStep = WizardStep.Step1;
    private int _completionSecondsLeft = 6;
    private bool _completionTimerActive;
    private bool _isApplyingLanguage;
    private bool _returnedToInstallStepFromCompletion;

    private enum WizardStep
    {
        Step1,
        Step2,
        Step3,
        Step4,
        Step5,
        Step6
    }

    public MainForm()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        SyncCachedGameDataFromFilesystem();

        _selectedGamePath = IsCachedGameFolderValid() ? _settings.GamePath ?? string.Empty : string.Empty;
        _selectedModSourcePath = !string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder)
            ? _settings.ModSourceFolder
            : string.Empty;

        var startupStep = DetermineFirstRequiredStep();
        _currentStep = startupStep;

        ApplyCurrentLanguage();
        ApplyCurrentTheme();
        ConfigureUi();
        InitializeSidebar();
        InitializeWizard();
        GoToStep(_currentStep);
        RefreshModLibrary();
        RefreshModList();
        UpdateSidebarState();

        // Synchronize UI inputs from cache for the initial step
        SynchronizeStepInputs(_currentStep);
    }

    private void RestoreCachedValueIfMissing(TextBox? textBox, string? cachedValue, Func<string, bool> isValidCachedValue)
    {
        if (textBox == null || textBox.IsDisposed || string.IsNullOrWhiteSpace(cachedValue) || !isValidCachedValue(cachedValue))
        {
            return;
        }

        var currentValue = textBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentValue) || string.Equals(currentValue, cachedValue, StringComparison.OrdinalIgnoreCase))
        {
            textBox.Text = cachedValue.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    private static void ApplyPathSelectorTextBoxStyle(TextBox textBox)
    {
        if (textBox == null || textBox.IsDisposed)
        {
            return;
        }

        textBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        textBox.Height = 38;
        textBox.Margin = new Padding(0);
        textBox.Padding = new Padding(8, 6, 8, 3);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.BackColor = Color.White;
        textBox.ForeColor = SystemColors.WindowText;
        textBox.ReadOnly = true;
        textBox.TextAlign = HorizontalAlignment.Left;
    }

    private static void ApplyBrowseButtonStyle(Button button, Color normalColor)
    {
        if (button == null || button.IsDisposed)
        {
            return;
        }

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(148, 163, 184);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(59, 130, 246);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(96, 165, 250);
        button.Margin = new Padding(12, 0, 0, 0);
        button.Height = 38;
        button.Width = 140;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.ForeColor = Color.White;
        button.BackColor = normalColor;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.EnabledChanged += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = normalColor;
            }
            else
            {
                button.BackColor = Color.FromArgb(203, 213, 225);
                button.ForeColor = Color.FromArgb(71, 85, 105);
            }
        };
        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = normalColor;
            }
        };
        button.MouseDown += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = Color.FromArgb(30, 64, 175);
            }
        };
        button.MouseUp += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
            }
        };
    }

    private void SynchronizeStepInputs(WizardStep step)
    {
        if (step == WizardStep.Step1)
        {
            if (_wizardPanels.TryGetValue(WizardStep.Step1, out var panel))
            {
                var tb = panel.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "Step1GamePathTextBox");
                RestoreCachedValueIfMissing(tb, _settings.GamePath, value => !string.IsNullOrWhiteSpace(value) && Directory.Exists(value) && GameService.IsValidGameFolder(value));
            }

            if (GamePathTextBox != null)
            {
                RestoreCachedValueIfMissing(GamePathTextBox, _settings.GamePath, value => !string.IsNullOrWhiteSpace(value) && Directory.Exists(value) && GameService.IsValidGameFolder(value));
            }
        }

        if (step == WizardStep.Step2)
        {
            if (_wizardPanels.TryGetValue(WizardStep.Step2, out var panel))
            {
                var tb = panel.Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "Step2ModLibraryPathTextBox");
                RestoreCachedValueIfMissing(tb, _settings.ModSourceFolder, value => !string.IsNullOrWhiteSpace(value) && Directory.Exists(value));
            }

            if (ModLibraryPathTextBox != null)
            {
                RestoreCachedValueIfMissing(ModLibraryPathTextBox, _settings.ModSourceFolder, value => !string.IsNullOrWhiteSpace(value) && Directory.Exists(value));
            }
        }
    }

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

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            AutoSize = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };

        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _sidebarPreviousButton = new Button { Text = _localizationService.GetString("Previous", "Previous"), Width = 190, Height = 36, Enabled = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarNextButton = new Button { Text = _localizationService.GetString("Next", "Next"), Width = 190, Height = 36, Enabled = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarLanguageComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarThemeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
        _sidebarReadmeButton = new Button { Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt"), Width = 190, Height = 36, Enabled = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Visible = false };

        _sidebarLanguageComboBox.Items.AddRange(new object[] { "English", "فارسی" });
        _sidebarThemeComboBox.Items.AddRange(new object[] { "System", "Light Blue", "Light Purple", "Light Green", "Light Orange", "Dark Blue", "Dark Purple", "Dark Green", "Dark Red" });

        _sidebarPreviousButton.Click += (_, _) => HandleSidebarPrevious();
        _sidebarNextButton.Click += (_, _) => HandleSidebarNext();
        _sidebarReadmeButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath))
            {
                ShowReadmeDialog(_selectedReadmePath);
            }
        };
        _sidebarLanguageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
        _sidebarThemeComboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;

        stack.Controls.Add(_sidebarPreviousButton, 0, 0);
        stack.Controls.Add(_sidebarNextButton, 0, 1);
        stack.Controls.Add(new Label { Text = _localizationService.GetString("Language", "Language"), AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) }, 0, 2);
        stack.Controls.Add(_sidebarLanguageComboBox, 0, 3);
        stack.Controls.Add(new Label { Text = _localizationService.GetString("Theme", "Theme"), AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) }, 0, 4);
        stack.Controls.Add(_sidebarThemeComboBox, 0, 5);
        stack.Controls.Add(_sidebarReadmeButton, 0, 6);

        _sidebarPanel.Controls.Add(stack);
        MainPanel.Controls.Add(_sidebarPanel);
        _sidebarPanel.BringToFront();

        ApplySidebarDirection();
    }

    private void InitializeWizard()
    {
        if (MainPanel == null)
        {
            return;
        }

        _wizardHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = Color.Transparent,
            Visible = true
        };

        MainPanel.Controls.Add(_wizardHost);
        _wizardHost.BringToFront();

        foreach (var panel in new[] { GamePanel, ModLibraryPanel, ModLoaderPanel })
        {
            if (panel != null)
            {
                panel.Visible = false;
            }
        }

        _wizardPanels[WizardStep.Step1] = CreateWizardStep1();
        _wizardPanels[WizardStep.Step2] = CreateWizardStep2();
        _wizardPanels[WizardStep.Step3] = CreateWizardStep3();
        _wizardPanels[WizardStep.Step4] = CreateWizardStep4();
        _wizardPanels[WizardStep.Step5] = CreateWizardStep5();
        _wizardPanels[WizardStep.Step6] = CreateWizardStep6();

        foreach (var step in _wizardPanels.Values)
        {
            step.Dock = DockStyle.Fill;
            step.Visible = false;
            _wizardHost.Controls.Add(step);
        }

        _completionTimer.Interval = 1000;
        _completionTimer.Tick += (_, _) =>
        {
            if (!_completionTimerActive)
            {
                return;
            }

            _completionSecondsLeft--;
            if (_completionSecondsLeft <= 0)
            {
                _completionTimer.Stop();
                _completionTimerActive = false;
                Application.Exit();
                return;
            }

            if (_wizardPanels.TryGetValue(WizardStep.Step6, out var panel) && panel.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "CountdownLabel") is { } countdownLabel)
            {
                countdownLabel.Text = _localizationService.GetString("CompletedIn", "Completed") + " " + _completionSecondsLeft + "s";
            }
        };
    }

    private Panel CreateWizardStep1()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoSize = true };
        var title = new Label { Text = _localizationService.GetString("Step1GameFolder", "Game folder"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var description = new Label { Text = _localizationService.GetString("GameFolderRequired", "Choose your GTA San Andreas folder."), AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 0, 0, 10) };
        var pathText = new TextBox { Name = "Step1GamePathTextBox", Width = 560, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var browse = new Button { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
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

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        flow.Controls.Add(pathText);
        flow.Controls.Add(browse);

        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
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
        var title = new Label { Text = "Base Mods Folder", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0) };
        var subtitle = new Label { Text = "select base mod for easy access", AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 0, 0, 10) };

        var modBaseText = new TextBox { Name = "Step2ModLibraryPathTextBox", Width = 520, Height = 38, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        var modBaseBrowse = new Button { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
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

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        flow.Controls.Add(modBaseText);
        flow.Controls.Add(modBaseBrowse);

        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
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
        var browse = new Button { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 38, Anchor = AnchorStyles.Left };
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
            selectedName.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;
            UpdateSidebarState();
        };

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        flow.Controls.Add(folderText);
        flow.Controls.Add(browse);

        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
        stack.Controls.Add(title);
        stack.Controls.Add(folderLabel);
        stack.Controls.Add(flow);
        stack.Controls.Add(selectedName);

        panel.Controls.Add(stack);
        return panel;
    }

    private Panel CreateWizardStep4()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Installing", "Installing"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var status = new Label { Name = "ProgressStatus", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        var progressBar = new ProgressBar { Width = 680, Height = 24, Minimum = 0, Maximum = 100, Value = 0 };
        var fileList = new ListBox { Width = 680, Height = 180, BorderStyle = BorderStyle.FixedSingle };
        var readme = new TextBox { Width = 680, Height = 180, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
        var gallery = new FlowLayoutPanel { Width = 680, Height = 180, AutoScroll = true, WrapContents = true };

        panel.Controls.Add(title);
        panel.Controls.Add(status);
        panel.Controls.Add(progressBar);
        panel.Controls.Add(fileList);
        panel.Controls.Add(readme);
        panel.Controls.Add(gallery);
        title.Location = new Point(18, 18);
        status.Location = new Point(18, 58);
        progressBar.Location = new Point(18, 88);
        fileList.Location = new Point(18, 120);
        readme.Location = new Point(18, 120);
        gallery.Location = new Point(18, 120);
        readme.Visible = false;
        gallery.Visible = false;
        return panel;
    }

    private Panel CreateWizardStep5()
    {
        var panel = new Panel { Name = "AssetStepPanel", BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18), AutoScroll = true };
        var title = new Label { Text = _localizationService.GetString("Step5Category", "Cars / Weapons / Skins"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Name = "AssetStepDescription", Text = _localizationService.GetString("CategoryPlaceholder", "Select an asset to install."), AutoSize = true, MaximumSize = new Size(900, 0), Font = new Font("Segoe UI", 11F) };
        var categoryLabel = new Label { Text = "Category", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var categoryFilter = new ComboBox { Name = "AssetCategoryFilter", Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        var columnsLabel = new Label { Text = "Columns", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(18, 6, 0, 0) };
        var columns = new ComboBox { Name = "AssetColumns", Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        columns.Items.AddRange(new object[] { "2", "3", "4", "5" });
        columns.SelectedItem = "3";
        var filters = new FlowLayoutPanel { Name = "AssetFilters", AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 12, 0, 8) };
        filters.Controls.Add(categoryLabel);
        filters.Controls.Add(categoryFilter);
        filters.Controls.Add(columnsLabel);
        filters.Controls.Add(columns);
        var gallery = new FlowLayoutPanel { Name = "AssetGallery", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = 900, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        var continueButton = new Button { Text = _localizationService.GetString("Continue", "Continue"), Width = 170, Height = 40, Margin = new Padding(0, 16, 0, 0) };

        categoryFilter.SelectedIndexChanged += (_, _) => RefreshAssetStep();
        columns.SelectedIndexChanged += (_, _) => RefreshAssetStep();
        continueButton.Click += (_, _) => GoToStep(WizardStep.Step6);

        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Controls.Add(filters);
        panel.Controls.Add(gallery);
        panel.Controls.Add(continueButton);
        title.Location = new Point(18, 18);
        description.Location = new Point(18, 58);
        filters.Location = new Point(18, 94);
        gallery.Location = new Point(18, 145);
        continueButton.Location = new Point(18, 500);
        return panel;
    }

    private void RefreshAssetStep()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step5, out var panel))
        {
            return;
        }

        var gallery = panel.Controls.Find("AssetGallery", false).FirstOrDefault() as FlowLayoutPanel;
        var categoryFilter = panel.Controls.Find("AssetCategoryFilter", false).FirstOrDefault() as ComboBox;
        if (gallery == null || categoryFilter == null)
        {
            return;
        }

        gallery.Controls.Clear();
        var detectedAssets = _assetCatalogService.FindAssetsInPackage(_selectedModPayloadPath);
        var catalogAssets = _assetCatalogService.LoadAssets();
        var detectedTypes = detectedAssets.Select(asset => asset.AssetType).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assets = detectedTypes.Count == 0
            ? catalogAssets
            : catalogAssets.Where(asset => detectedTypes.Contains(asset.AssetType)).ToList();
        var categories = assets.Select(asset => asset.Category).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category).ToList();
        var selectedCategory = categoryFilter.SelectedItem?.ToString();
        categoryFilter.Items.Clear();
        categoryFilter.Items.Add("All");
        foreach (var category in categories)
        {
            categoryFilter.Items.Add(category);
        }

        categoryFilter.SelectedItem = !string.IsNullOrWhiteSpace(selectedCategory) && categories.Contains(selectedCategory, StringComparer.OrdinalIgnoreCase)
            ? selectedCategory
            : "All";

        var visibleAssets = string.Equals(categoryFilter.SelectedItem?.ToString(), "All", StringComparison.OrdinalIgnoreCase)
            ? assets
            : assets.Where(asset => string.Equals(asset.Category, categoryFilter.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
        var selectedColumns = int.TryParse(panel.Controls.Find("AssetColumns", false).FirstOrDefault() is ComboBox columns
            ? columns.SelectedItem?.ToString()
            : null, out var columnCount) ? Math.Clamp(columnCount, 2, 5) : 3;
        var cardWidth = Math.Max(170, (gallery.Width - (columnCount - 1) * 12) / columnCount);

        foreach (var asset in visibleAssets)
        {
            gallery.Controls.Add(CreateAssetCard(asset, cardWidth));
        }

        if (visibleAssets.Count == 0)
        {
            gallery.Controls.Add(new Label { Text = "No matching assets were found in this Mod.", AutoSize = true, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 12, 0, 0) });
        }
    }

    private Control CreateAssetCard(GameAsset asset, int cardWidth)
    {
        var innerWidth = cardWidth - 18;
        var card = new Panel { Width = cardWidth, Height = 220, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 12, 12), BackColor = Color.White };
        var preview = new PictureBox { Width = innerWidth, Height = 145, Location = new Point(8, 8), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(245, 247, 250) };
        var name = new Label { Text = asset.Name + " (" + asset.NameFile + ")", AutoSize = false, Width = innerWidth, Height = 38, Location = new Point(8, 158), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var id = new Label { Text = string.IsNullOrWhiteSpace(asset.Id) ? asset.AssetType : asset.AssetType + "  ID: " + asset.Id, AutoSize = false, Width = innerWidth, Height = 18, Location = new Point(8, 198), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(71, 85, 105) };
        var imagePath = _assetCatalogService.ResolveImagePath(asset);
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            try
            {
                using var sourceImage = Image.FromFile(imagePath);
                preview.Image = new Bitmap(sourceImage);
            }
            catch (Exception) when (imagePath.Contains("Image_not_available", StringComparison.OrdinalIgnoreCase))
            {
                preview.Image = null;
            }
            catch
            {
                var fallback = _assetCatalogService.ResolveImagePath(new GameAsset());
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    using var fallbackImage = Image.FromFile(fallback);
                    preview.Image = new Bitmap(fallbackImage);
                }
            }
        }

        card.Controls.Add(preview);
        card.Controls.Add(name);
        card.Controls.Add(id);
        void SelectAsset(object? _, EventArgs __)
        {
            _selectedAssetForInstall = asset;
            foreach (var sibling in card.Parent?.Controls.OfType<Panel>() ?? Enumerable.Empty<Panel>())
            {
                sibling.BackColor = Color.White;
            }

            card.BackColor = Color.FromArgb(219, 234, 254);
            UpdateSidebarState();
        }

        card.Click += SelectAsset;
        preview.Click += SelectAsset;
        name.Click += SelectAsset;
        id.Click += SelectAsset;
        return card;
    }

    private Panel CreateWizardStep6()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Completed", "Completed"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("InstallationComplete", "Installation complete."), AutoSize = true, Font = new Font("Segoe UI", 11F) };
        var countdown = new Label { Name = "CountdownLabel", AutoSize = true, Font = new Font("Segoe UI", 10F) };
        var openButton = new Button { Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder"), Width = 180, Height = 42 };
        var runButton = new Button { Text = _localizationService.GetString("RunGame", "Run Game"), Width = 150, Height = 42 };
        var gallery = new FlowLayoutPanel { Width = 680, Height = 180, AutoScroll = true, WrapContents = true };

        openButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedGamePath) && Directory.Exists(_selectedGamePath))
            {
                Process.Start(new ProcessStartInfo { FileName = _selectedGamePath, UseShellExecute = true, Verb = "open" });
            }
        };

        runButton.Click += (_, _) => GameService.LaunchGame(_selectedGamePath);

        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(openButton);
        flow.Controls.Add(runButton);

        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Controls.Add(flow);
        panel.Controls.Add(countdown);
        panel.Controls.Add(gallery);
        title.Location = new Point(18, 18);
        description.Location = new Point(18, 58);
        flow.Location = new Point(18, 90);
        countdown.Location = new Point(18, 160);
        gallery.Location = new Point(18, 190);
        panel.AutoScroll = true;
        return panel;
    }

    private void GoToStep(WizardStep step)
    {
        _currentStep = step;
        foreach (var item in _wizardPanels)
        {
            item.Value.Visible = item.Key == step;
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

        if (step == WizardStep.Step6)
        {
            _completionSecondsLeft = 6;
            _completionTimerActive = true;
            _completionTimer.Start();
            if (_wizardPanels[WizardStep.Step6].Controls.OfType<Label>().FirstOrDefault(x => x.Name == "CountdownLabel") is { } countdown)
            {
                countdown.Text = _localizationService.GetString("CompletedIn", "Completed") + " 6s";
            }
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

    private async Task InstallSelectedModAsync()
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
                if (jsonDoc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object || jsonDoc.RootElement.EnumerateObject().Any())
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

        if (_selectedModManifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons")
        {
            if (_selectedAssetForInstall != null)
            {
                await InstallTypedPackageAsync(_selectedModPayloadPath, _selectedModName, _selectedModPackageRoot, _selectedModManifest);
                GoToStep(WizardStep.Step6);
                return;
            }

            _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
            _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
            GoToStep(WizardStep.Step5);
            return;
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
        GoToStep(WizardStep.Step5);
        _selectedReadmePath = FindReadmeFile(targetDir);

        GoToStep(WizardStep.Step6);
    }

    private async Task CopyPayloadWithProgressAsync(string sourceDir, string targetDir)
    {
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        var total = files.Length;
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var relative = Path.GetRelativePath(sourceDir, file).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var destination = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);

            if (_wizardPanels.TryGetValue(WizardStep.Step4, out var panel))
            {
                foreach (var control in panel.Controls)
                {
                    if (control is ProgressBar pb)
                    {
                        pb.Value = Math.Min(100, (int)((i + 1) * 100d / Math.Max(1, total)));
                    }

                    if (control is Label statusLabel && statusLabel.Name == "ProgressStatus")
                    {
                        statusLabel.Text = _localizationService.GetString("Installing", "Installing") + " " + _selectedModName + " - " + Math.Min(100, (int)((i + 1) * 100d / Math.Max(1, total))) + "%";
                    }

                    if (control is ListBox listBox)
                    {
                        listBox.Items.Clear();
                        foreach (var item in files.Select(path => Path.GetRelativePath(sourceDir, path)))
                        {
                            listBox.Items.Add(item);
                        }
                    }
                }
            }

            await Task.Delay(30);
        }
    }

    private static string BuildProfileId(string gameFolder, string executable)
    {
        return System.Text.RegularExpressions.Regex.Replace(gameFolder.Trim(), "[\\/]+", "/") + "|" + executable;
    }

    private static string ResolveDirectoryName(string selectedPath)
    {
        var cleanName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return ModPackageService.NormalizeDisplayName(cleanName ?? "Mod");
    }

    private static bool IsReadmeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var normalized = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

        return normalized.Contains("readme", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindReadmeFile(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return string.Empty;
        }

        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (IsReadmeFileName(name))
            {
                return file;
            }
        }

        return string.Empty;
    }

    private static List<string> FindImageFiles(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return new List<string>();
        }

        return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void ShowReadmeDialog(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        using var form = new Form
        {
            Text = Path.GetFileName(path),
            Width = 760,
            Height = 520,
            StartPosition = FormStartPosition.CenterParent
        };

        var box = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Fill
        };

        box.Text = File.ReadAllText(path);
        form.Controls.Add(box);
        form.ShowDialog(this);
    }

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
        if (ModLoaderFlowPanel == null)
        {
            return;
        }

        ModLoaderFlowPanel.Controls.Clear();
        if (string.IsNullOrWhiteSpace(_selectedGamePath) || !GameService.IsValidGameFolder(_selectedGamePath))
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

        foreach (var mod in ModLoaderService.GetInstalledMods(_selectedGamePath))
        {
            var card = new Panel { Width = 260, Height = 250, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10), Margin = new Padding(10) };
            var preview = new PictureBox { Width = 220, Height = 110, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            var name = new Label { Text = mod.Name, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var status = new Label { Text = mod.Status, AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            var path = new Label { Text = mod.FolderPath, AutoSize = true, MaximumSize = new Size(220, 60), Font = new Font("Segoe UI", 8.5F) };
            var readme = new Button { Text = _localizationService.GetString("ViewReadme", "View README"), Width = 120, Height = 32, Visible = !string.IsNullOrWhiteSpace(mod.ReadmePath) };

            if (File.Exists(mod.PreviewPath))
            {
                preview.Image = Image.FromFile(mod.PreviewPath);
            }

            readme.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(mod.ReadmePath) && File.Exists(mod.ReadmePath))
                {
                    ShowReadmeDialog(mod.ReadmePath);
                }
            };

            card.Controls.Add(preview);
            card.Controls.Add(name);
            card.Controls.Add(status);
            card.Controls.Add(path);
            card.Controls.Add(readme);
            preview.Location = new Point(10, 10);
            name.Location = new Point(10, 128);
            status.Location = new Point(10, 158);
            path.Location = new Point(10, 180);
            readme.Location = new Point(10, 210);
            ModLoaderFlowPanel.Controls.Add(card);
        }
    }

    private void RefreshModLibrary()
    {
        if (ModLibraryListBox == null)
        {
            return;
        }

        ModLibraryListBox.Items.Clear();
        if (string.IsNullOrWhiteSpace(_selectedModSourcePath) || !Directory.Exists(_selectedModSourcePath))
        {
            ModLibraryListBox.Items.Add(_localizationService.GetString("NoModPackagesInLibrary", "No mod packages found in this library yet."));
            return;
        }

        var entries = ModPackageService.DiscoverModPackages(_selectedModSourcePath)
            .Select(item => item.DisplayName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

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

    private async Task InstallTypedPackageAsync(string payloadPath, string modName, string packageRoot, ModManifest manifest)
    {
        var packageFiles = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "mod.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (packageFiles.Count == 0)
        {
            MessageBox.Show(_localizationService.GetString("ModSourceInvalid", "The selected mod package does not contain installable files."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

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

        var progressForm = new InstallProgressForm(_localizationService, modName);
        progressForm.Show(this);
        progressForm.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName);
        try
        {
            var records = new List<ReplaceInstallationRecord>();
            for (var index = 0; index < packageFiles.Count; index++)
            {
                var sourcePath = packageFiles[index];
                var relativePath = Path.GetRelativePath(payloadPath, sourcePath);
                var asset = manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons"
                    ? _selectedAssetForInstall
                    : null;
                if (asset != null
                    && (manifest.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons")
                    && (Path.GetExtension(sourcePath) is ".dff" or ".txd"))
                {
                    relativePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, asset.NameFile + Path.GetExtension(sourcePath));
                }
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
                progressForm.UpdateProgress((int)((index + 1) * 100d / packageFiles.Count) + "%");
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

            progressForm.Complete();
            MessageBox.Show(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshModList();
        }
        catch (Exception ex)
        {
            progressForm.Fail();
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progressForm.Close();
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
            .Where(path => !string.Equals(Path.GetFileName(path), "mod.json", StringComparison.OrdinalIgnoreCase))
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

        var progressForm = new InstallProgressForm(_localizationService, modName);
        progressForm.Show(this);
        progressForm.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName + " (Replacing)" );

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
                progressForm.UpdateProgress(percent + "%");
                await Task.Delay(30);
            }

            progressForm.Complete();
            MessageBox.Show(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progressForm.Fail();
            MessageBox.Show(_localizationService.GetString("InstallationFailed", "The mod could not be installed.") + " " + ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progressForm.Close();
        }
    }

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

    private void ThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var selectedComboBox = sender as ComboBox ?? ThemeComboBox;
        if (selectedComboBox == null || selectedComboBox.SelectedItem == null)
        {
            return;
        }

        var selected = selectedComboBox.SelectedItem.ToString();
        _settings.Theme = ThemeManager.ParseTheme(selected).ToString();
        _settingsService.Save(_settings);
        ApplyCurrentTheme();
        ApplySidebarDirection();
    }

    private void ApplyComboSelectionSafely(ComboBox? comboBox, string displayValue)
    {
        if (comboBox == null || comboBox.IsDisposed)
        {
            return;
        }

        if (!comboBox.Items.Contains(displayValue))
        {
            return;
        }

        if (string.Equals(comboBox.Text, displayValue, StringComparison.Ordinal))
        {
            return;
        }

        comboBox.SelectedIndexChanged -= LanguageComboBox_SelectedIndexChanged;
        comboBox.SelectedIndexChanged -= ThemeComboBox_SelectedIndexChanged;
        try
        {
            comboBox.SelectedItem = displayValue;
        }
        finally
        {
            comboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            comboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;
        }
    }

    private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isApplyingLanguage)
        {
            return;
        }

        var selectedComboBox = sender as ComboBox ?? LanguageComboBox;
        if (selectedComboBox == null || selectedComboBox.IsDisposed || selectedComboBox.SelectedItem == null)
        {
            return;
        }

        var selectedValue = selectedComboBox.SelectedItem.ToString();
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            return;
        }

        var requestedLanguage = selectedValue switch
        {
            "فارسی" => "Persian",
            _ => "English"
        };

        if (string.Equals(_settings.Language, requestedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _isApplyingLanguage = true;
        try
        {
            _settings.Language = requestedLanguage;
            ApplyCurrentLanguage();
            ApplyLocalization();
            ApplyCurrentTheme();
            ApplySidebarDirection();
            UpdateSidebarState();
            RefreshModList();
            RefreshModLibrary();
            Invalidate();
            _settingsService.Save(_settings);
            ApplyComboSelectionSafely(LanguageComboBox, GetLanguageDisplayName(_settings.Language));
            ApplyComboSelectionSafely(_sidebarLanguageComboBox, GetLanguageDisplayName(_settings.Language));
        }
        finally
        {
            _isApplyingLanguage = false;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        var startupStep = DetermineFirstRequiredStep();
        if (_currentStep != startupStep)
        {
            _currentStep = startupStep;
        }

        if (ThemeComboBox != null && !ThemeComboBox.IsDisposed)
        {
            var themeDisplay = ThemeManager.GetDisplayName(ThemeManager.ParseTheme(_settings.Theme));
            ApplyComboSelectionSafely(ThemeComboBox, themeDisplay);
        }

        if (LanguageComboBox != null && !LanguageComboBox.IsDisposed)
        {
            ApplyComboSelectionSafely(LanguageComboBox, GetLanguageDisplayName(_settings.Language));
        }

        if (_sidebarThemeComboBox != null && !_sidebarThemeComboBox.IsDisposed)
        {
            var themeDisplay = ThemeManager.GetDisplayName(ThemeManager.ParseTheme(_settings.Theme));
            ApplyComboSelectionSafely(_sidebarThemeComboBox, themeDisplay);
        }

        if (_sidebarLanguageComboBox != null && !_sidebarLanguageComboBox.IsDisposed)
        {
            ApplyComboSelectionSafely(_sidebarLanguageComboBox, GetLanguageDisplayName(_settings.Language));
        }

        ApplyCurrentTheme();
        ApplyLocalization();
        UpdateSidebarState();
        GoToStep(_currentStep);
    }

    private static string GetLanguageDisplayName(string language)
    {
        return string.Equals(language, "Persian", StringComparison.OrdinalIgnoreCase) ? "فارسی" : "English";
    }

    private void ApplyCurrentLanguage()
    {
        var culture = _localizationService.GetCultureForLanguage(_settings.Language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        ApplyRtlForLanguage(_localizationService.ParseLanguage(_settings.Language));
        ApplyDirectionalState(this, _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian);
    }

    private void RebuildWizardPanels()
    {
        if (_wizardHost == null)
        {
            return;
        }

        _wizardPanels.Clear();
        _wizardHost.Controls.Clear();

        _wizardPanels[WizardStep.Step1] = CreateWizardStep1();
        _wizardPanels[WizardStep.Step2] = CreateWizardStep2();
        _wizardPanels[WizardStep.Step3] = CreateWizardStep3();
        _wizardPanels[WizardStep.Step4] = CreateWizardStep4();
        _wizardPanels[WizardStep.Step5] = CreateWizardStep5();
        _wizardPanels[WizardStep.Step6] = CreateWizardStep6();

        foreach (var panel in _wizardPanels.Values)
        {
            panel.Dock = DockStyle.Fill;
            panel.Visible = false;
            _wizardHost.Controls.Add(panel);
        }

        GoToStep(_currentStep);
    }

    private void ApplyCurrentTheme()
    {
        ThemeManager.ApplyTheme(this, ThemeManager.ParseTheme(_settings.Theme));
    }

    private void ApplyRtlForLanguage(SupportedLanguage language)
    {
        RightToLeft = language == SupportedLanguage.Persian ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = language == SupportedLanguage.Persian;
        ApplySidebarDirection();
    }

    private static void ApplyDirectionalState(Control control, bool isRtl)
    {
        control.RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;

        foreach (Control child in control.Controls)
        {
            ApplyDirectionalState(child, isRtl);
        }
    }

    private void ApplySidebarDirection()
    {
        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        if (_sidebarPanel != null)
        {
            _sidebarPanel.Dock = isRtl ? DockStyle.Right : DockStyle.Left;
        }

        if (MainPanel != null)
        {
            MainPanel.RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;
        }
    }

    private void ApplyLocalization()
    {
        if (GameStatusLabel != null && !GameStatusLabel.IsDisposed) GameStatusLabel.Text = _localizationService.GetString("GameStatus", "Game Status");
        if (GamePathLabel != null && !GamePathLabel.IsDisposed) GamePathLabel.Text = _localizationService.GetString("GamePath", "Game Path");
        if (GameFolderNotConfiguredLabel != null && !GameFolderNotConfiguredLabel.IsDisposed) GameFolderNotConfiguredLabel.Text = _localizationService.GetString("GameFolderNotConfigured", "Game folder not configured");
        if (ModLibraryTitleLabel != null && !ModLibraryTitleLabel.IsDisposed) ModLibraryTitleLabel.Text = _localizationService.GetString("ModLibraryTitle", "Mod Library");
        if (ModLibraryPathLabel != null && !ModLibraryPathLabel.IsDisposed) ModLibraryPathLabel.Text = _localizationService.GetString("ModLibraryPath", "Base Mods Folder");
        if (ModLibraryChangeButton != null && !ModLibraryChangeButton.IsDisposed) ModLibraryChangeButton.Text = _localizationService.GetString("Change", "Change");
        if (ModLibraryOpenButton != null && !ModLibraryOpenButton.IsDisposed) ModLibraryOpenButton.Text = _localizationService.GetString("OpenFolder", "Open Folder");
        if (OpenGameFolderButton != null && !OpenGameFolderButton.IsDisposed) OpenGameFolderButton.Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder");
        if (RunGameButton != null && !RunGameButton.IsDisposed) RunGameButton.Text = _localizationService.GetString("RunGame", "Run Game");
        if (InstallModButton != null && !InstallModButton.IsDisposed) InstallModButton.Text = _localizationService.GetString("InstallMod", "Install Mod");
        if (ModLoaderTitleLabel != null && !ModLoaderTitleLabel.IsDisposed) ModLoaderTitleLabel.Text = _localizationService.GetString("ModLoaderMods", "ModLoader Mods");
        if (ThemeLabel != null && !ThemeLabel.IsDisposed) ThemeLabel.Text = _localizationService.GetString("Theme", "Theme");
        if (LanguageLabel != null && !LanguageLabel.IsDisposed) LanguageLabel.Text = _localizationService.GetString("Language", "Language");

        if (_sidebarPreviousButton != null && !_sidebarPreviousButton.IsDisposed) _sidebarPreviousButton.Text = _localizationService.GetString("Previous", "Previous");
        if (_sidebarNextButton != null && !_sidebarNextButton.IsDisposed) _sidebarNextButton.Text = _localizationService.GetString("Next", "Next");
        if (_sidebarReadmeButton != null && !_sidebarReadmeButton.IsDisposed) _sidebarReadmeButton.Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt");

        ApplyComboSelectionSafely(_sidebarLanguageComboBox, GetLanguageDisplayName(_settings.Language));
        ApplyComboSelectionSafely(LanguageComboBox, GetLanguageDisplayName(_settings.Language));

        foreach (var panel in _wizardPanels.Values.Where(p => p != null && !p.IsDisposed).ToList())
        {
            foreach (var control in panel.Controls.Cast<Control>().ToList())
            {
                if (control is Label label && label.Name == "ProfileSummary")
                {
                    label.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;
                }
                else if (control is Button button && button.Name == "NextActionButton")
                {
                    button.Text = _localizationService.GetString("Next", "Next");
                }
                else if (control is Button button2 && button2.Name == "InstallActionButton")
                {
                    button2.Text = _localizationService.GetString("InstallMod", "Install Mod");
                }
                else if (control is Button button3 && button3.Name == "ContinueButton")
                {
                    button3.Text = _localizationService.GetString("Continue", "Continue");
                }
            }
        }

        UpdateSidebarState();
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
        _sidebarReadmeButton.Visible = _currentStep is WizardStep.Step4 or WizardStep.Step6;
        _sidebarReadmeButton.Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt");
        _sidebarReadmeButton.Enabled = !string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath);

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
                _sidebarNextButton.Enabled = _returnedToInstallStepFromCompletion;
                _sidebarNextButton.Text = _returnedToInstallStepFromCompletion
                    ? _localizationService.GetString("Next", "Next")
                    : _localizationService.GetString("Installing", "Installing");
                break;
            case WizardStep.Step5:
                _sidebarPreviousButton.Enabled = true;
                _sidebarNextButton.Enabled = _selectedAssetForInstall != null;
                _sidebarNextButton.Text = _selectedModManifest?.NormalizedType is "vehicleandskinandweapon" or "vehiclesandskinsandweapons"
                    ? _localizationService.GetString("InstallMod", "Install Mod")
                    : _localizationService.GetString("Next", "Next");
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
            NavigateToStep(WizardStep.Step4);
            return;
        }

        if (_currentStep == WizardStep.Step6)
        {
            _returnedToInstallStepFromCompletion = true;
            NavigateToStep(WizardStep.Step4);
            return;
        }
    }

    private void HandleSidebarNext()
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
                    _ = InstallSelectedModAsync();
                }
                break;
            case WizardStep.Step5:
                if (_selectedAssetForInstall != null)
                {
                    _ = InstallSelectedModAsync();
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
}
