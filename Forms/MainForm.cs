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
    private AppSettings _settings;
    private readonly string _appName = "Mod Manager";
    private string _selectedGamePath = string.Empty;
    private string _selectedModSourcePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        ApplyCurrentLanguage();
        ApplyCurrentTheme();
        ConfigureUi();
        EnsureValidatedApplicationSetup();
        LoadSettingsIntoUi();
        RefreshModList();
        RefreshModLibrary();
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

    private void EnsureValidatedApplicationSetup()
    {
        var needsSave = false;

        if (!GameService.IsValidGameFolder(_settings.GamePath))
        {
            var selectedGamePath = PromptForFolderSelection(_localizationService.GetString("SelectGameFolder", "Select the GTA San Andreas folder"));
            if (GameService.IsValidGameFolder(selectedGamePath))
            {
                _settings.GamePath = selectedGamePath;
                needsSave = true;
                GameService.EnsureModLoaderFolder(selectedGamePath);
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.ModSourceFolder) || !Directory.Exists(_settings.ModSourceFolder))
        {
            var selectedLibraryPath = PromptForFolderSelection(_localizationService.GetString("SelectModLibraryFolder", "Select the Mod Library / Mods Source Folder"));
            if (!string.IsNullOrWhiteSpace(selectedLibraryPath) && Directory.Exists(selectedLibraryPath))
            {
                _settings.ModSourceFolder = selectedLibraryPath;
                needsSave = true;
            }
        }

        if (needsSave)
        {
            _settingsService.Save(_settings);
        }
    }

    private string PromptForFolderSelection(string description)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = description
        };

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
            GameStatusValueLabel.Text = _localizationService.GetString("Configured", "Configured");
            GameFolderNotConfiguredLabel.Visible = false;
        }
        else
        {
            GameStatusValueLabel.Text = _localizationService.GetString("NotConfigured", "Not configured");
            GameFolderNotConfiguredLabel.Visible = true;
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

    private void ApplyCurrentLanguage()
    {
        var culture = _localizationService.GetCultureForLanguage(_settings.Language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        ApplyRtlForLanguage(_localizationService.ParseLanguage(_settings.Language));
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

    private void RefreshModList()
    {
        if (ModLoaderFlowPanel == null)
        {
            return;
        }

        ModLoaderFlowPanel.Controls.Clear();
        if (string.IsNullOrWhiteSpace(_selectedGamePath) || !GameService.IsValidGameFolder(_selectedGamePath))
        {
            var emptyPanel = new Panel
            {
                Width = 500,
                Height = 120,
                BackColor = Color.Transparent
            };

            var label = new Label
            {
                Text = _localizationService.GetString("NoModsInstalled", "No ModLoader mods installed yet."),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            emptyPanel.Controls.Add(label);
            ModLoaderFlowPanel.Controls.Add(emptyPanel);
            return;
        }

        foreach (var mod in ModLoaderService.GetInstalledMods(_selectedGamePath))
        {
            var card = new Panel
            {
                Width = 260,
                Height = 260,
                BackColor = Color.FromArgb(255, 255, 255),
                Margin = new Padding(10),
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            var image = new PictureBox
            {
                Width = 220,
                Height = 110,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Image = mod.PreviewPath != null && File.Exists(mod.PreviewPath) ? Image.FromFile(mod.PreviewPath) : null
            };

            var nameLabel = new Label
            {
                Text = mod.Name,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };

            var statusLabel = new Label
            {
                Text = mod.Status,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            var pathLabel = new Label
            {
                Text = mod.FolderPath,
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(220, 60),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };

            var readmeButton = new Button
            {
                Text = _localizationService.GetString("ViewReadme", "View README"),
                Dock = DockStyle.Bottom,
                Visible = !string.IsNullOrWhiteSpace(mod.ReadmePath),
                Margin = new Padding(0, 8, 0, 0)
            };

            readmeButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(mod.ReadmePath) && File.Exists(mod.ReadmePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = mod.ReadmePath,
                        UseShellExecute = true
                    });
                }
            };

            card.Controls.Add(readmeButton);
            card.Controls.Add(pathLabel);
            card.Controls.Add(statusLabel);
            card.Controls.Add(nameLabel);
            card.Controls.Add(image);
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
            .Select(package => package.DisplayName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
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
            var result = MessageBox.Show(
                string.Format(_localizationService.GetString("DuplicateModPrompt", "A mod named '{0}' already exists. Replace it?"), modName),
                _appName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

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
            var progress = new Progress<string>(message =>
            {
                progressForm.UpdateProgress(message);
            });

            await ModPackageService.CopyDirectoryAsync(payloadPath, targetDir, progress);
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
        ModLibraryPathTextBox.Text = selected;
        RefreshModLibrary();
    }

    private void ModLibraryOpenButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedModSourcePath) || !Directory.Exists(_selectedModSourcePath))
        {
            MessageBox.Show(_localizationService.GetString("InvalidModLibraryFolder", "This folder is not a valid mods library folder."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _selectedModSourcePath,
            UseShellExecute = true,
            Verb = "open"
        });
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
        GamePathTextBox.Text = selected;
        _settings.GamePath = selected;
        _settingsService.Save(_settings);
        GameService.EnsureModLoaderFolder(selected);
        GameStatusValueLabel.Text = _localizationService.GetString("Configured", "Configured");
        GameFolderNotConfiguredLabel.Visible = false;
        OpenGameFolderButton.Enabled = true;
        RunGameButton.Enabled = true;
        RefreshModList();
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
        var selected = LanguageComboBox.Text;
        _settings.Language = selected;
        _settingsService.Save(_settings);
        ApplyCurrentLanguage();
        ConfigureUi();
        LoadSettingsIntoUi();
        RefreshModList();
        RefreshModLibrary();
        ApplyCurrentTheme();
        Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var selectedTheme = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        var themeName = ThemeManager.GetDisplayName(ThemeManager.ParseTheme(_settings.Theme));
        ThemeComboBox.SelectedItem = themeName;
        LanguageComboBox.SelectedItem = _settings.Language;
        ApplyCurrentTheme();
    }
}
