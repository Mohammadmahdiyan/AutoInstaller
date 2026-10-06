using System;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Models;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Core.cs
    // -------------------------------------------------------------------------
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
    private Panel _sidebarReadmeHost = null!;
    private TableLayoutPanel _sidebarStep3GalleryPanel = null!;
    private Panel _sidebarSelectedModelPanel = null!;
    private PictureBox _sidebarSelectedAssetImage = null!;
    private Label _sidebarSelectedModelArrowLabel = null!;
    private Label _sidebarSelectedAssetNameLabel = null!;
    private Label _sidebarSelectedAssetIdLabel = null!;
    private TableLayoutPanel _sidebarStack = null!;
    private MediaPreviewControl _sidebarStep4Image = null!;
    private Panel _sidebarImageNavPanel = null!;
    private Button _sidebarImagePrevButton = null!;
    private Button _sidebarImageNextButton = null!;
    private Label _sidebarDetectedModLabel = null!;
    private readonly System.Windows.Forms.Timer _step4ImageTimer = new();
    private readonly System.Windows.Forms.Timer _step5ImageTimer = new();
    private readonly System.Windows.Forms.Timer _step5ArrowTimer = new();
    private readonly System.Windows.Forms.Timer _detectedModTimer = new();
    private Label? _detectedModLabel;
    private int _step4ImageIndex;
    private float _step5ArrowPhase;
    private AppSettings _settings;
    private readonly string _appName = "Mod Manager";
    private string _selectedGamePath = string.Empty;
    private string _selectedModSourcePath = string.Empty;
    private string _selectedModName = string.Empty;
    private string _selectedModPayloadPath = string.Empty;
    private string _selectedModPackageRoot = string.Empty;
    private GtaSaModManager.Models.ModManifest? _selectedModManifest;
    private GtaSaModManager.Models.GameAsset? _selectedAssetForInstall;
    private string _selectedReadmePath = string.Empty;
    private List<string> _selectedImageFiles = new();
    private InstallProgressView? _step4ProgressView;
    private WizardStep _currentStep = WizardStep.Step1;
    private int _completionSecondsLeft = 6;
    private bool _completionTimerActive;
    private bool _isApplyingLanguage;
    private bool _returnedToInstallStepFromCompletion;
    private bool _isInstallingOptionalPackage;
    private bool _isDetectedModStatusVisible;
    private bool _lastActionWasDelete;
    private bool _isCheckingPreviousAssetInstallation;
    private bool _isRefreshingAssetStep;
    private DialogResult? _pendingExistingAssetInstallAction;
    private List<string> _pendingAssetModelFilesToDelete = new();
    private Panel? _globalLoadingOverlay;
    private Label? _globalLoadingLabel;
    private Label? _globalLoadingSpinnerLabel;
    private readonly System.Windows.Forms.Timer _loadingSpinnerTimer = new();
    private readonly System.Windows.Forms.Timer _saveMissionSlotAnimationTimer = new();
    private readonly List<string> _saveMissionSlotBaseTexts = new();
    private RadioButton? _saveMissionSlotAnimationButton;
    private Color _saveMissionSlotAnimationStartColor;
    private Color _saveMissionSlotAnimationTargetColor;
    private Color _saveMissionSlotAnimationStartBackColor;
    private Color _saveMissionSlotAnimationTargetBackColor;
    private int _saveMissionSlotAnimationFrame;
    private float _loadingSpinnerAngle;
    private readonly List<GameAsset> _step5DetectedAssets = new();
    private readonly HashSet<string> _step5SelectedAssetKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SourceModel> _multiSourceModels = new();
    private readonly List<SaveMissionSourceFile> _saveMissionPackageFiles = new();
    private readonly List<string> _saveMissionInstalledFiles = new();
    private readonly List<RadioButton> _saveMissionSlotButtons = new();
    private int _multiIndex;
    private int _saveMissionFileIndex;
    private int? _selectedSaveMissionSlot;
    private bool _isSaveMissionStepActive;
    private bool _isUpdatingMultiAssetTabs;
    private Dictionary<string, List<string>> _step5OccupiedAssetFolders = new(StringComparer.OrdinalIgnoreCase);
    private string _step5OccupiedScanKey = string.Empty;
    private bool _step5OccupiedScanComplete;
    private bool _occupiedAssetInstallWarningAcknowledged;
    private readonly Dictionary<string, Image?> _assetImageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _occupiedAssetImageFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _occupiedAssetSourceFolders = new(StringComparer.OrdinalIgnoreCase);
    private string _step5PreparedPayloadPath = string.Empty;
    private string _step5ExistingInstallationDestination = string.Empty;
    private string _step5DetectedAssetType = string.Empty;
    private bool _step5UnknownAssetTypeCancelled;
    private string _lastShownAssetCatalogError = string.Empty;
    private bool _step5PreparationAttempted;
    private int _step5ColumnCount = 3;
    private string _step5CategoryFilter = string.Empty;
    private string _step5SortMode = "Name";
    private const string Step5SortByName = "Name";
    private const string Step5SortById = "Id";
    private const string UnknownAssetCancelModName = "RZL-Skin";

    private enum WizardStep
    {
        Step0,
        Step1,
        Step2,
        Step3,
        Step4,
        Step5,
        Step6
    }

    private sealed record SaveMissionSourceFile(string Path, bool IsMission);

    public MainForm()
    {
        InitializeComponent();
        _saveMissionSlotAnimationTimer.Interval = 15;
        _saveMissionSlotAnimationTimer.Tick += (_, _) => AnimateSaveMissionSlotSelection();
        FormClosed += (_, _) => _saveMissionSlotAnimationTimer.Dispose();
        _settings = _settingsService.Load();
        SyncCachedGameDataFromFilesystem();

        _selectedGamePath = IsCachedGameFolderValid() ? _settings.GamePath ?? string.Empty : string.Empty;
        _selectedModSourcePath = !string.IsNullOrWhiteSpace(_settings.ModSourceFolder) && Directory.Exists(_settings.ModSourceFolder)
            ? _settings.ModSourceFolder
            : string.Empty;

        var startupStep = DetermineStartupStep();
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
                _detectedModLabel.Visible = false;
            }

            _isDetectedModStatusVisible = false;

            if (_sidebarDetectedModLabel != null && !_sidebarDetectedModLabel.IsDisposed)
            {
                _sidebarDetectedModLabel.Text = string.Empty;
                _sidebarDetectedModLabel.Visible = false;
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

            if (_isCheckingPreviousAssetInstallation && _currentStep == WizardStep.Step3
                && _sidebarNextButton != null && !_sidebarNextButton.IsDisposed)
            {
                _sidebarNextButton.Text = spinnerText + " " + _localizationService.GetString("CheckingPreviousInstallation", "Checking previous installation");
                _sidebarNextButton.Refresh();
            }
        };

        GoToStep(_currentStep);
        RefreshModLibrary();
        RefreshModList();
        UpdateSidebarState();

    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Core.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Core.cs
    // -------------------------------------------------------------------------
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

        _wizardPanels[WizardStep.Step0] = CreateWizardStep0();
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












    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Core.cs
    // -------------------------------------------------------------------------
}
