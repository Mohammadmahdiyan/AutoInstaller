using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
    private TextBox _sidebarReadmeTextBox = null!;
    private PictureBox _sidebarStep4Image = null!;
    private Panel _sidebarImageNavPanel = null!;
    private Button _sidebarImagePrevButton = null!;
    private Button _sidebarImageNextButton = null!;
    private Label _sidebarDetectedModLabel = null!;
    private readonly System.Windows.Forms.Timer _step4ImageTimer = new();
    private readonly System.Windows.Forms.Timer _step5ImageTimer = new();
    private readonly System.Windows.Forms.Timer _detectedModTimer = new();
    private Label? _detectedModLabel;
    private int _step4ImageIndex;
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
    private bool _isRefreshingAssetStep;
    private Panel? _globalLoadingOverlay;
    private Label? _globalLoadingLabel;
    private readonly System.Windows.Forms.Timer _loadingSpinnerTimer = new();
    private float _loadingSpinnerAngle;
    private readonly List<GameAsset> _step5DetectedAssets = new();
    private readonly HashSet<string> _step5SelectedAssetKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Image> _assetImageCache = new(StringComparer.OrdinalIgnoreCase);
    private int _step5ColumnCount = 3;
    private string _step5CategoryFilter = string.Empty;
    private string _step5SortMode = "Name";

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
        _step5CategoryFilter = _localizationService.GetString("AssetAll", "All");
        ApplyCurrentTheme();
        ConfigureUi();
        InitializeSidebar();
        InitializeWizard();
        _detectedModTimer.Interval = 3000;
        _detectedModTimer.Tick += (_, _) =>
        {
            if (_detectedModLabel != null && !_detectedModLabel.IsDisposed)
            {
                _detectedModLabel.Text = string.Empty;
            }
            _detectedModTimer.Stop();
        };

        _loadingSpinnerTimer.Interval = 30;
        _loadingSpinnerTimer.Tick += (_, _) =>
        {
            _loadingSpinnerAngle = (_loadingSpinnerAngle + 24f) % 360f;

            var spinnerText = GetLoadingSpinnerGlyph(_loadingSpinnerAngle);
            foreach (var spinner in GetVisibleLoadingSpinners())
            {
                if (!spinner.IsDisposed)
                {
                    spinner.Text = spinnerText;
                    spinner.Refresh();
                }
            }
        };

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
        textBox.Height = 42;
        textBox.Margin = new Padding(0);
        textBox.Padding = new Padding(12, 9, 12, 9);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.BackColor = Color.FromArgb(248, 250, 252);
        textBox.ForeColor = Color.FromArgb(15, 23, 42);
        textBox.ReadOnly = true;
        textBox.TextAlign = HorizontalAlignment.Left;
        textBox.Multiline = false;
    }

    private static void ApplyBrowseButtonStyle(Button button, Color normalColor)
    {
        if (button == null || button.IsDisposed)
        {
            return;
        }

        button.TabStop = false;
        button.NotifyDefault(false);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = normalColor;
        button.FlatAppearance.MouseOverBackColor = normalColor;
        button.FlatAppearance.CheckedBackColor = normalColor;
        button.Margin = new Padding(10, 0, 0, 0);
        button.Height = 42;
        button.Width = 140;
        button.Font = new Font("Segoe UI", 9.25F, FontStyle.Bold);
        button.ForeColor = Color.White;
        button.BackColor = normalColor;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(8, 0, 8, 0);
        button.EnabledChanged += (_, _) => button.Invalidate();
    }

    private void EnsureGlobalLoadingOverlay()
    {
        if (_globalLoadingOverlay != null && !_globalLoadingOverlay.IsDisposed)
        {
            return;
        }

        _globalLoadingOverlay = new Panel
        {
            Name = "GlobalLoadingOverlay",
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Color.FromArgb(245, 247, 250),
            BorderStyle = BorderStyle.None,
            Enabled = false
        };

        _globalLoadingLabel = new Label
        {
            Name = "GlobalLoadingLabel",
            AutoSize = true,
            Text = "Loading...",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _globalLoadingOverlay.Controls.Add(_globalLoadingLabel);
        _globalLoadingOverlay.Resize += (_, _) =>
        {
            if (_globalLoadingLabel != null && _globalLoadingOverlay != null)
            {
                _globalLoadingLabel.Location = new Point(
                    (_globalLoadingOverlay.Width - _globalLoadingLabel.Width) / 2,
                    (_globalLoadingOverlay.Height - _globalLoadingLabel.Height) / 2);
            }
        };

        Controls.Add(_globalLoadingOverlay);
        _globalLoadingOverlay.BringToFront();
    }

    private void ShowGlobalLoadingOverlay(string message)
    {
        EnsureGlobalLoadingOverlay();
        if (_globalLoadingLabel != null)
        {
            _globalLoadingLabel.Text = message;
        }

        if (_globalLoadingOverlay != null)
        {
            var snapshot = CaptureBlurredBackgroundForOverlay(this);
            _globalLoadingOverlay.BackgroundImage = snapshot;
            _globalLoadingOverlay.BackgroundImageLayout = ImageLayout.Stretch;
            _globalLoadingOverlay.Visible = true;
            _globalLoadingOverlay.Enabled = true;
            _globalLoadingOverlay.BringToFront();
            _globalLoadingOverlay.Refresh();
            _loadingSpinnerTimer.Start();
        }
    }

    private void HideGlobalLoadingOverlay()
    {
        _loadingSpinnerTimer.Stop();
        if (_globalLoadingOverlay != null)
        {
            _globalLoadingOverlay.Visible = false;
            _globalLoadingOverlay.Enabled = false;
            _globalLoadingOverlay.BackgroundImage = null;
        }
    }

    private static IEnumerable<Label> GetVisibleLoadingSpinners()
    {
        foreach (var control in Application.OpenForms.Cast<Form>().SelectMany(form => form.Controls.Cast<Control>()))
        {
            if (control is Label label && label.Name is "Step4LoadingSpinner")
            {
                yield return label;
            }
        }
    }

    private static string GetLoadingSpinnerGlyph(float angle)
    {
        var frames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var index = (int)((angle / 36f) % frames.Length);
        return frames[index];
    }

    private static Bitmap? CaptureBlurredBackgroundForOverlay(Control target)
    {
        if (target == null || target.IsDisposed || target.Width <= 0 || target.Height <= 0)
        {
            return null;
        }

        try
        {
            var image = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(image);
            g.CopyFromScreen(target.PointToScreen(new Point(0, 0)), new Point(0, 0), target.Size);

            var blurred = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using var blurredGraphics = Graphics.FromImage(blurred);
            var preview = new Rectangle(0, 0, image.Width, image.Height);
            var attrs = new ImageAttributes();
            var matrix = new ColorMatrix(new[]
            {
                new[] { 0.7f, 0, 0, 0, 0 },
                new[] { 0, 0.7f, 0, 0, 0 },
                new[] { 0, 0, 0.7f, 0, 0 },
                new[] { 0, 0, 0, 1f, 0 },
                new[] { 0, 0, 0, 0, 1f }
            });
            attrs.SetColorMatrix(matrix);
            blurredGraphics.DrawImage(image, preview, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
            return blurred;
        }
        catch
        {
            return null;
        }
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var corner = Math.Max(2, radius);
        var x = rect.X;
        var y = rect.Y;
        var width = rect.Width;
        var height = rect.Height;

        path.AddArc(x, y, corner * 2, corner * 2, 180, 90);
        path.AddLine(x + corner, y, x + width - corner, y);
        path.AddArc(x + width - corner * 2, y, corner * 2, corner * 2, 270, 90);
        path.AddLine(x + width, y + corner, x + width, y + height - corner);
        path.AddArc(x + width - corner * 2, y + height - corner * 2, corner * 2, corner * 2, 0, 90);
        path.AddLine(x + width - corner, y + height, x + corner, y + height);
        path.AddArc(x, y + height - corner * 2, corner * 2, corner * 2, 90, 90);
        path.AddLine(x, y + height - corner, x, y + corner);
        path.CloseFigure();
        return path;
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
            RowCount = 9,
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
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
        _sidebarStep4Image = new PictureBox { Width = 190, Height = 110, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(245, 247, 250), Visible = false, Margin = new Padding(0, 0, 0, 10) };
        _sidebarImageNavPanel = new Panel { Dock = DockStyle.Fill, Visible = false, Height = 38, Margin = new Padding(0, 0, 0, 8), BackColor = Color.Transparent };
        _sidebarImagePrevButton = new Button { Name = "SidebarImagePrevButton", Text = "◀", Width = 36, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
        _sidebarImageNextButton = new Button { Name = "SidebarImageNextButton", Text = "▶", Width = 36, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
        _sidebarImagePrevButton.FlatAppearance.BorderSize = 0;
        _sidebarImageNextButton.FlatAppearance.BorderSize = 0;
        _sidebarImagePrevButton.Click += (_, _) =>
        {
            if (_currentStep == WizardStep.Step4)
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
            if (_currentStep == WizardStep.Step4)
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
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 8.5F),
            Visible = false,
            Margin = new Padding(0, 0, 0, 10),
            Height = 120,
            WordWrap = true
        };
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
        stack.Controls.Add(_sidebarDetectedModLabel, 0, 6);
        stack.Controls.Add(_sidebarStep4Image, 0, 7);
        stack.Controls.Add(_sidebarImageNavPanel, 0, 8);
        stack.Controls.Add(_sidebarReadmeTextBox, 0, 9);
        stack.Controls.Add(_sidebarReadmeButton, 0, 10);

        _sidebarPanel.Controls.Add(stack);
        MainPanel.Controls.Add(_sidebarPanel);
        _sidebarPanel.BringToFront();

        ApplySidebarDirection();

        _step4ImageTimer.Interval = 1800;
        _step4ImageTimer.Tick += (_, _) => ShowNextStep4Image();
        _step5ImageTimer.Interval = 3200;
        _step5ImageTimer.Tick += (_, _) => ShowNextStep5Image();
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

    private Panel CreateWizardStep4()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Installing", "Installing"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Dock = DockStyle.Top };
        var status = new Label { Name = "ProgressStatus", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 0) };
        var progressBar = new ProgressBar { Width = 680, Height = 24, Minimum = 0, Maximum = 100, Value = 0, Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 8) };
        var fileList = new ListBox { Name = "Step4FileList", Dock = DockStyle.Bottom, Height = 140, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, 8, 0, 0), Visible = true };
        var previewRoot = new Panel { Name = "Step4PreviewRoot", Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), BackColor = Color.FromArgb(255, 255, 255) };
        var loadingOverlay = new Panel
        {
            Name = "Step4LoadingOverlay",
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(18),
            BorderStyle = BorderStyle.None
        };
        var loadingSpinner = new Label
        {
            Name = "Step4LoadingSpinner",
            Text = "⏳",
            AutoSize = true,
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            TextAlign = ContentAlignment.MiddleCenter
        };
        var loadingText = new Label
        {
            Name = "Step4LoadingText",
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = _localizationService.GetString("LoadingVehicles", "Loading vehicles...")
        };
        loadingSpinner.Anchor = AnchorStyles.None;
        loadingText.Anchor = AnchorStyles.None;
        loadingOverlay.Controls.Add(loadingText);
        loadingOverlay.Controls.Add(loadingSpinner);
        loadingOverlay.Resize += (_, _) =>
        {
            if (loadingOverlay.Width > 0)
            {
                loadingSpinner.Location = new Point((loadingOverlay.Width - loadingSpinner.Width) / 2, (loadingOverlay.Height - loadingSpinner.Height - loadingText.Height - 16) / 2);
                loadingText.Location = new Point((loadingOverlay.Width - loadingText.Width) / 2, loadingSpinner.Bottom + 12);
            }
        };
        var readmeButton = new Button
        {
            Name = "Step4ReadmeButton",
            Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt"),
            AutoSize = true,
            Visible = false,
            Dock = DockStyle.Bottom,
            Height = 36,
            Margin = new Padding(0, 10, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        readmeButton.FlatAppearance.BorderSize = 0;
        readmeButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath))
            {
                ShowReadmeDialog(_selectedReadmePath);
            }
        };

        panel.Controls.Add(title);
        panel.Controls.Add(status);
        panel.Controls.Add(progressBar);
        panel.Controls.Add(previewRoot);
        panel.Controls.Add(fileList);
        panel.Controls.Add(readmeButton);
        panel.Controls.Add(loadingOverlay);
        panel.Controls.SetChildIndex(previewRoot, 3);
        panel.Controls.SetChildIndex(fileList, 4);
        panel.Controls.SetChildIndex(readmeButton, 5);
        panel.Controls.SetChildIndex(loadingOverlay, 6);
        return panel;
    }

    private string GetStep4LoadingText(string? assetType)
    {
        var normalizedType = assetType?.Trim();
        return normalizedType switch
        {
            "Vehicle" => _localizationService.GetString("LoadingVehicles", "Loading vehicles..."),
            "Weapon" => _localizationService.GetString("LoadingWeapons", "Loading weapons..."),
            "Skin" => _localizationService.GetString("LoadingSkins", "Loading skins..."),
            _ => _localizationService.GetString("LoadingVehicles", "Loading vehicles...")
        };
    }

    private void ShowStep4LoadingOverlay(string? assetType)
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var overlay = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4LoadingOverlay");
        var loadingText = overlay?.Controls.OfType<Label>().FirstOrDefault(control => control.Name == "Step4LoadingText");
        var spinner = overlay?.Controls.OfType<Label>().FirstOrDefault(control => control.Name == "Step4LoadingSpinner");
        if (overlay == null || loadingText == null || spinner == null)
        {
            return;
        }

        loadingText.Text = GetStep4LoadingText(assetType);
        overlay.BackgroundImage = CaptureBlurredBackgroundForOverlay(panel);
        overlay.BackgroundImageLayout = ImageLayout.Stretch;
        overlay.BringToFront();
        overlay.Visible = true;
        overlay.Enabled = true;
        overlay.Refresh();
        spinner.Text = "⏳";
        spinner.Refresh();
        _loadingSpinnerTimer.Start();
    }

    private void HideStep4LoadingOverlay()
    {
        _loadingSpinnerTimer.Stop();
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var overlay = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4LoadingOverlay");
        if (overlay == null)
        {
            return;
        }

        overlay.Visible = false;
        overlay.Enabled = false;
        overlay.BackgroundImage = null;
    }

    private async Task ShowStep4LoadingTransitionAsync(string? assetType)
    {
        ShowStep4LoadingOverlay(assetType);
        await Task.Yield();
        HideStep4LoadingOverlay();
    }

    private void RefreshStep4Preview()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var root = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4PreviewRoot");
        var readmeButton = panel.Controls.OfType<Button>().FirstOrDefault(control => control.Name == "Step4ReadmeButton");
        var fileList = panel.Controls.OfType<ListBox>().FirstOrDefault(control => control.Name == "Step4FileList");
        if (root == null)
        {
            return;
        }

        if (fileList != null)
        {
            fileList.Items.Clear();
            foreach (var item in _installPaths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => Path.GetRelativePath(_selectedModPayloadPath, path).Replace('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                fileList.Items.Add(item);
            }
            fileList.Visible = _installPaths.Count > 0;
            fileList.Height = _installPaths.Count > 0 ? 140 : 0;
        }

        root.SuspendLayout();
        root.Controls.Clear();

        var hasReadme = !string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath);
        var imageFiles = _selectedImageFiles.Where(File.Exists).ToList();
        var hasImages = imageFiles.Count > 0;

        if (readmeButton != null)
        {
            readmeButton.Visible = hasReadme;
            readmeButton.Enabled = hasReadme;
            readmeButton.Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt");
        }

        if (!hasReadme && !hasImages)
        {
            root.Controls.Clear();
            root.Visible = false;
            root.Height = 0;
            root.ResumeLayout(true);
            return;
        }

        root.Visible = true;
        root.Height = 0;

        var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0), Margin = new Padding(0), BackColor = Color.White };

        if (hasReadme && hasImages)
        {
            var availableHeight = Math.Max(180, root.ClientSize.Height - 40);
            var readmePanelHeight = Math.Clamp(availableHeight - 160, 180, Math.Max(180, availableHeight));

            var readmePanel = new Panel { Dock = DockStyle.Bottom, Height = readmePanelHeight, Padding = new Padding(0), Margin = new Padding(0, 0, 0, 8) };
            var readmeBox = CreateStep4ReadmeBox(_selectedReadmePath, readmePanelHeight);
            readmeBox.Dock = DockStyle.Fill;
            readmePanel.Controls.Add(readmeBox);

            var imagePanel = CreateStep4ImagePanel(imageFiles);
            imagePanel.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(imagePanel);
            contentPanel.Controls.Add(readmePanel);
            imagePanel.BringToFront();
            readmePanel.BringToFront();
        }
        else if (hasReadme)
        {
            var readmePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            var readmeBox = CreateStep4ReadmeBox(_selectedReadmePath);
            readmeBox.Dock = DockStyle.Fill;
            readmePanel.Controls.Add(readmeBox);
            contentPanel.Controls.Add(readmePanel);
        }
        else
        {
            var imagePanel = CreateStep4ImagePanel(imageFiles);
            imagePanel.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(imagePanel);
        }

        root.Controls.Add(contentPanel);
        contentPanel.BringToFront();
        root.ResumeLayout(true);
        panel.PerformLayout();
    }

    private int CalculateReadmeViewportHeight(string content, int availableWidth, Font font, int minimumHeight, int maximumHeight)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return minimumHeight;
        }

        var textWidth = Math.Max(120, availableWidth - 24);
        var textSize = TextRenderer.MeasureText(content, font, new Size(textWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix);

        var computedHeight = textSize.Height + 20;
        if (computedHeight < minimumHeight)
        {
            return minimumHeight;
        }

        return Math.Min(Math.Max(computedHeight, minimumHeight), maximumHeight);
    }

    private TextBox CreateStep4ReadmeBox(string readmePath, int availableHeight = 220)
    {
        var box = new TextBox
        {
            Name = "Step4ReadmeBox",
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(15, 23, 42),
            WordWrap = true,
            RightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian ? RightToLeft.Yes : RightToLeft.No,
            Dock = DockStyle.Fill,
            Height = availableHeight,
            MinimumSize = new Size(0, 120)
        };

        try
        {
            box.Text = File.ReadAllText(readmePath);
            var computedHeight = CalculateReadmeViewportHeight(box.Text, Math.Max(160, box.Width), box.Font, 120, Math.Max(180, availableHeight));
            box.Height = Math.Min(Math.Max(computedHeight, 120), Math.Max(180, availableHeight));
            if (!string.IsNullOrWhiteSpace(box.Text))
            {
                box.SelectionStart = 0;
                box.SelectionLength = 0;
                box.ScrollToCaret();
            }
        }
        catch
        {
            box.Text = _localizationService.GetString("ReadmeFallback", "README");
        }

        return box;
    }

    private Panel CreateStep4ImagePanel(List<string> imageFiles)
    {
        var basePanel = new Panel { Name = "Step4ImagePanel", BackColor = Color.FromArgb(255, 255, 255), Padding = new Padding(0) };
        if (imageFiles.Count == 0)
        {
            return basePanel;
        }

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        if (imageFiles.Count > 4)
        {
            var slideHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0), BackColor = Color.White };
            var slideView = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = Color.White };
            var prev = new Button { Text = "◀", Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Dock = DockStyle.Left };
            var next = new Button { Text = "▶", Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Dock = DockStyle.Right };
            var index = 0;

            void RenderCurrentSlide()
            {
                slideView.Controls.Clear();
                var preview = CreateImagePreviewCard(imageFiles[index], index, imageFiles);
                preview.Dock = DockStyle.Fill;
                slideView.Controls.Add(preview);
            }

            RenderCurrentSlide();
            prev.Click += (_, _) =>
            {
                index = (index - 1 + imageFiles.Count) % imageFiles.Count;
                RenderCurrentSlide();
            };
            next.Click += (_, _) =>
            {
                index = (index + 1) % imageFiles.Count;
                RenderCurrentSlide();
            };

            if (isRtl)
            {
                prev.Dock = DockStyle.Right;
                next.Dock = DockStyle.Left;
            }

            slideHost.Controls.Add(slideView);
            slideHost.Controls.Add(prev);
            slideHost.Controls.Add(next);
            prev.BringToFront();
            next.BringToFront();
            basePanel.Controls.Add(slideHost);
            return basePanel;
        }

        var grid = new FlowLayoutPanel
        {
            Name = "Step4ImageGrid",
            Dock = DockStyle.Fill,
            WrapContents = true,
            AutoScroll = false,
            FlowDirection = FlowDirection.LeftToRight,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
            Padding = new Padding(0),
            BackColor = Color.White
        };

        var columns = imageFiles.Count switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 4
        };

        foreach (var (imageFile, index) in imageFiles.Select((file, i) => (file, i)))
        {
            var card = CreateImagePreviewCard(imageFile, index, imageFiles);
            var width = imageFiles.Count == 1 ? Math.Max(220, basePanel.Width - 20) : (basePanel.Width - (columns - 1) * 8) / columns;
            card.Width = Math.Max(120, width);
            card.Height = imageFiles.Count switch
            {
                1 => Math.Max(220, basePanel.Height - 20),
                2 => Math.Max(120, (basePanel.Height - 12) / 1),
                _ => Math.Max(120, (basePanel.Height - 12) / Math.Max(1, (imageFiles.Count + columns - 1) / columns))
            };
            card.Margin = new Padding(4);
            card.Tag = imageFile;
            grid.Controls.Add(card);
        }

        basePanel.Controls.Add(grid);
        return basePanel;
    }

    private static void OpenImageViewerFromPictureBox(PictureBox pictureBox, List<string> imageFiles, string? fallbackTitle = null)
    {
        if (pictureBox == null || pictureBox.IsDisposed)
        {
            return;
        }

        var validPaths = imageFiles
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            return;
        }

        var currentIndex = Math.Clamp(Math.Max(0, imageFiles.FindIndex(path => string.Equals(path, pictureBox.Tag?.ToString(), StringComparison.OrdinalIgnoreCase))), 0, validPaths.Count - 1);
        if (pictureBox.Tag is string selectedPath && validPaths.Contains(selectedPath, StringComparer.OrdinalIgnoreCase))
        {
            currentIndex = validPaths.FindIndex(path => string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase));
        }

        var mainForm = pictureBox.FindForm();
        if (mainForm is MainForm parentForm)
        {
            parentForm.OpenFullImageViewer(validPaths, currentIndex, fallbackTitle ?? Path.GetFileName(validPaths[currentIndex]));
        }
    }

    private PictureBox CreateImagePreviewCard(string imagePath, int index, List<string>? imageList = null)
    {
        var items = imageList is { Count: > 0 } ? imageList : new List<string> { imagePath };
        var box = new PictureBox
        {
            Name = $"Step4Image_{index}",
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Cursor = Cursors.Hand,
            Margin = new Padding(4),
            Padding = new Padding(0),
            Dock = DockStyle.Fill,
            Image = TryLoadImage(imagePath),
            Tag = imagePath
        };

        box.Click += (_, _) => OpenImageViewerFromPictureBox(box, items, imagePath);
        box.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                OpenImageViewerFromPictureBox(box, items, imagePath);
            }
        };
        return box;
    }

    private void OpenFullImageViewer(List<string> imagePaths, int selectedIndex, string? fallbackTitle = null)
    {
        var validPaths = imagePaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            return;
        }

        var currentIndex = Math.Clamp(selectedIndex, 0, validPaths.Count - 1);

        using var viewer = new Form
        {
            Text = fallbackTitle ?? Path.GetFileName(validPaths[currentIndex]),
            StartPosition = FormStartPosition.CenterParent,
            WindowState = FormWindowState.Normal,
            Width = 1100,
            Height = 760,
            MinimumSize = new Size(640, 420),
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = true,
            MinimizeBox = true,
            ShowIcon = false,
            BackColor = Color.Black
        };

        var imageBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.None,
            BackColor = Color.Black,
            Image = TryLoadImage(validPaths[currentIndex]),
            Cursor = Cursors.Hand
        };

        imageBox.Click += (_, _) => viewer.Close();

        var closeButton = new Button
        {
            Text = _localizationService.GetString("Close", "Close"),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        var prevButton = new Button
        {
            Text = "◀",
            Width = 44,
            Height = 44,
            Visible = validPaths.Count > 1,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        var nextButton = new Button
        {
            Text = "▶",
            Width = 44,
            Height = 44,
            Visible = validPaths.Count > 1,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        void RenderCurrentImage()
        {
            var selectedPath = validPaths[currentIndex];
            viewer.Text = fallbackTitle ?? Path.GetFileName(selectedPath);
            imageBox.Image?.Dispose();
            imageBox.Image = TryLoadImage(selectedPath);
            prevButton.Visible = validPaths.Count > 1;
            nextButton.Visible = validPaths.Count > 1;
        }

        closeButton.Click += (_, _) => viewer.Close();
        prevButton.Click += (_, _) =>
        {
            currentIndex = (currentIndex - 1 + validPaths.Count) % validPaths.Count;
            RenderCurrentImage();
        };
        nextButton.Click += (_, _) =>
        {
            currentIndex = (currentIndex + 1) % validPaths.Count;
            RenderCurrentImage();
        };

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(15, 23, 42),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        topBar.Controls.Add(closeButton);
        topBar.Controls.Add(nextButton);
        topBar.Controls.Add(prevButton);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Padding = new Padding(10) };
        content.Controls.Add(imageBox);

        viewer.Controls.Add(content);
        viewer.Controls.Add(topBar);
        topBar.BringToFront();
        RenderCurrentImage();
        viewer.ShowDialog(this);
    }

    private static Image? TryLoadImage(string path)
    {
        return TryLoadBitmap(path);
    }

    private static Bitmap? TryLoadBitmap(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var image = System.Drawing.Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            try
            {
                using var webpImage = SixLabors.ImageSharp.Image.Load(path);
                using var pngStream = new MemoryStream();
                webpImage.Save(pngStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                pngStream.Position = 0;
                using var converted = System.Drawing.Image.FromStream(pngStream);
                return new Bitmap(converted);
            }
            catch
            {
                return null;
            }
        }
    }

    private Panel CreateWizardStep5()
    {
        var panel = new Panel { Name = "AssetStepPanel", BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Name = "AssetStepTitle", Text = _localizationService.GetString("AssetStepTitleGeneric", "If you wish to select the model to be replaced..."), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Dock = DockStyle.Top };
        var filters = new FlowLayoutPanel { Name = "AssetFilters", Dock = DockStyle.Top, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 4) };
        var categoryLabel = new Label { Name = "AssetCategoryLabel", Text = _localizationService.GetString("AssetCategory", "Category"), AutoSize = true, Margin = new Padding(0, 7, 8, 0), Visible = false };
        var categoryFilter = new ComboBox { Name = "AssetCategoryFilter", Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
        var columns = new ComboBox { Name = "AssetColumns", Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        var sorting = new ComboBox { Name = "AssetSortMode", Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        var columnsLabel = new Label { Name = "AssetColumnsLabel", Text = _localizationService.GetString("AssetColumns", "Columns"), AutoSize = true, Margin = new Padding(18, 7, 8, 0) };
        var sortLabel = new Label { Name = "AssetSortLabel", Text = _localizationService.GetString("AssetSort", "Sort"), AutoSize = true, Margin = new Padding(18, 7, 8, 0) };
        var allText = _localizationService.GetString("AssetAll", "All");

        columns.Items.AddRange(new object[] { "2", "3", "4", "5" });
        columns.SelectedItem = "3";
        sorting.Items.AddRange(new object[] { _localizationService.GetString("SortByFileName", "Sort by file name"), _localizationService.GetString("SortById", "Sort by ID") });
        sorting.SelectedItem = _localizationService.GetString("SortByFileName", "Sort by file name");

        filters.Controls.Add(categoryLabel);
        filters.Controls.Add(categoryFilter);
        filters.Controls.Add(columnsLabel);
        filters.Controls.Add(columns);
        filters.Controls.Add(sortLabel);
        filters.Controls.Add(sorting);
        var gallery = new FlowLayoutPanel { Name = "AssetGallery", Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 8, 0, 0) };
        gallery.SizeChanged += (_, _) => RefreshAssetStep();

        categoryFilter.SelectedIndexChanged += (_, _) =>
        {
            if (categoryFilter.SelectedItem is string selectedCategory)
            {
                _step5CategoryFilter = selectedCategory;
            }

            RefreshAssetStep();
        };

        columns.SelectedIndexChanged += (_, _) =>
        {
            ApplyStep5ColumnCount(columns);
            RefreshAssetStep();
        };

        sorting.SelectedIndexChanged += (_, _) =>
        {
            _step5SortMode = sorting.SelectedItem?.ToString() == _localizationService.GetString("SortById", "Sort by ID") ? "Id" : "Name";
            RefreshAssetStep();
        };

        panel.Controls.Add(gallery);
        panel.Controls.Add(filters);
        panel.Controls.Add(title);
        return panel;
    }

    private string DetectSourceModelName(string payloadPath)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return string.Empty;
        }

        return Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".dff" or ".txd")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }

    private string DetectAssetTypeByName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return string.Empty;
        }

        return _assetCatalogService.LoadAssets()
            .FirstOrDefault(asset => string.Equals(asset.NameFile, modelName, StringComparison.OrdinalIgnoreCase))?
            .AssetType ?? string.Empty;
    }

    private string GetReplacementTitleForType(string assetType)
    {
        return assetType switch
        {
            "Vehicle" => _localizationService.GetString("AssetStepTitleVehicle", "If you wish to select the Vehicle model to be replaced..."),
            "Weapon" => _localizationService.GetString("AssetStepTitleWeapon", "If you wish to select the Weapon model to be replaced..."),
            "Skin" => _localizationService.GetString("AssetStepTitleSkin", "If you wish to select the Skin model to be replaced..."),
            _ => _localizationService.GetString("AssetStepTitleGeneric", "If you wish to select the model to be replaced...")
        };
    }

    private string GetCurrentStep5AssetType()
    {
        if (_step5DetectedAssets.Count > 0)
        {
            return _step5DetectedAssets[0].AssetType;
        }

        var sourceModel = DetectSourceModelName(_selectedModPayloadPath);
        return DetectAssetTypeByName(sourceModel);
    }

    private void PrepareDetectedAssetStep(string payloadPath, ModManifest? manifest)
    {
        _step5DetectedAssets.Clear();
        _step5SelectedAssetKeys.Clear();
        _selectedAssetForInstall = null;

        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return;
        }

        var sourceModelName = DetectSourceModelName(payloadPath);
        if (string.IsNullOrWhiteSpace(sourceModelName))
        {
            return;
        }

        var detectedType = DetectAssetTypeByName(sourceModelName);
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            return;
        }

        var catalogAssets = _assetCatalogService.LoadAssets()
            .Where(asset => string.Equals(asset.AssetType, detectedType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var asset in catalogAssets)
        {
            _step5DetectedAssets.Add(asset);
        }

        if (_step5DetectedAssets.Count == 0)
        {
            return;
        }

        var sourceAsset = _step5DetectedAssets.FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase))
            ?? _step5DetectedAssets.First();

        _step5SelectedAssetKeys.Add(GetAssetSelectionKey(sourceAsset));
        _selectedAssetForInstall = sourceAsset;
        _step5CategoryFilter = !string.IsNullOrWhiteSpace(sourceAsset.Category)
            ? sourceAsset.Category
            : _localizationService.GetString("AssetAll", "All");
    }

    private static string GetAssetSelectionKey(GameAsset asset)
    {
        return (string.IsNullOrWhiteSpace(asset.AssetType) ? "asset" : asset.AssetType.Trim()) + "|" + (string.IsNullOrWhiteSpace(asset.NameFile) ? asset.Name : asset.NameFile.Trim());
    }

    private Image? LoadCachedImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        if (_assetImageCache.TryGetValue(imagePath, out var cachedImage))
        {
            return cachedImage;
        }

        try
        {
            var bitmap = TryLoadBitmap(imagePath);
            if (bitmap == null)
            {
                return null;
            }

            _assetImageCache[imagePath] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private List<GameAsset> GetSelectedAssetListForInstall(ModManifest manifest, string payloadPath)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return new List<GameAsset>();
        }

        var sourceModelName = DetectSourceModelName(payloadPath);
        var sourceType = DetectAssetTypeByName(sourceModelName);
        var availableAssets = _assetCatalogService.LoadAssets()
            .Where(asset => string.Equals(asset.AssetType, sourceType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (availableAssets.Count == 0)
        {
            return new List<GameAsset>();
        }

        if (_selectedAssetForInstall != null)
        {
            return availableAssets
                .Where(asset => string.Equals(GetAssetSelectionKey(asset), GetAssetSelectionKey(_selectedAssetForInstall), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var defaultSourceAsset = availableAssets.FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase)) ?? availableAssets.First();
        return defaultSourceAsset is null ? new List<GameAsset>() : new List<GameAsset> { defaultSourceAsset };
    }

    private void ApplyStep5ColumnCount(ComboBox? columns)
    {
        if (columns == null)
        {
            return;
        }

        var selectedValue = columns.SelectedItem?.ToString();
        if (int.TryParse(selectedValue, out var parsedColumns))
        {
            _step5ColumnCount = Math.Clamp(parsedColumns, 2, 6);
        }
        else
        {
            _step5ColumnCount = 3;
        }

        Debug.WriteLine($"[Step5] columns changed => {_step5ColumnCount}");
    }

    private void RefreshAssetStep()
    {
        if (_isRefreshingAssetStep)
        {
            return;
        }

        if (!_wizardPanels.TryGetValue(WizardStep.Step5, out var panel))
        {
            return;
        }

        var gallery = panel.Controls.Find("AssetGallery", false).FirstOrDefault() as FlowLayoutPanel;
        var categoryFilter = panel.Controls.Find("AssetCategoryFilter", false).FirstOrDefault() as ComboBox;
        var categoryLabel = panel.Controls.Find("AssetCategoryLabel", false).FirstOrDefault() as Label;
        var title = panel.Controls.Find("AssetStepTitle", false).FirstOrDefault() as Label;
        var columns = panel.Controls.Find("AssetColumns", false).FirstOrDefault() as ComboBox;
        var sortFilter = panel.Controls.Find("AssetSortMode", false).FirstOrDefault() as ComboBox;
        if (gallery == null)
        {
            return;
        }

        if (_step5DetectedAssets.Count == 0 && !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
        {
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
        }

        var sourceType = GetCurrentStep5AssetType();
        if (title != null)
        {
            title.Text = GetReplacementTitleForType(sourceType);
        }

        if (columns != null && columns.SelectedItem == null)
        {
            columns.SelectedItem = "3";
        }

        if (columns != null)
        {
            ApplyStep5ColumnCount(columns);
        }

        if (sortFilter != null && string.IsNullOrWhiteSpace(sortFilter.SelectedItem?.ToString()) == false)
        {
            _step5SortMode = sortFilter.SelectedItem?.ToString() == _localizationService.GetString("SortById", "Sort by ID") ? "Id" : "Name";
        }

        _isRefreshingAssetStep = true;
        gallery.SuspendLayout();
        gallery.Controls.Clear();

        var assets = _step5DetectedAssets
            .Where(asset => string.IsNullOrWhiteSpace(sourceType) || string.Equals(asset.AssetType, sourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var categories = assets
            .Where(asset => !string.IsNullOrWhiteSpace(asset.Category))
            .Select(asset => asset.Category.Trim())
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasMeaningfulCategories = categories.Count > 0;
        Debug.WriteLine($"[Step5] categories={categories.Count}; selectedFilter={_step5CategoryFilter}; sourceType={sourceType}");

        if (categoryFilter != null)
        {
            categoryFilter.Visible = hasMeaningfulCategories;
            categoryFilter.Enabled = hasMeaningfulCategories;
        }
        if (categoryLabel != null)
        {
            categoryLabel.Visible = hasMeaningfulCategories;
        }

        var allText = _localizationService.GetString("AssetAll", "All");
        if (categoryFilter != null)
        {
            categoryFilter.Items.Clear();
            categoryFilter.Items.Add(allText);
            foreach (var category in categories)
            {
                categoryFilter.Items.Add(category);
            }

            if (string.IsNullOrWhiteSpace(_step5CategoryFilter))
            {
                _step5CategoryFilter = allText;
            }

            var normalizedSelected = NormalizeCategoryValue(_step5CategoryFilter);
            var validSelection = string.Equals(normalizedSelected, NormalizeCategoryValue(allText), StringComparison.OrdinalIgnoreCase)
                || categories.Any(category => string.Equals(NormalizeCategoryValue(category), normalizedSelected, StringComparison.OrdinalIgnoreCase));

            if (!validSelection)
            {
                Debug.WriteLine($"[Step5] category selection reset from '{_step5CategoryFilter}' to '{allText}'");
                _step5CategoryFilter = allText;
            }

            categoryFilter.SelectedItem = _step5CategoryFilter;
            if (categoryFilter.SelectedItem == null)
            {
                categoryFilter.SelectedIndex = 0;
                _step5CategoryFilter = allText;
            }
            else
            {
                _step5CategoryFilter = categoryFilter.SelectedItem.ToString() ?? allText;
            }
        }

        var visibleAssets = assets;
        if (hasMeaningfulCategories && !string.Equals(_step5CategoryFilter, allText, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedSelectedCategory = NormalizeCategoryValue(_step5CategoryFilter);
            visibleAssets = assets
                .Where(asset => string.Equals(NormalizeCategoryValue(asset.Category), normalizedSelectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (_step5SortMode == "Id")
        {
            visibleAssets = visibleAssets
                .OrderBy(asset => string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            visibleAssets = visibleAssets
                .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var availableGalleryWidth = gallery.ClientSize.Width > 0
            ? gallery.ClientSize.Width
            : Math.Max(320, panel.ClientSize.Width - 32);

        var usableGalleryWidth = Math.Max(180, availableGalleryWidth - gallery.Padding.Horizontal);
        var columnGap = 12;
        var columnCount = Math.Max(1, _step5ColumnCount);
        var rawCardWidth = (usableGalleryWidth - (columnCount - 1) * columnGap) / (double)columnCount;
        var baseCardWidth = Math.Max(120, (int)Math.Floor(rawCardWidth));
        var cardHeight = Math.Max(95, (int)Math.Round(baseCardWidth * 0.50d));

        foreach (var asset in visibleAssets)
        {
            var (cardWidth, cardBodyHeight) = GetStep5CardSize(asset.AssetType, baseCardWidth, cardHeight);
            gallery.Controls.Add(CreateAssetCard(asset, cardWidth, cardBodyHeight));
        }

        if (visibleAssets.Count == 0)
        {
            gallery.Controls.Add(new Label { Text = _localizationService.GetString("AssetNoMatching", "No matching assets were found in this Mod."), AutoSize = true, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 12, 0, 0) });
        }

        gallery.ResumeLayout(true);
        _isRefreshingAssetStep = false;
    }

    private static string NormalizeCategoryValue(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        return Regex.Replace(category.Trim(), @"\s+", " ");
    }

    private static (int Width, int Height) GetStep5CardSize(string? assetType, int baseWidth, int baseHeight)
    {
        var normalizedType = assetType?.Trim();
        var width = Math.Max(150, baseWidth);

        if (string.Equals(normalizedType, "Vehicle", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(120, (int)Math.Round(width * 0.62d));
            return (width, height);
        }

        if (string.Equals(normalizedType, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(110, (int)Math.Round(width * 0.88d));
            return (width, height);
        }

        if (string.Equals(normalizedType, "Skin", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(150, (int)Math.Round(width * 1.35d));
            return (width, height);
        }

        return (width, baseHeight);
    }

    private Control CreateAssetCard(GameAsset asset, int cardWidth, int cardHeight)
    {
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        var innerWidth = cardWidth - 18;
        var previewHeight = Math.Max(62, cardHeight - 38);
        var selectionKey = GetAssetSelectionKey(asset);
        var isSelected = _step5SelectedAssetKeys.Contains(selectionKey);
        var card = new Panel
        {
            Width = cardWidth,
            Height = cardHeight,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = isSelected ? palette.AccentSoft : palette.Card,
            ForeColor = palette.TextPrimary,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        var preview = new PictureBox { Width = innerWidth, Height = previewHeight, Location = new Point(8, 8), SizeMode = PictureBoxSizeMode.Zoom, BackColor = palette.SurfaceSecondary, BorderStyle = BorderStyle.None, Cursor = Cursors.Hand };
        var fileName = new Label { Text = asset.NameFile, AutoSize = false, Width = innerWidth, Height = 22, Location = new Point(8, cardHeight - 28), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = palette.TextPrimary, Cursor = Cursors.Hand };
        var name = new Label { Text = asset.Name, AutoSize = false, Width = innerWidth, Height = 20, Location = new Point(8, cardHeight - 28), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = palette.TextSecondary, Visible = false, Cursor = Cursors.Hand };
        var idText = string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id;
        var id = string.IsNullOrWhiteSpace(idText)
            ? null
            : new Label
            {
                Text = idText,
                Width = 44,
                Height = 22,
                Location = new Point(8, previewHeight + 6),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = GetAssetTypeColor(asset.AssetType, palette),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                AutoSize = false,
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Visible = true,
                Tag = $"AssetId:{idText}"
            };

        if (id != null)
        {
            id.TextAlign = ContentAlignment.MiddleCenter;
            id.Margin = new Padding(0);
        }

        var imagePath = _assetCatalogService.ResolveImagePath(asset);
        var cachedImage = LoadCachedImage(imagePath);
        if (cachedImage == null)
        {
            preview.Image = null;
            preview.BackColor = palette.SurfaceSecondary;
            preview.Controls.Add(new Label
            {
                Text = _localizationService.GetString("ImageUnavailableFriendly", "No image available"),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = palette.TextSecondary,
                BackColor = palette.SurfaceSecondary
            });
        }
        else
        {
            preview.Image = cachedImage;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
        }

        card.Controls.Add(preview);
        card.Controls.Add(fileName);
        card.Controls.Add(name);
        if (id != null)
        {
            card.Controls.Add(id);
        }

        var tooltip = new ToolTip
        {
            AutoPopDelay = 2000,
            InitialDelay = 250,
            ReshowDelay = 100,
            ShowAlways = true
        };
        tooltip.SetToolTip(preview, $"{asset.NameFile}\nID: {asset.Id}");
        if (id != null)
        {
            tooltip.SetToolTip(id, $"{asset.NameFile}\nID: {asset.Id}");
        }

        void ToggleSelection(object? _, EventArgs __)
        {
            _step5SelectedAssetKeys.Clear();
            _step5SelectedAssetKeys.Add(selectionKey);
            _selectedAssetForInstall = asset;

            foreach (var sibling in card.Parent?.Controls.OfType<Panel>() ?? Enumerable.Empty<Panel>())
            {
                sibling.BackColor = sibling == card ? palette.AccentSoft : palette.Card;
                sibling.ForeColor = palette.TextPrimary;
            }

            card.BackColor = palette.AccentSoft;

            UpdateSidebarState();
            RefreshAssetStep();
        }

        void ShowAssetNameHover(object? _, EventArgs __)
        {
            fileName.Visible = false;
            name.Visible = true;
        }

        void ShowAssetFileNameHover(object? _, EventArgs __)
        {
            fileName.Visible = true;
            name.Visible = false;
        }

        preview.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var selectedIndex = _step5DetectedAssets
                .Where(assetItem => string.Equals(assetItem.AssetType, asset.AssetType, StringComparison.OrdinalIgnoreCase))
                .Select(assetItem => assetItem)
                .ToList()
                .FindIndex(item => string.Equals(GetAssetSelectionKey(item), selectionKey, StringComparison.OrdinalIgnoreCase));

            var imageList = _step5DetectedAssets
                .Where(assetItem => string.Equals(assetItem.AssetType, asset.AssetType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.NameFile, StringComparer.OrdinalIgnoreCase)
                .Select(item => _assetCatalogService.ResolveImagePath(item))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Where(path => File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (imageList.Count == 0)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= imageList.Count)
            {
                selectedIndex = 0;
            }

            OpenFullImageViewer(imageList, selectedIndex, imagePath);
        };

        card.Click += ToggleSelection;
        preview.Click += ToggleSelection;
        fileName.Click += ToggleSelection;
        name.Click += ToggleSelection;
        if (id != null)
        {
            id.Click += ToggleSelection;
        }
        card.MouseEnter += ShowAssetNameHover;
        card.MouseLeave += ShowAssetFileNameHover;
        preview.MouseEnter += ShowAssetNameHover;
        preview.MouseLeave += ShowAssetFileNameHover;
        fileName.MouseEnter += ShowAssetNameHover;
        fileName.MouseLeave += ShowAssetFileNameHover;
        name.MouseEnter += ShowAssetNameHover;
        name.MouseLeave += ShowAssetFileNameHover;
        return card;
    }

    private static Color GetAssetTypeColor(string assetType, ThemePalette? palette = null)
    {
        var resolvedPalette = palette ?? ThemeManager.ResolvePalette(AppTheme.LightBlue);
        return assetType.ToLowerInvariant() switch
        {
            "vehicle" => resolvedPalette.Accent,
            "skin" => resolvedPalette.Success,
            "weapon" => resolvedPalette.Error,
            _ => resolvedPalette.TextSecondary
        };
    }

    private Panel CreateWizardStep6()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Completed", "Completed"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("InstallationComplete", "Installation complete."), AutoSize = true, Font = new Font("Segoe UI", 11F) };
        var countdown = new Label { Name = "CountdownLabel", AutoSize = true, Font = new Font("Segoe UI", 10F) };
        var openButton = new Button { Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder"), Width = 180, Height = 42 };
        var runButton = new Button { Text = _localizationService.GetString("RunGame", "Run Game"), Width = 150, Height = 42 };
        var gallery = new FlowLayoutPanel
        {
            Name = "CompletionImageGallery",
            AutoScroll = true,
            WrapContents = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(255, 255, 255)
        };

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
        gallery.Size = new Size(panel.Width - 36, panel.Height - 220);
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
            if (_selectedAssetForInstall != null || _selectedModManifest.IsMultiAssetPackage)
            {
                _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
                _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
                PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
                GoToStep(WizardStep.Step4);
                await InstallTypedPackageAsync(_selectedModPayloadPath, _selectedModName, _selectedModPackageRoot, _selectedModManifest);
                await ShowStep4LoadingTransitionAsync(GetCurrentStep5AssetType());
                GoToStep(WizardStep.Step6);
                return;
            }

            _selectedReadmePath = FindReadmeFile(_selectedModPayloadPath);
            _selectedImageFiles = FindImageFiles(_selectedModPayloadPath);
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
            GoToStep(WizardStep.Step4);
            await CompleteAssetPreparationStepAsync();
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
        await ShowStep4LoadingTransitionAsync(GetCurrentStep5AssetType());
        GoToStep(WizardStep.Step5);
        _selectedReadmePath = FindReadmeFile(targetDir);
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
            var preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 247, 250), Cursor = Cursors.Hand };
            var controlsPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false };
            var prev = new Button { Text = "◀", Width = 70, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
            var next = new Button { Text = "▶", Width = 70, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand };
            var index = 0;

            void RenderCurrentImage()
            {
                if (validImages.Count == 0)
                {
                    preview.Image = null;
                    return;
                }

                var imagePath = validImages[index];
                try
                {
                    var bitmap = TryLoadBitmap(imagePath);
                    preview.Image?.Dispose();
                    preview.Image = bitmap;
                }
                catch
                {
                    preview.Image = null;
                }
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

            preview.Click += (_, _) => OpenFullImageViewer(validImages, index, validImages[index]);
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
                var sourceImage = TryLoadBitmap(imageFile);
                if (sourceImage == null)
                {
                    continue;
                }

                var imageList = validImages;
                var pictureBox = new PictureBox
                {
                    Width = 280,
                    Height = 220,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 247, 250),
                    Image = sourceImage,
                    Margin = new Padding(0, 0, 14, 14),
                    Cursor = Cursors.Hand
                };

                pictureBox.Click += (_, _) =>
                {
                    var selectedIndex = imageList.FindIndex(path => string.Equals(path, imageFile, StringComparison.OrdinalIgnoreCase));
                    if (selectedIndex < 0)
                    {
                        selectedIndex = 0;
                    }

                    OpenFullImageViewer(imageList, selectedIndex, imageFile);
                };
                pictureBox.MouseUp += (_, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        var selectedIndex = imageList.FindIndex(path => string.Equals(path, imageFile, StringComparison.OrdinalIgnoreCase));
                        if (selectedIndex < 0)
                        {
                            selectedIndex = 0;
                        }

                        OpenFullImageViewer(imageList, selectedIndex, imageFile);
                    }
                };

                gallery.Controls.Add(pictureBox);
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
        for (var i = 0; i < files.Count; i++)
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
                || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
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
            var card = new Panel { Width = 260, Height = 250, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10), Margin = new Padding(10) };
            var preview = new PictureBox { Width = 220, Height = 110, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            var name = new Label { Text = mod.Name, AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var status = new Label { Text = mod.Status, AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            var path = new Label { Text = mod.FolderPath, AutoSize = true, MaximumSize = new Size(220, 60), Font = new Font("Segoe UI", 8.5F) };
            var readme = new Button { Text = _localizationService.GetString("ViewReadme", "View README"), Width = 120, Height = 32, Visible = !string.IsNullOrWhiteSpace(mod.ReadmePath) };

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

    private async Task InstallSaveOrDyomPackageAsync(string packageRoot, string modName, ModManifest manifest)
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
        MessageBox.Show(_localizationService.GetString("InstallationCompleted", "Installation completed successfully."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task InstallTypedPackageAsync(string payloadPath, string modName, string packageRoot, ModManifest manifest)
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

        var progressForm = new InstallProgressForm(_localizationService, modName);
        progressForm.Show(this);
        progressForm.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName);
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
                progressForm.UpdateProgress(percent, sourcePath);
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
                progressForm.UpdateProgress(percent, sourceFile);
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

        if (_currentStep == WizardStep.Step5)
        {
            RefreshAssetStep();
        }
    }

    private void ApplyComboSelectionSafely(ComboBox? comboBox, string displayValue)
    {
        if (comboBox == null || comboBox.IsDisposed || !comboBox.IsHandleCreated)
        {
            return;
        }

        var hasDisplayValue = comboBox.Items.Cast<object?>()
            .Any(item => item != null && string.Equals(comboBox.GetItemText(item), displayValue, StringComparison.Ordinal));

        if (!hasDisplayValue)
        {
            return;
        }

        var currentText = comboBox.SelectedItem is null ? comboBox.Text : comboBox.GetItemText(comboBox.SelectedItem);
        if (string.Equals(currentText, displayValue, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            comboBox.SelectedIndexChanged -= LanguageComboBox_SelectedIndexChanged;
            comboBox.SelectedIndexChanged -= ThemeComboBox_SelectedIndexChanged;
            comboBox.SelectedItem = comboBox.Items.Cast<object?>()
                .First(item => item != null && string.Equals(comboBox.GetItemText(item), displayValue, StringComparison.Ordinal));
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        finally
        {
            comboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            comboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;
        }
    }

    private async void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
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

        var selectedValue = selectedComboBox.SelectedItem?.ToString();
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
            ShowGlobalLoadingOverlay(_localizationService.GetString("LoadingVehicles", "Loading vehicles...") /* lightweight refresh indicator */);
            _settings.Language = requestedLanguage;
            ApplyCurrentLanguage();
            ApplyLocalization();
            ApplyCurrentTheme();
            ApplySidebarDirection();
            UpdateSidebarState();
            await RefreshModListAsync();
            await RefreshModLibraryAsync();
            Invalidate();
            _settingsService.Save(_settings);

            if (LanguageComboBox != null && !LanguageComboBox.IsDisposed)
            {
                ApplyComboSelectionSafely(LanguageComboBox, GetLanguageDisplayName(_settings.Language));
            }

            if (_sidebarLanguageComboBox != null && !_sidebarLanguageComboBox.IsDisposed)
            {
                ApplyComboSelectionSafely(_sidebarLanguageComboBox, GetLanguageDisplayName(_settings.Language));
            }
        }
        catch (ObjectDisposedException)
        {
            // Ignore transient disposal during rapid UI rebuilds.
        }
        finally
        {
            HideGlobalLoadingOverlay();
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
                else if (control is Label assetTitle && assetTitle.Name == "AssetStepTitle")
                {
                    assetTitle.Text = GetReplacementTitleForType(GetCurrentStep5AssetType());
                }
                else if (control is Label assetCategory && assetCategory.Name == "AssetCategoryLabel")
                {
                    assetCategory.Text = _localizationService.GetString("AssetCategory", "Category");
                }
                else if (control is Label assetColumnsLabel && assetColumnsLabel.Name == "AssetColumnsLabel")
                {
                    assetColumnsLabel.Text = _localizationService.GetString("AssetColumns", "Columns");
                }
                else if (control is Label assetSortLabel && assetSortLabel.Name == "AssetSortLabel")
                {
                    assetSortLabel.Text = _localizationService.GetString("AssetSort", "Sort");
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

        if (_currentStep == WizardStep.Step5)
        {
            RefreshAssetStep();
        }

        UpdateSidebarState();
    }

    private int GetSidebarReadmeHeight(string? readmeText, bool hasVisibleImage)
    {
        var availableWidth = Math.Max(140, _sidebarReadmeTextBox.Width - 18);
        var normalizedText = string.IsNullOrWhiteSpace(readmeText) ? "README" : readmeText;

        using var graphics = _sidebarReadmeTextBox.CreateGraphics();
        var size = TextRenderer.MeasureText(
            graphics,
            normalizedText,
            _sidebarReadmeTextBox.Font,
            new Size(availableWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding);

        var requiredHeight = size.Height + 16;
        var availableHeight = hasVisibleImage ? 190 : 260;
        var targetHeight = Math.Clamp(requiredHeight, 80, availableHeight);

        _sidebarReadmeTextBox.ScrollBars = requiredHeight > availableHeight ? ScrollBars.Vertical : ScrollBars.None;
        return targetHeight;
    }

    private int GetSidebarImageHeight(bool hasVisibleReadme)
    {
        return hasVisibleReadme ? 130 : 180;
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

        var hasSidebarReadme = !string.IsNullOrWhiteSpace(_selectedReadmePath)
            && File.Exists(_selectedReadmePath);
        var sidebarImageFiles = _currentStep == WizardStep.Step4
            ? _selectedImageFiles.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : GetStep5SidebarImageFiles();
        var hasSidebarImage = sidebarImageFiles.Count > 0;
        var readmeText = hasSidebarReadme ? TryReadTextFile(_selectedReadmePath) : string.Empty;
        var hasDetectedMod = !string.IsNullOrWhiteSpace(_selectedModName) && (_currentStep == WizardStep.Step4 || _currentStep == WizardStep.Step5);

        _sidebarDetectedModLabel.Visible = hasDetectedMod;
        _sidebarDetectedModLabel.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;

        _sidebarReadmeButton.Visible = hasSidebarReadme;
        _sidebarReadmeButton.Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt");
        _sidebarReadmeButton.Enabled = hasSidebarReadme;

        _sidebarReadmeTextBox.Visible = hasSidebarReadme;
        _sidebarReadmeTextBox.Enabled = hasSidebarReadme;
        _sidebarReadmeTextBox.Text = string.IsNullOrWhiteSpace(readmeText)
            ? _localizationService.GetString("ReadmeFallback", "README")
            : readmeText;
        _sidebarReadmeTextBox.Height = GetSidebarReadmeHeight(_sidebarReadmeTextBox.Text, hasSidebarImage);
        _sidebarReadmeTextBox.Margin = new Padding(0, 0, 0, hasSidebarImage ? 8 : 10);

        _sidebarStep4Image.Visible = hasSidebarImage;
        _sidebarStep4Image.Enabled = hasSidebarImage;
        _sidebarStep4Image.Height = GetSidebarImageHeight(hasSidebarReadme) + (sidebarImageFiles.Count > 1 ? 10 : 0);
        _sidebarStep4Image.Margin = new Padding(0, 0, 0, hasSidebarReadme ? 8 : 10);

        _sidebarImageNavPanel.Visible = hasSidebarImage && sidebarImageFiles.Count > 1;
        _sidebarImagePrevButton.Visible = hasSidebarImage && sidebarImageFiles.Count > 1;
        _sidebarImageNextButton.Visible = hasSidebarImage && sidebarImageFiles.Count > 1;
        _sidebarImagePrevButton.Enabled = hasSidebarImage && sidebarImageFiles.Count > 1;
        _sidebarImageNextButton.Enabled = hasSidebarImage && sidebarImageFiles.Count > 1;

        if (_currentStep == WizardStep.Step4 && hasSidebarImage)
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
            _sidebarStep4Image.Image?.Dispose();
            _sidebarStep4Image.Image = null;
            _sidebarStep4Image.Visible = false;
            _sidebarImageNavPanel.Visible = false;
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

    private List<string> GetStep5SidebarImageFiles()
    {
        if (_selectedAssetForInstall == null)
        {
            return new List<string>();
        }

        var imagePath = _assetCatalogService.ResolveImagePath(_selectedAssetForInstall);
        return string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)
            ? new List<string>()
            : new List<string> { imagePath };
    }

    private string TryReadTextFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return string.Empty;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

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
            var sourceImage = TryLoadBitmap(imageFiles[_step4ImageIndex]);
            _sidebarStep4Image.Image?.Dispose();
            _sidebarStep4Image.Image = sourceImage;
            _sidebarStep4Image.Tag = imageFiles[_step4ImageIndex];
            _sidebarStep4Image.Visible = sourceImage != null;

            _sidebarStep4Image.Click -= SidebarImageClick;
            _sidebarStep4Image.Click += SidebarImageClick;
            _sidebarStep4Image.MouseUp -= SidebarImageMouseUp;
            _sidebarStep4Image.MouseUp += SidebarImageMouseUp;
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
            var sourceImage = TryLoadBitmap(imageFiles[_step4ImageIndex]);
            _sidebarStep4Image.Image?.Dispose();
            _sidebarStep4Image.Image = sourceImage;
            _sidebarStep4Image.Tag = imageFiles[_step4ImageIndex];
            _sidebarStep4Image.Visible = sourceImage != null;

            _sidebarStep4Image.Click -= SidebarImageClick;
            _sidebarStep4Image.Click += SidebarImageClick;
            _sidebarStep4Image.MouseUp -= SidebarImageMouseUp;
            _sidebarStep4Image.MouseUp += SidebarImageMouseUp;
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
                else
                {
                    NavigateToStep(WizardStep.Step6);
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
