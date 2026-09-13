using GtaSaModManager.Models;
using System.Drawing.Drawing2D;

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
            Background = Color.FromArgb(241, 245, 249),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(248, 250, 252),
            Card = Color.FromArgb(255, 255, 255),

            TextPrimary = Color.FromArgb(15, 23, 42),
            TextSecondary = Color.FromArgb(71, 85, 105),

            Accent = Color.FromArgb(37, 99, 235),
            AccentHover = Color.FromArgb(59, 130, 246),

            Border = Color.FromArgb(203, 213, 225),

            Success = Color.FromArgb(16, 185, 129),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(239, 68, 68)
        },

        [AppTheme.LightPurple] = new ThemePalette
        {
            Background = Color.FromArgb(245, 243, 255),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(250, 248, 255),
            Card = Color.FromArgb(255, 255, 255),

            TextPrimary = Color.FromArgb(30, 27, 75),
            TextSecondary = Color.FromArgb(88, 88, 122),

            Accent = Color.FromArgb(124, 58, 237),
            AccentHover = Color.FromArgb(168, 85, 247),

            Border = Color.FromArgb(221, 214, 254),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.LightGreen] = new ThemePalette
        {
            Background = Color.FromArgb(240, 253, 244),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(248, 252, 249),
            Card = Color.FromArgb(255, 255, 255),

            TextPrimary = Color.FromArgb(20, 83, 45),
            TextSecondary = Color.FromArgb(52, 88, 61),

            Accent = Color.FromArgb(22, 163, 74),
            AccentHover = Color.FromArgb(34, 197, 94),

            Border = Color.FromArgb(187, 247, 208),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(234, 179, 8),
            Error = Color.FromArgb(239, 68, 68)
        },

        [AppTheme.LightOrange] = new ThemePalette
        {
            Background = Color.FromArgb(255, 247, 237),
            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(255, 251, 245),
            Card = Color.FromArgb(255, 255, 255),

            TextPrimary = Color.FromArgb(67, 39, 11),
            TextSecondary = Color.FromArgb(122, 90, 62),

            Accent = Color.FromArgb(234, 88, 12),
            AccentHover = Color.FromArgb(249, 115, 22),

            Border = Color.FromArgb(253, 186, 116),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(234, 88, 12),
            Error = Color.FromArgb(239, 68, 68)
        },

        [AppTheme.DarkBlue] = new ThemePalette
        {
            Background = Color.FromArgb(9, 15, 29),
            Surface = Color.FromArgb(15, 23, 42),
            SurfaceSecondary = Color.FromArgb(24, 33, 52),
            Card = Color.FromArgb(17, 25, 44),

            TextPrimary = Color.FromArgb(226, 232, 240),
            TextSecondary = Color.FromArgb(148, 163, 184),

            Accent = Color.FromArgb(37, 99, 235),
            AccentHover = Color.FromArgb(59, 130, 246),

            Border = Color.FromArgb(51, 65, 85),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkPurple] = new ThemePalette
        {
            Background = Color.FromArgb(18, 12, 28),
            Surface = Color.FromArgb(31, 21, 45),
            SurfaceSecondary = Color.FromArgb(42, 31, 59),
            Card = Color.FromArgb(34, 23, 48),

            TextPrimary = Color.FromArgb(245, 232, 255),
            TextSecondary = Color.FromArgb(196, 181, 253),

            Accent = Color.FromArgb(126, 34, 206),
            AccentHover = Color.FromArgb(168, 85, 247),

            Border = Color.FromArgb(91, 71, 120),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkGreen] = new ThemePalette
        {
            Background = Color.FromArgb(10, 19, 17),
            Surface = Color.FromArgb(13, 32, 23),
            SurfaceSecondary = Color.FromArgb(24, 44, 35),
            Card = Color.FromArgb(15, 35, 25),

            TextPrimary = Color.FromArgb(220, 252, 231),
            TextSecondary = Color.FromArgb(134, 239, 172),

            Accent = Color.FromArgb(22, 163, 74),
            AccentHover = Color.FromArgb(34, 197, 94),

            Border = Color.FromArgb(34, 84, 57),

            Success = Color.FromArgb(74, 222, 128),
            Warning = Color.FromArgb(234, 179, 8),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkRed] = new ThemePalette
        {
            Background = Color.FromArgb(24, 10, 12),
            Surface = Color.FromArgb(39, 18, 22),
            SurfaceSecondary = Color.FromArgb(53, 25, 30),
            Card = Color.FromArgb(43, 20, 25),

            TextPrimary = Color.FromArgb(254, 226, 226),
            TextSecondary = Color.FromArgb(252, 165, 165),

            Accent = Color.FromArgb(185, 28, 28),
            AccentHover = Color.FromArgb(220, 38, 38),

            Border = Color.FromArgb(94, 31, 35),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        }
    };

    private sealed class ButtonVisualState
    {
        public bool Hovered { get; set; }
        public bool Pressed { get; set; }
        public ThemePalette? Palette { get; set; }
    }

    private static readonly Dictionary<Button, ButtonVisualState> ButtonStates = new();

    public static AppTheme ParseTheme(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AppTheme.System;

        var normalized = value.Replace(" ", string.Empty);

        if (Enum.TryParse<AppTheme>(normalized, true, out var theme))
            return theme;

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

        form.Invalidate(true);
    }

    public static ThemePalette ResolvePalette(AppTheme theme)
    {
        if (theme == AppTheme.System)
        {
            return IsWindowsDarkMode()
                ? PaletteMap[AppTheme.DarkBlue]
                : PaletteMap[AppTheme.LightBlue];
        }

        return PaletteMap.TryGetValue(theme, out var palette)
            ? palette
            : PaletteMap[AppTheme.LightBlue];
    }

    private static void ApplyThemeToControl(
        Control control,
        ThemePalette palette)
    {
        control.BackColor =
            control is Form
                ? palette.Background
                : palette.Surface;

        control.ForeColor = palette.TextPrimary;

        if (control is Button button)
        {
            ApplyModernButton(button, palette);
        }
        else if (control is Label label)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = palette.TextPrimary;
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
        else if (control is FlowLayoutPanel flowLayoutPanel)
        {
            flowLayoutPanel.BackColor = palette.Background;
            flowLayoutPanel.ForeColor = palette.TextPrimary;
        }
        else if (control is Panel panel)
        {
            panel.BackColor = palette.Surface;
            panel.ForeColor = palette.TextPrimary;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child, palette);
        }
    }

    private static void ApplyModernButton(
        Button button,
        ThemePalette palette)
    {
        if (!ButtonStates.TryGetValue(button, out var state))
        {
            state = new ButtonVisualState();
            ButtonStates[button] = state;

            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
            button.MouseDown += Button_MouseDown;
            button.MouseUp += Button_MouseUp;
            button.EnabledChanged += Button_EnabledChanged;
            button.Disposed += Button_Disposed;
            button.Paint += Button_Paint;
        }

        state.Palette = palette;

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;

        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;

        button.UseVisualStyleBackColor = false;

        button.BackColor = Color.Transparent;
        button.ForeColor = Color.White;

        button.Cursor = button.Enabled
            ? Cursors.Hand
            : Cursors.Default;

        button.Padding = new Padding(16, 7, 16, 7);

        if (button.Font == null || button.Font.Size < 8)
        {
            button.Font = new Font(
                "Segoe UI",
                9.5f,
                FontStyle.Regular,
                GraphicsUnit.Point);
        }

        // Consistent modern button proportions.
        if (button.Height < 40)
        {
            button.Height = 40;
        }

        if (button.Width < 100)
        {
            button.Width = 100;
        }

        button.Invalidate();
    }

    private static void Button_MouseEnter(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (ButtonStates.TryGetValue(button, out var state))
        {
            state.Hovered = true;
            button.Invalidate();
        }
    }

    private static void Button_MouseLeave(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (ButtonStates.TryGetValue(button, out var state))
        {
            state.Hovered = false;
            state.Pressed = false;

            button.Invalidate();
        }
    }

    private static void Button_MouseDown(
        object? sender,
        MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (e.Button != MouseButtons.Left)
            return;

        if (!button.Enabled)
            return;

        if (ButtonStates.TryGetValue(button, out var state))
        {
            state.Pressed = true;
            button.Invalidate();
        }
    }

    private static void Button_MouseUp(
        object? sender,
        MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (ButtonStates.TryGetValue(button, out var state))
        {
            state.Pressed = false;
            button.Invalidate();
        }
    }

    private static void Button_EnabledChanged(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        button.Cursor = button.Enabled
            ? Cursors.Hand
            : Cursors.Default;

        button.Invalidate();
    }

    private static void Button_Disposed(
        object? sender,
        EventArgs e)
    {
        if (sender is Button button)
        {
            ButtonStates.Remove(button);
        }
    }

    private static void Button_Paint(
        object? sender,
        PaintEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (!ButtonStates.TryGetValue(button, out var state))
            return;

        var palette = state.Palette;

        if (palette == null)
            return;

        var bounds = button.ClientRectangle;

        if (bounds.Width < 4 || bounds.Height < 4)
            return;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.HighQuality;

        e.Graphics.CompositingQuality =
            CompositingQuality.HighQuality;

        int radius = Math.Min(
            11,
            Math.Max(7, bounds.Height / 4));

        var buttonRect = new Rectangle(
            1,
            1,
            bounds.Width - 2,
            bounds.Height - 2);

        using var buttonPath =
            CreateRoundedRectangle(
                buttonRect,
                radius);

        Color gradientStart;
        Color gradientEnd;
        Color borderColor;
        Color textColor;

        if (!button.Enabled)
        {
            gradientStart = GetDisabledGradientStart(palette);
            gradientEnd = GetDisabledGradientEnd(palette);
            borderColor = GetDisabledBorder(palette);
            textColor = GetDisabledText(palette);
        }
        else if (state.Pressed)
        {
            gradientStart = Darken(
                palette.AccentHover,
                0.18f);

            gradientEnd = Darken(
                palette.Accent,
                0.20f);

            borderColor = Darken(
                palette.Accent,
                0.24f);

            textColor = Color.White;
        }
        else if (state.Hovered)
        {
            gradientStart = Lighten(
                palette.AccentHover,
                0.08f);

            gradientEnd = palette.AccentHover;

            borderColor = Lighten(
                palette.AccentHover,
                0.10f);

            textColor = Color.White;
        }
        else
        {
            // Normal state:
            // REAL LINEAR GRADIENT.
            gradientStart = palette.Accent;
            gradientEnd = palette.AccentHover;

            borderColor = Blend(
                palette.Accent,
                palette.Border,
                0.30f);

            textColor = Color.White;
        }

        // Subtle depth/shadow.
        if (button.Enabled)
        {
            var shadowRect = new Rectangle(
                1,
                3,
                bounds.Width - 2,
                bounds.Height - 2);

            using var shadowPath =
                CreateRoundedRectangle(
                    shadowRect,
                    radius);

            using var shadowBrush =
                new SolidBrush(
                    Color.FromArgb(
                        IsDarkPalette(palette)
                            ? 80
                            : 32,
                        0,
                        0,
                        0));

            e.Graphics.FillPath(
                shadowBrush,
                shadowPath);
        }

        // ============================================================
        // IMPORTANT:
        // Actual Linear Gradient used for the button surface.
        // ============================================================

        using (var gradientBrush =
               new LinearGradientBrush(
                   buttonRect,
                   gradientStart,
                   gradientEnd,
                   LinearGradientMode.Horizontal))
        {
            e.Graphics.FillPath(
                gradientBrush,
                buttonPath);
        }

        // Very subtle top highlight.
        if (button.Enabled && !state.Pressed)
        {
            var highlightRect = new Rectangle(
                buttonRect.X + 1,
                buttonRect.Y + 1,
                Math.Max(1, buttonRect.Width - 2),
                Math.Max(1, buttonRect.Height / 3));

            using var highlightPath =
                CreateRoundedRectangle(
                    highlightRect,
                    Math.Max(4, radius - 2));

            using var highlightBrush =
                new LinearGradientBrush(
                    highlightRect,
                    Color.FromArgb(
                        state.Hovered ? 28 : 18,
                        255,
                        255,
                        255),
                    Color.FromArgb(
                        0,
                        255,
                        255,
                        255),
                    LinearGradientMode.Vertical);

            e.Graphics.FillPath(
                highlightBrush,
                highlightPath);
        }

        // Border.
        using (var borderPen =
               new Pen(borderColor, 1f))
        {
            e.Graphics.DrawPath(
                borderPen,
                buttonPath);
        }

        DrawButtonText(
            e.Graphics,
            button,
            buttonRect,
            textColor);
    }

    private static void DrawButtonText(
        Graphics graphics,
        Button button,
        Rectangle rect,
        Color textColor)
    {
        if (string.IsNullOrEmpty(button.Text))
            return;

        TextFormatFlags flags =
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix;

        TextRenderer.DrawText(
            graphics,
            button.Text,
            button.Font,
            rect,
            textColor,
            flags);
    }

    private static GraphicsPath CreateRoundedRectangle(
        Rectangle rectangle,
        int radius)
    {
        var path = new GraphicsPath();

        radius = Math.Max(1, radius);

        int diameter = radius * 2;

        diameter = Math.Min(
            diameter,
            Math.Min(
                rectangle.Width,
                rectangle.Height));

        path.AddArc(
            rectangle.X,
            rectangle.Y,
            diameter,
            diameter,
            180,
            90);

        path.AddArc(
            rectangle.Right - diameter,
            rectangle.Y,
            diameter,
            diameter,
            270,
            90);

        path.AddArc(
            rectangle.Right - diameter,
            rectangle.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);

        path.AddArc(
            rectangle.X,
            rectangle.Bottom - diameter,
            diameter,
            diameter,
            90,
            90);

        path.CloseFigure();

        return path;
    }

    private static Color GetDisabledGradientStart(
        ThemePalette palette)
    {
        return Blend(
            palette.SurfaceSecondary,
            palette.Background,
            IsDarkPalette(palette)
                ? 0.35f
                : 0.20f);
    }

    private static Color GetDisabledGradientEnd(
        ThemePalette palette)
    {
        return Blend(
            palette.SurfaceSecondary,
            palette.Background,
            IsDarkPalette(palette)
                ? 0.65f
                : 0.45f);
    }

    private static Color GetDisabledBorder(
        ThemePalette palette)
    {
        return Blend(
            palette.Border,
            palette.Background,
            0.45f);
    }

    private static Color GetDisabledText(
        ThemePalette palette)
    {
        return Blend(
            palette.TextSecondary,
            palette.Background,
            0.25f);
    }

    private static Color Lighten(
        Color color,
        float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);

        return Color.FromArgb(
            color.A,
            color.R + (int)((255 - color.R) * amount),
            color.G + (int)((255 - color.G) * amount),
            color.B + (int)((255 - color.B) * amount));
    }

    private static Color Darken(
        Color color,
        float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);

        return Color.FromArgb(
            color.A,
            (int)(color.R * (1f - amount)),
            (int)(color.G * (1f - amount)),
            (int)(color.B * (1f - amount)));
    }

    private static Color Blend(
        Color first,
        Color second,
        float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);

        return Color.FromArgb(
            first.A,

            (int)(
                first.R +
                (second.R - first.R) * amount),

            (int)(
                first.G +
                (second.G - first.G) * amount),

            (int)(
                first.B +
                (second.B - first.B) * amount));
    }

    private static bool IsDarkPalette(
        ThemePalette palette)
    {
        int brightness =
            (
                palette.Background.R * 299 +
                palette.Background.G * 587 +
                palette.Background.B * 114
            ) / 1000;

        return brightness < 128;
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            const string registryPath =
                "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

            var value =
                Microsoft.Win32.Registry.GetValue(
                    registryPath,
                    "AppsUseLightTheme",
                    null);

            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }
}