using System;
using System.Globalization;
using System.Windows.Forms;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
   // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------
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

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------
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

        foreach (var root in new Control[] { this, MainPanel, _sidebarPanel, _wizardHost }.Where(control => control != null && !control.IsDisposed).ToList())
        {
            RefreshLocalizedTextForControl(root);
        }

        if (_currentStep == WizardStep.Step5)
        {
            RefreshAssetStep();
        }

        UpdateSidebarState();
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------
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

    private void RefreshLocalizedTextForControl(Control control)
    {
        if (control == null || control.IsDisposed)
        {
            return;
        }

        if (control is Label label)
        {
            switch (label.Name)
            {
                case "ProfileSummary":
                    label.Text = _localizationService.GetString("DetectedMod", "Detected mod") + ": " + _selectedModName;
                    return;
                case "AssetStepTitle":
                    label.Text = GetReplacementTitleForType(GetCurrentStep5AssetType());
                    return;
                case "AssetCategoryLabel":
                    label.Text = _localizationService.GetString("AssetCategory", "Category");
                    return;
                case "AssetColumnsLabel":
                    label.Text = _localizationService.GetString("AssetColumns", "Columns");
                    return;
                case "AssetSortLabel":
                    label.Text = _localizationService.GetString("AssetSort", "Sort");
                    return;
                case "Step4LoadingText":
                    label.Text = GetStep4LoadingText(GetCurrentStep5AssetType());
                    return;
            }
        }
        else if (control is Button button)
        {
            switch (button.Name)
            {
                case "NextActionButton":
                    button.Text = _localizationService.GetString("Next", "Next");
                    return;
                case "InstallActionButton":
                    button.Text = _localizationService.GetString("InstallMod", "Install Mod");
                    return;
                case "ContinueButton":
                    button.Text = _localizationService.GetString("Continue", "Continue");
                    return;
                case "Step4ReadmeButton":
                    button.Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt");
                    return;
            }
        }

        foreach (Control child in control.Controls)
        {
            RefreshLocalizedTextForControl(child);
        }
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

 
 

 

 

 

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Localization.cs
    // -------------------------------------------------------------------------
}
