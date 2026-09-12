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
    private readonly Dictionary<WizardStep, Panel> _wizardPanels = new();
    private readonly System.Windows.Forms.Timer _completionTimer = new();
    private AppSettings _settings;
    private readonly string _appName = "Mod Manager";
    private string _selectedGamePath = string.Empty;
    private string _selectedModSourcePath = string.Empty;
    private string _selectedModName = string.Empty;
    private string _selectedModPayloadPath = string.Empty;
    private string _selectedReadmePath = string.Empty;
    private List<string> _selectedImageFiles = new();
    private List<string> _installPaths = new();
    private WizardStep _currentStep = WizardStep.Step1;
    private int _completionSecondsLeft = 6;
    private bool _completionTimerActive;

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
        ApplyCurrentLanguage();
        ApplyCurrentTheme();
        ConfigureUi();
        EnsureValidatedApplicationSetup();
        LoadSettingsIntoUi();
        InitializeWizard();
        AdvanceToValidStep();
        RefreshModLibrary();
        RefreshModList();
    }

    private void ConfigureUi()
    {
        Text = _appName;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 250);
        MinimumSize = new Size(1080, 760);

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
            ModLibraryPathLabel.Text = _localizationService.GetString("ModLibraryPath", "Mods Folder");
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

    private void InitializeWizard()
    {
        if (MainPanel == null)
        {
            return;
        }

        var wizardHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = Color.Transparent,
            Visible = true
        };

        MainPanel.Controls.Add(wizardHost);
        wizardHost.BringToFront();

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
            wizardHost.Controls.Add(step);
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
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Step1GameFolder", "Game folder"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("GameFolderRequired", "Choose your GTA San Andreas folder."), AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F) };
        var pathText = new TextBox { Width = 560, Height = 32, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
        var browse = new Button { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 36 };
        var next = new Button { Text = _localizationService.GetString("Continue", "Continue"), Width = 170, Height = 40, Enabled = false };

        browse.Click += (_, _) =>
        {
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"));
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
            next.Enabled = true;
            GameService.EnsureModLoaderFolder(selected);
        };

        next.Click += (_, _) =>
        {
            if (!GameService.IsValidGameFolder(_selectedGamePath))
            {
                return;
            }

            LoadSettingsIntoUi();
            GoToStep(WizardStep.Step2);
        };

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(pathText);
        flow.Controls.Add(browse);
        flow.Controls.Add(next);

        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Controls.Add(flow);
        title.Location = new Point(18, 18);
        description.Location = new Point(18, 58);
        flow.Location = new Point(18, 100);
        return panel;
    }

    private Panel CreateWizardStep2()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Step2Profile", "Game profile"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var summary = new Label { Name = "ProfileSummary", AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F) };
        var continueButton = new Button { Text = _localizationService.GetString("Continue", "Continue"), Width = 170, Height = 40 };

        continueButton.Click += (_, _) => GoToStep(WizardStep.Step3);

        panel.Controls.Add(title);
        panel.Controls.Add(summary);
        panel.Controls.Add(continueButton);
        title.Location = new Point(18, 18);
        summary.Location = new Point(18, 58);
        continueButton.Location = new Point(18, 140);
        return panel;
    }

    private Panel CreateWizardStep3()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Step3Mod", "Select mod"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var folderLabel = new Label { Text = _localizationService.GetString("ModFolder", "Mod Folder"), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        var folderText = new TextBox { Width = 520, Height = 32, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
        var browse = new Button { Text = _localizationService.GetString("Browse", "Browse"), Width = 140, Height = 36 };
        var install = new Button { Text = _localizationService.GetString("InstallMod", "Install Mod"), Width = 180, Height = 42 };
        var selectedName = new Label { AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F) };

        browse.Click += (_, _) =>
        {
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectModFolder", "Select the mod folder"));
            if (string.IsNullOrWhiteSpace(selected) || !Directory.Exists(selected))
            {
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(selected).Any())
            {
                MessageBox.Show(_localizationService.GetString("ModFolderEmpty", "The selected mod folder is empty or unreadable."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedModName = ResolveDirectoryName(selected);
            _selectedModPayloadPath = selected;
            folderText.Text = selected;
            selectedName.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;
        };

        install.Click += async (_, _) => await InstallSelectedModAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(folderText);
        flow.Controls.Add(browse);
        flow.Controls.Add(install);

        panel.Controls.Add(title);
        panel.Controls.Add(folderLabel);
        panel.Controls.Add(flow);
        panel.Controls.Add(selectedName);
        title.Location = new Point(18, 18);
        folderLabel.Location = new Point(18, 58);
        flow.Location = new Point(18, 88);
        selectedName.Location = new Point(18, 150);
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
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Step5Category", "Cars / Weapons / Skins"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("CategoryPlaceholder", "This category step is reserved for future advanced installers."), AutoSize = true, MaximumSize = new Size(700, 0), Font = new Font("Segoe UI", 11F) };
        var continueButton = new Button { Text = _localizationService.GetString("Continue", "Continue"), Width = 170, Height = 40 };

        continueButton.Click += (_, _) => GoToStep(WizardStep.Step6);

        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Controls.Add(continueButton);
        title.Location = new Point(18, 18);
        description.Location = new Point(18, 58);
        continueButton.Location = new Point(18, 150);
        return panel;
    }

    private Panel CreateWizardStep6()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Completed", "Completed"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("InstallationComplete", "Installation complete."), AutoSize = true, Font = new Font("Segoe UI", 11F) };
        var countdown = new Label { Name = "CountdownLabel", AutoSize = true, Font = new Font("Segoe UI", 10F) };
        var openButton = new Button { Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder"), Width = 180, Height = 42 };
        var runButton = new Button { Text = _localizationService.GetString("RunGame", "Run Game"), Width = 150, Height = 42 };
        var readmeButton = new Button { Text = _localizationService.GetString("ReadMe", "Read Me"), Width = 150, Height = 42, Visible = !string.IsNullOrWhiteSpace(_selectedReadmePath) };
        var gallery = new FlowLayoutPanel { Width = 680, Height = 180, AutoScroll = true, WrapContents = true };

        openButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedGamePath) && Directory.Exists(_selectedGamePath))
            {
                Process.Start(new ProcessStartInfo { FileName = _selectedGamePath, UseShellExecute = true, Verb = "open" });
            }
        };

        runButton.Click += (_, _) => GameService.LaunchGame(_selectedGamePath);
        readmeButton.Click += (_, _) => ShowReadmeDialog(_selectedReadmePath);

        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(openButton);
        flow.Controls.Add(runButton);
        flow.Controls.Add(readmeButton);

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
        return panel;
    }

    private void GoToStep(WizardStep step)
    {
        _currentStep = step;
        foreach (var item in _wizardPanels)
        {
            item.Value.Visible = item.Key == step;
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

    private void AdvanceToValidStep()
    {
        _selectedGamePath = _settings.GamePath ?? string.Empty;
        if (GameService.IsValidGameFolder(_selectedGamePath))
        {
            _selectedModSourcePath = _settings.ModSourceFolder ?? string.Empty;
            _selectedGamePath = _settings.GamePath ?? string.Empty;
            GoToStep(WizardStep.Step2);
            return;
        }

        GoToStep(WizardStep.Step1);
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

    private static string FindReadmeFile(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return string.Empty;
        }

        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("README", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Read Me", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("readme", StringComparison.OrdinalIgnoreCase))
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
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"));
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
            var selected = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"));
            if (!string.IsNullOrWhiteSpace(selected) && Directory.Exists(selected))
            {
                _settings.ModSourceFolder = selected;
                _settingsService.Save(_settings);
            }
        }
    }

    private string PromptForFolderSelection(string description)
    {
        using var folderDialog = new FolderBrowserDialog { Description = description };
        return folderDialog.ShowDialog(this) == DialogResult.OK ? folderDialog.SelectedPath : string.Empty;
    }

    private void LoadSettingsIntoUi()
    {
        _selectedGamePath = _settings.GamePath ?? string.Empty;
        _selectedModSourcePath = _settings.ModSourceFolder ?? string.Empty;

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

        var modName = ModPackageService.ResolveModName(packageRoot, Path.GetFileName(packageRoot));
        var modLoaderFolder = GameService.GetModLoaderFolder(_selectedGamePath);
        var targetDir = Path.Combine(modLoaderFolder, modName);

        if (Directory.Exists(targetDir))
        {
            var result = MessageBox.Show(string.Format(_localizationService.GetString("DuplicateModPrompt", "A mod named '{0}' already exists. Replace it?"), modName), _appName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            Directory.Delete(targetDir, true);
        }

        var progressForm = new InstallProgressForm(_localizationService, modName);
        progressForm.Show(this);
        progressForm.SetStatus(_localizationService.GetString("Installing", "Installing") + " " + modName);

        try
        {
            var progress = new Progress<string>(message => progressForm.UpdateProgress(message));
            await ModPackageService.CopyDirectoryAsync(payloadPath, targetDir, progress);
            progressForm.Complete();
            ModLoaderService.RecordInstallation(modName, packageRoot, targetDir);
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
        var selected = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"));
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
        var selected = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"));
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

    private void ThemeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ThemeComboBox.SelectedItem == null)
        {
            return;
        }

        var selected = ThemeComboBox.SelectedItem.ToString();
        _settings.Theme = ThemeManager.ParseTheme(selected).ToString();
        _settingsService.Save(_settings);
        ApplyCurrentTheme();
    }

    private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (LanguageComboBox.SelectedItem == null)
        {
            return;
        }

        var selectedValue = LanguageComboBox.SelectedItem.ToString();
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            return;
        }

        _settings.Language = selectedValue switch
        {
            "فارسی" => "Persian",
            _ => "English"
        };

        _settingsService.Save(_settings);
        ApplyCurrentLanguage();
        ApplyRtlForLanguage(_localizationService.ParseLanguage(_settings.Language));
        ConfigureUi();
        ApplyCurrentTheme();
        RefreshModList();
        RefreshModLibrary();
        Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (ThemeComboBox != null)
        {
            ThemeComboBox.SelectedItem = ThemeManager.GetDisplayName(ThemeManager.ParseTheme(_settings.Theme));
        }

        if (LanguageComboBox != null)
        {
            LanguageComboBox.SelectedItem = GetLanguageDisplayName(_settings.Language);
        }

        ApplyCurrentTheme();
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

    private void ApplyCurrentTheme()
    {
        ThemeManager.ApplyTheme(this, ThemeManager.ParseTheme(_settings.Theme));
    }

    private void ApplyRtlForLanguage(SupportedLanguage language)
    {
        RightToLeft = language == SupportedLanguage.Persian ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = language == SupportedLanguage.Persian;
    }

    private static void ApplyDirectionalState(Control control, bool isRtl)
    {
        control.RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;

        foreach (Control child in control.Controls)
        {
            ApplyDirectionalState(child, isRtl);
        }
    }
}
