using GtaSaModManager.Models;

namespace GtaSaModManager.UI;

public sealed class ThemePalette
{
    public Color Background { get; init; }
    public Color Surface { get; init; }
    public Color SurfaceSecondary { get; init; }
    public Color Card { get; init; }
    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }
    public Color Accent { get; init; }
    public Color AccentHover { get; init; }
    public Color Border { get; init; }
    public Color Success { get; init; }
    public Color Warning { get; init; }
    public Color Error { get; init; }
}

public static class ThemeManager
{
    private static readonly Dictionary<AppTheme, ThemePalette> PaletteMap = new()
    {
        [AppTheme.LightBlue] = new ThemePalette
        {
            Background = Color.FromArgb(239, 246, 255),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(219, 234, 254),
            Card = Color.FromArgb(255, 255, 255),
            TextPrimary = Color.FromArgb(15, 23, 42),
            TextSecondary = Color.FromArgb(71, 85, 105),
            Accent = Color.FromArgb(59, 130, 246),
            AccentHover = Color.FromArgb(37, 99, 235),
            Border = Color.FromArgb(191, 219, 254),
            Success = Color.FromArgb(16, 185, 129),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(239, 68, 68)
        },
        [AppTheme.LightPurple] = new ThemePalette
        {
            Background = Color.FromArgb(245, 243, 255),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(237, 233, 254),
            Card = Color.FromArgb(255, 255, 255),
            TextPrimary = Color.FromArgb(30, 27, 75),
            TextSecondary = Color.FromArgb(88, 88, 122),
            Accent = Color.FromArgb(139, 92, 246),
            AccentHover = Color.FromArgb(124, 58, 237),
            Border = Color.FromArgb(216, 180, 254),
            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Error = Color.FromArgb(248, 113, 113)
        },
        [AppTheme.LightGreen] = new ThemePalette
        {
            Background = Color.FromArgb(240, 253, 244),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(220, 252, 231),
            Card = Color.FromArgb(255, 255, 255),
            TextPrimary = Color.FromArgb(20, 83, 45),
            TextSecondary = Color.FromArgb(52, 88, 61),
            Accent = Color.FromArgb(34, 197, 94),
            AccentHover = Color.FromArgb(22, 163, 74),
            Border = Color.FromArgb(187, 247, 208),
            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(234, 179, 8),
            Error = Color.FromArgb(239, 68, 68)
        },
        [AppTheme.LightOrange] = new ThemePalette
        {
            Background = Color.FromArgb(255, 247, 237),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(254, 215, 170),
            Card = Color.FromArgb(255, 255, 255),
            TextPrimary = Color.FromArgb(67, 39, 11),
            TextSecondary = Color.FromArgb(122, 90, 62),
            Accent = Color.FromArgb(249, 115, 22),
            AccentHover = Color.FromArgb(234, 88, 12),
            Border = Color.FromArgb(253, 186, 116),
            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(234, 88, 12),
            Error = Color.FromArgb(239, 68, 68)
        },
        [AppTheme.DarkBlue] = new ThemePalette
        {
            Background = Color.FromArgb(9, 15, 29),
            Surface = Color.FromArgb(15, 23, 42),
            SurfaceSecondary = Color.FromArgb(30, 41, 59),
            Card = Color.FromArgb(15, 23, 42),
            TextPrimary = Color.FromArgb(224, 236, 255),
            TextSecondary = Color.FromArgb(148, 163, 184),
            Accent = Color.FromArgb(59, 130, 246),
            AccentHover = Color.FromArgb(96, 165, 250),
            Border = Color.FromArgb(51, 65, 85),
            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        },
        [AppTheme.DarkPurple] = new ThemePalette
        {
            Background = Color.FromArgb(18, 12, 28),
            Surface = Color.FromArgb(31, 21, 45),
            SurfaceSecondary = Color.FromArgb(46, 31, 66),
            Card = Color.FromArgb(31, 21, 45),
            TextPrimary = Color.FromArgb(245, 232, 255),
            TextSecondary = Color.FromArgb(196, 181, 253),
            Accent = Color.FromArgb(168, 85, 247),
            AccentHover = Color.FromArgb(192, 132, 252),
            Border = Color.FromArgb(91, 71, 120),
            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Error = Color.FromArgb(248, 113, 113)
        },
        [AppTheme.DarkGreen] = new ThemePalette
        {
            Background = Color.FromArgb(10, 19, 17),
            Surface = Color.FromArgb(13, 32, 23),
            SurfaceSecondary = Color.FromArgb(22, 46, 35),
            Card = Color.FromArgb(13, 32, 23),
            TextPrimary = Color.FromArgb(220, 252, 231),
            TextSecondary = Color.FromArgb(134, 239, 172),
            Accent = Color.FromArgb(34, 197, 94),
            AccentHover = Color.FromArgb(74, 222, 128),
            Border = Color.FromArgb(34, 84, 57),
            Success = Color.FromArgb(74, 222, 128),
            Warning = Color.FromArgb(234, 179, 8),
            Error = Color.FromArgb(248, 113, 113)
        },
        [AppTheme.DarkRed] = new ThemePalette
        {
            Background = Color.FromArgb(24, 10, 12),
            Surface = Color.FromArgb(39, 18, 22),
            SurfaceSecondary = Color.FromArgb(62, 25, 31),
            Card = Color.FromArgb(39, 18, 22),
            TextPrimary = Color.FromArgb(254, 226, 226),
            TextSecondary = Color.FromArgb(252, 165, 165),
            Accent = Color.FromArgb(220, 38, 38),
            AccentHover = Color.FromArgb(239, 68, 68),
            Border = Color.FromArgb(94, 31, 35),
            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        }
    };

