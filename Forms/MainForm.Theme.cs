using System;
using System.Windows.Forms;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------
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

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------
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

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------
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

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------
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
        textBox.Tag = "BrowseInput";
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

    private Panel CreateBrowseInputGroup(TextBox textBox, Button button, int textWidth)
    {
        const int inputLeftPadding = 10;
        const int inputRightGap = 3;
        const int inputVerticalOffset = 3;

        var group = new Panel
        {
            Width = textWidth + button.Width + 9,
            Height = 48,
            Padding = new Padding(4),
            Margin = new Padding(0),
            BackColor = Color.White,
                BorderStyle = BorderStyle.None
        };

        textBox.BorderStyle = BorderStyle.None;
        textBox.BackColor = Color.White;
    textBox.Location = new Point(inputLeftPadding, Math.Max(0, (group.ClientSize.Height - textBox.Height) / 2 + inputVerticalOffset));
    textBox.Width = textWidth - inputLeftPadding - inputRightGap;
        textBox.Height = 38;
        textBox.Margin = new Padding(0);

        button.Margin = new Padding(0);
        button.Location = new Point(textWidth + 7, (group.ClientSize.Height - button.Height) / 2);
        button.Width = Math.Max(120, button.Width);
        button.Height = 38;

        group.Resize += (_, _) =>
        {
            if (group.Width > 0 && group.Height > 0)
            {
                textBox.Location = new Point(inputLeftPadding, Math.Max(0, (group.ClientSize.Height - textBox.Height) / 2 + inputVerticalOffset));
                button.Location = new Point(textWidth + 7, Math.Max(0, (group.ClientSize.Height - button.Height) / 2));
                group.Region = new Region(CreateRoundedRectanglePath(
                    new Rectangle(0, 0, group.Width, group.Height), 8));
            }
        };
        group.Paint += (_, e) =>
        {
            using var borderPen = new Pen(Color.FromArgb(95, 148, 163, 184));
            e.Graphics.DrawPath(borderPen, CreateRoundedRectanglePath(
                new Rectangle(0, 0, group.Width - 1, group.Height - 1), 8));
        };

        group.Controls.Add(textBox);
        group.Controls.Add(button);
        return group;
    }







    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Theme.cs
    // -------------------------------------------------------------------------
}