    public static AppTheme ParseTheme(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AppTheme.System;
        }

        var normalized = value.Replace(" ", string.Empty);
        if (Enum.TryParse<AppTheme>(normalized, true, out var theme))
        {
            return theme;
        }

        return AppTheme.System;
    }

    public static string GetDisplayName(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.LightBlue => "Light Blue",
            AppTheme.LightPurple => "Light Purple",
            AppTheme.LightGreen => "Light Green",
            AppTheme.LightOrange => "Light Orange",
            AppTheme.DarkBlue => "Dark Blue",
            AppTheme.DarkPurple => "Dark Purple",
            AppTheme.DarkGreen => "Dark Green",
            AppTheme.DarkRed => "Dark Red",
            AppTheme.System => "System",
            _ => "System"
        };
    }

    public static void ApplyTheme(Form form, AppTheme theme)
    {
        var palette = ResolvePalette(theme);
        ApplyThemeToControl(form, palette);
        form.BackColor = palette.Background;
        form.ForeColor = palette.TextPrimary;
    }

    public static ThemePalette ResolvePalette(AppTheme theme)
    {
        if (theme == AppTheme.System)
        {
            return IsWindowsDarkMode() ? PaletteMap[AppTheme.DarkBlue] : PaletteMap[AppTheme.LightBlue];
        }

        return PaletteMap.TryGetValue(theme, out var palette) ? palette : PaletteMap[AppTheme.LightBlue];
    }

    private static void ApplyThemeToControl(Control control, ThemePalette palette)
    {
        control.BackColor = control is Form ? palette.Background : palette.Surface;
        control.ForeColor = palette.TextPrimary;

        if (control is Label label)
        {
            label.ForeColor = palette.TextPrimary;
        }

        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = palette.Border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = palette.Accent;
            button.ForeColor = Color.White;
            button.UseVisualStyleBackColor = false;
        }
        else if (control is TextBox textBox)
        {
            textBox.BackColor = palette.SurfaceSecondary;
            textBox.ForeColor = palette.TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox comboBox)
        {
            comboBox.BackColor = palette.SurfaceSecondary;
            comboBox.ForeColor = palette.TextPrimary;
            comboBox.FlatStyle = FlatStyle.Flat;
        }
        else if (control is ListBox listBox)
        {
            listBox.BackColor = palette.SurfaceSecondary;
            listBox.ForeColor = palette.TextPrimary;
            listBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is Panel panel)
        {
            panel.BackColor = palette.Surface;
            panel.ForeColor = palette.TextPrimary;
        }
        else if (control is FlowLayoutPanel flowLayoutPanel)
        {
            flowLayoutPanel.BackColor = palette.Background;
            flowLayoutPanel.ForeColor = palette.TextPrimary;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child, palette);
        }
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            const string registryPath = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            var value = Microsoft.Win32.Registry.GetValue(registryPath, "AppsUseLightTheme", null);
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }
}
