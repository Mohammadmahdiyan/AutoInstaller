using GtaSaModManager.Models;
using Microsoft.Win32;
using System.Drawing.Drawing2D;

namespace GtaSaModManager.UI;

public sealed class ThemePalette
{
    public Color Background { get; init; }
    public Color BackgroundGradientEnd { get; init; }

    public Color Surface { get; init; }
    public Color SurfaceSecondary { get; init; }
    public Color Card { get; init; }

    public Color Sidebar { get; init; }
    public Color SidebarGradientEnd { get; init; }

    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }

    public Color Accent { get; init; }
    public Color AccentHover { get; init; }
    public Color AccentSoft { get; init; }

    public Color Border { get; init; }
    public Color BorderSoft { get; init; }

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
            Background = Color.FromArgb(239, 244, 252),
            BackgroundGradientEnd = Color.FromArgb(250, 252, 255),

            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(247, 249, 253),
            Card = Color.FromArgb(255, 255, 255),

            Sidebar = Color.FromArgb(238, 243, 252),
            SidebarGradientEnd = Color.FromArgb(247, 249, 255),

            TextPrimary = Color.FromArgb(20, 28, 45),
            TextSecondary = Color.FromArgb(91, 103, 124),

            Accent = Color.FromArgb(59, 91, 219),
            AccentHover = Color.FromArgb(91, 116, 235),
            AccentSoft = Color.FromArgb(224, 231, 255),

            Border = Color.FromArgb(214, 221, 235),
            BorderSoft = Color.FromArgb(229, 233, 242),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(217, 119, 6),
            Error = Color.FromArgb(220, 38, 38)
        },

        [AppTheme.LightPurple] = new ThemePalette
        {
            Background = Color.FromArgb(243, 241, 251),
            BackgroundGradientEnd = Color.FromArgb(251, 250, 255),

            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(249, 247, 253),
            Card = Color.FromArgb(255, 255, 255),

            Sidebar = Color.FromArgb(242, 239, 250),
            SidebarGradientEnd = Color.FromArgb(249, 247, 255),

            TextPrimary = Color.FromArgb(31, 26, 52),
            TextSecondary = Color.FromArgb(101, 94, 124),

            Accent = Color.FromArgb(109, 67, 213),
            AccentHover = Color.FromArgb(139, 92, 232),
            AccentSoft = Color.FromArgb(237, 233, 254),

            Border = Color.FromArgb(221, 215, 235),
            BorderSoft = Color.FromArgb(234, 230, 244),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(217, 119, 6),
            Error = Color.FromArgb(220, 38, 38)
        },

        [AppTheme.LightGreen] = new ThemePalette
        {
            Background = Color.FromArgb(239, 247, 243),
            BackgroundGradientEnd = Color.FromArgb(250, 253, 251),

            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(247, 251, 249),
            Card = Color.FromArgb(255, 255, 255),

            Sidebar = Color.FromArgb(237, 246, 241),
            SidebarGradientEnd = Color.FromArgb(248, 252, 250),

            TextPrimary = Color.FromArgb(20, 45, 32),
            TextSecondary = Color.FromArgb(77, 105, 90),

            Accent = Color.FromArgb(22, 142, 76),
            AccentHover = Color.FromArgb(43, 174, 98),
            AccentSoft = Color.FromArgb(220, 252, 231),

            Border = Color.FromArgb(205, 225, 214),
            BorderSoft = Color.FromArgb(226, 239, 231),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(202, 138, 4),
            Error = Color.FromArgb(220, 38, 38)
        },

        [AppTheme.LightOrange] = new ThemePalette
        {
            Background = Color.FromArgb(251, 245, 238),
            BackgroundGradientEnd = Color.FromArgb(255, 252, 248),

            Surface = Color.FromArgb(255, 255, 255),
            SurfaceSecondary = Color.FromArgb(252, 249, 245),
            Card = Color.FromArgb(255, 255, 255),

            Sidebar = Color.FromArgb(250, 243, 235),
            SidebarGradientEnd = Color.FromArgb(255, 250, 245),

            TextPrimary = Color.FromArgb(55, 35, 20),
            TextSecondary = Color.FromArgb(111, 88, 69),

            Accent = Color.FromArgb(214, 89, 20),
            AccentHover = Color.FromArgb(239, 113, 39),
            AccentSoft = Color.FromArgb(255, 237, 213),

            Border = Color.FromArgb(235, 215, 195),
            BorderSoft = Color.FromArgb(244, 230, 216),

            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(217, 119, 6),
            Error = Color.FromArgb(220, 38, 38)
        },

        [AppTheme.DarkBlue] = new ThemePalette
        {
            Background = Color.FromArgb(8, 13, 25),
            BackgroundGradientEnd = Color.FromArgb(16, 23, 42),

            Surface = Color.FromArgb(17, 24, 39),
            SurfaceSecondary = Color.FromArgb(23, 32, 52),
            Card = Color.FromArgb(20, 28, 46),

            Sidebar = Color.FromArgb(10, 16, 31),
            SidebarGradientEnd = Color.FromArgb(20, 27, 48),

            TextPrimary = Color.FromArgb(239, 244, 255),
            TextSecondary = Color.FromArgb(148, 163, 184),

            Accent = Color.FromArgb(70, 95, 220),
            AccentHover = Color.FromArgb(100, 122, 239),
            AccentSoft = Color.FromArgb(36, 48, 94),

            Border = Color.FromArgb(47, 61, 87),
            BorderSoft = Color.FromArgb(35, 46, 68),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkPurple] = new ThemePalette
        {
            Background = Color.FromArgb(15, 11, 24),
            BackgroundGradientEnd = Color.FromArgb(27, 19, 43),

            Surface = Color.FromArgb(24, 18, 38),
            SurfaceSecondary = Color.FromArgb(34, 25, 52),
            Card = Color.FromArgb(29, 22, 46),

            Sidebar = Color.FromArgb(18, 12, 29),
            SidebarGradientEnd = Color.FromArgb(35, 22, 54),

            TextPrimary = Color.FromArgb(247, 243, 255),
            TextSecondary = Color.FromArgb(181, 170, 204),

            Accent = Color.FromArgb(121, 72, 215),
            AccentHover = Color.FromArgb(154, 103, 237),
            AccentSoft = Color.FromArgb(62, 40, 96),

            Border = Color.FromArgb(73, 53, 98),
            BorderSoft = Color.FromArgb(51, 39, 69),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkGreen] = new ThemePalette
        {
            Background = Color.FromArgb(8, 18, 14),
            BackgroundGradientEnd = Color.FromArgb(14, 31, 23),

            Surface = Color.FromArgb(13, 27, 21),
            SurfaceSecondary = Color.FromArgb(21, 39, 31),
            Card = Color.FromArgb(16, 32, 25),

            Sidebar = Color.FromArgb(9, 20, 15),
            SidebarGradientEnd = Color.FromArgb(17, 38, 28),

            TextPrimary = Color.FromArgb(235, 252, 242),
            TextSecondary = Color.FromArgb(143, 181, 158),

            Accent = Color.FromArgb(22, 142, 76),
            AccentHover = Color.FromArgb(43, 181, 101),
            AccentSoft = Color.FromArgb(26, 70, 48),

            Border = Color.FromArgb(37, 76, 56),
            BorderSoft = Color.FromArgb(28, 54, 42),

            Success = Color.FromArgb(74, 222, 128),
            Warning = Color.FromArgb(234, 179, 8),
            Error = Color.FromArgb(248, 113, 113)
        },

        [AppTheme.DarkRed] = new ThemePalette
        {
            Background = Color.FromArgb(21, 10, 13),
            BackgroundGradientEnd = Color.FromArgb(38, 17, 22),

            Surface = Color.FromArgb(31, 16, 20),
            SurfaceSecondary = Color.FromArgb(44, 23, 28),
            Card = Color.FromArgb(37, 19, 24),

            Sidebar = Color.FromArgb(23, 10, 14),
            SidebarGradientEnd = Color.FromArgb(43, 18, 24),

            TextPrimary = Color.FromArgb(255, 240, 240),
            TextSecondary = Color.FromArgb(194, 145, 151),

            Accent = Color.FromArgb(190, 43, 48),
            AccentHover = Color.FromArgb(222, 67, 70),
            AccentSoft = Color.FromArgb(91, 36, 41),

            Border = Color.FromArgb(94, 42, 48),
            BorderSoft = Color.FromArgb(61, 29, 34),

            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(248, 113, 113)
        }
    };

    private sealed class ButtonState
    {
        public bool Hovered { get; set; }
        public bool Pressed { get; set; }
        public ThemePalette? Palette { get; set; }
    }

    private static readonly Dictionary<Button, ButtonState> ButtonStates = new();

    public static AppTheme ParseTheme(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AppTheme.System;

        if (Enum.TryParse<AppTheme>(
                value.Replace(" ", string.Empty),
                true,
                out var theme))
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

    public static ThemePalette ResolvePalette(AppTheme theme)
    {
        if (theme == AppTheme.System)
        {
            return IsWindowsDarkMode()
                ? PaletteMap[AppTheme.DarkBlue]
                : PaletteMap[AppTheme.LightBlue];
        }

        return PaletteMap.TryGetValue(
            theme,
            out var palette)
            ? palette
            : PaletteMap[AppTheme.LightBlue];
    }

    public static void ApplyTheme(
        Form form,
        AppTheme theme)
    {
        var palette = ResolvePalette(theme);

        ApplyThemeToControl(
            form,
            palette,
            isRoot: true);

        form.Invalidate(true);
    }

    private static void ApplyThemeToControl(
        Control control,
        ThemePalette palette,
        bool isRoot = false)
    {
        // ------------------------------------------------------------
        // FORM / ROOT BACKGROUND
        // ------------------------------------------------------------

        if (isRoot || control is Form)
        {
            control.BackColor = palette.Background;
            control.ForeColor = palette.TextPrimary;

            EnsureGradientBackground(control, palette);
        }

        // ------------------------------------------------------------
        // BUTTON
        // ------------------------------------------------------------

        if (control is Button button)
        {
            ApplyModernButton(
                button,
                palette);
        }

        // ------------------------------------------------------------
        // LABEL
        // ------------------------------------------------------------

        else if (control is Label label)
        {
            label.BackColor = Color.Transparent;

            if (label.Tag?.ToString() == "SecondaryText")
                label.ForeColor = palette.TextSecondary;
            else
                label.ForeColor = palette.TextPrimary;
        }

        // ------------------------------------------------------------
        // TEXTBOX
        // ------------------------------------------------------------

        else if (control is TextBox textBox)
        {
            textBox.BackColor =
                palette.SurfaceSecondary;

            textBox.ForeColor =
                palette.TextPrimary;

            textBox.BorderStyle =
                BorderStyle.FixedSingle;
        }

        // ------------------------------------------------------------
        // COMBOBOX
        // ------------------------------------------------------------

        else if (control is ComboBox comboBox)
        {
            comboBox.BackColor =
                palette.SurfaceSecondary;

            comboBox.ForeColor =
                palette.TextPrimary;

            comboBox.FlatStyle =
                FlatStyle.Flat;
        }

        // ------------------------------------------------------------
        // LISTBOX
        // ------------------------------------------------------------

        else if (control is ListBox listBox)
        {
            listBox.BackColor =
                palette.SurfaceSecondary;

            listBox.ForeColor =
                palette.TextPrimary;

            listBox.BorderStyle =
                BorderStyle.FixedSingle;
        }

        // ------------------------------------------------------------
        // PANELS
        // ------------------------------------------------------------

        else if (control is Panel panel)
        {
            ApplyPanelAppearance(
                panel,
                palette);
        }

        // ------------------------------------------------------------
        // FLOW PANELS
        // ------------------------------------------------------------

        else if (control is FlowLayoutPanel flow)
        {
            ApplyPanelAppearance(
                flow,
                palette);
        }

        // ------------------------------------------------------------
        // TABLE PANELS
        // ------------------------------------------------------------

        else if (control is TableLayoutPanel table)
        {
            ApplyPanelAppearance(
                table,
                palette);
        }

        // ------------------------------------------------------------
        // GROUPBOX
        // ------------------------------------------------------------

        else if (control is GroupBox groupBox)
        {
            groupBox.BackColor =
                palette.Surface;

            groupBox.ForeColor =
                palette.TextPrimary;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(
                child,
                palette);
        }
    }

    private static void EnsureGradientBackground(
        Control control,
        ThemePalette palette)
    {
        if (control.Tag?.ToString() == "NoGradient")
            return;

        control.Paint -= RootGradientPaint;
        control.Paint += RootGradientPaint;

        control.Tag =
            control.Tag == null
                ? "ThemeGradientRoot"
                : control.Tag;
    }

    private static void RootGradientPaint(
        object? sender,
        PaintEventArgs e)
    {
        if (sender is not Control control)
            return;

        var palette =
            FindPaletteForControl(control);

        if (palette == null)
            return;

        var rect =
            control.ClientRectangle;

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        using var brush =
            new LinearGradientBrush(
                rect,
                palette.Background,
                palette.BackgroundGradientEnd,
                LinearGradientMode.ForwardDiagonal);

        e.Graphics.FillRectangle(
            brush,
            rect);

        // Very soft atmospheric light.
        using var glowBrush =
            new LinearGradientBrush(
                rect,
                Color.FromArgb(
                    IsDarkPalette(palette) ? 18 : 12,
                    palette.Accent),
                Color.FromArgb(
                    0,
                    palette.Accent),
                LinearGradientMode.Vertical);

        e.Graphics.FillRectangle(
            glowBrush,
            new Rectangle(
                0,
                0,
                rect.Width,
                Math.Max(
                    1,
                    rect.Height / 2)));
    }

    private static ThemePalette? FindPaletteForControl(
        Control control)
    {
        foreach (var pair in PaletteMap)
        {
            if (pair.Value.Background == control.BackColor)
                return pair.Value;
        }

        // Fallback for System / dynamically rendered controls.
        return PaletteMap[AppTheme.LightBlue];
    }

    private static void ApplyPanelAppearance(
        Control panel,
        ThemePalette palette)
    {
        string? tag =
            panel.Tag?.ToString();

        if (tag == "Sidebar")
        {
            panel.BackColor =
                palette.Sidebar;

            panel.Paint -= SidebarPaint;
            panel.Paint += SidebarPaint;

            panel.Tag =
                new ThemeTag(
                    "Sidebar",
                    palette);

            return;
        }

        if (tag == "Card")
        {
            panel.BackColor =
                palette.Card;

            panel.Paint -= CardPaint;
            panel.Paint += CardPaint;

            panel.Tag =
                new ThemeTag(
                    "Card",
                    palette);

            return;
        }

        panel.BackColor =
            palette.Surface;

        panel.ForeColor =
            palette.TextPrimary;
    }

    private sealed record ThemeTag(
        string Type,
        ThemePalette Palette);

    private static void SidebarPaint(
        object? sender,
        PaintEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.Tag is not ThemeTag tag)
            return;

        var palette = tag.Palette;

        var rect =
            control.ClientRectangle;

        if (rect.Width <= 0 ||
            rect.Height <= 0)
            return;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        // Main Sidebar Gradient.
        using var gradient =
            new LinearGradientBrush(
                rect,
                palette.Sidebar,
                palette.SidebarGradientEnd,
                LinearGradientMode.Vertical);

        e.Graphics.FillRectangle(
            gradient,
            rect);

        // Accent atmospheric glow.
        var glowRect =
            new Rectangle(
                0,
                0,
                rect.Width,
                Math.Max(
                    1,
                    rect.Height / 2));

        using var glow =
            new LinearGradientBrush(
                glowRect,
                Color.FromArgb(
                    IsDarkPalette(palette)
                        ? 24
                        : 16,
                    palette.Accent),
                Color.FromArgb(
                    0,
                    palette.Accent),
                LinearGradientMode.Vertical);

        e.Graphics.FillRectangle(
            glow,
            glowRect);

        // Right edge separation.
        using var borderPen =
            new Pen(
                palette.BorderSoft,
                1f);

        e.Graphics.DrawLine(
            borderPen,
            rect.Right - 1,
            0,
            rect.Right - 1,
            rect.Bottom);
    }

    private static void CardPaint(
        object? sender,
        PaintEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.Tag is not ThemeTag tag)
            return;

        var palette = tag.Palette;

        var rect =
            control.ClientRectangle;

        if (rect.Width < 5 ||
            rect.Height < 5)
            return;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        var cardRect =
            new Rectangle(
                1,
                1,
                rect.Width - 2,
                rect.Height - 2);

        int radius =
            Math.Min(
                14,
                Math.Max(
                    8,
                    rect.Height / 12));

        using var path =
            CreateRoundedRectangle(
                cardRect,
                radius);

        // Soft shadow.
        if (IsDarkPalette(palette))
        {
            using var shadow =
                new SolidBrush(
                    Color.FromArgb(
                        60,
                        0,
                        0,
                        0));

            var shadowRect =
                new Rectangle(
                    cardRect.X,
                    cardRect.Y + 2,
                    cardRect.Width,
                    cardRect.Height);

            using var shadowPath =
                CreateRoundedRectangle(
                    shadowRect,
                    radius);

            e.Graphics.FillPath(
                shadow,
                shadowPath);
        }

        // Card surface gradient.
        using var cardBrush =
            new LinearGradientBrush(
                cardRect,
                palette.Card,
                Blend(
                    palette.Card,
                    palette.SurfaceSecondary,
                    IsDarkPalette(palette)
                        ? 0.18f
                        : 0.35f),
                LinearGradientMode.Vertical);

        e.Graphics.FillPath(
            cardBrush,
            path);

        // Border.
        using var borderPen =
            new Pen(
                palette.BorderSoft,
                1f);

        e.Graphics.DrawPath(
            borderPen,
            path);
    }

    private static void ApplyModernButton(
        Button button,
        ThemePalette palette)
    {
        if (!ButtonStates.TryGetValue(
                button,
                out var state))
        {
            state =
                new ButtonState();

            ButtonStates[button] =
                state;

            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
            button.MouseDown += Button_MouseDown;
            button.MouseUp += Button_MouseUp;
            button.EnabledChanged += Button_EnabledChanged;
            button.Disposed += Button_Disposed;
            button.Paint += Button_Paint;
        }

        state.Palette =
            palette;

        button.FlatStyle =
            FlatStyle.Flat;

        button.FlatAppearance.BorderSize =
            0;

        button.FlatAppearance.MouseOverBackColor =
            Color.Transparent;

        button.FlatAppearance.MouseDownBackColor =
            Color.Transparent;

        button.UseVisualStyleBackColor =
            false;

        button.BackColor =
            Color.Transparent;

        button.ForeColor =
            Color.White;

        button.Cursor =
            button.Enabled
                ? Cursors.Hand
                : Cursors.Default;

        button.Padding =
            new Padding(
                16,
                7,
                16,
                7);

        if (button.Height < 40)
            button.Height = 40;

        if (button.Width < 100)
            button.Width = 100;

        button.Invalidate();
    }

    private static void Button_MouseEnter(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (ButtonStates.TryGetValue(
                button,
                out var state))
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

        if (ButtonStates.TryGetValue(
                button,
                out var state))
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

        if (e.Button != MouseButtons.Left ||
            !button.Enabled)
            return;

        if (ButtonStates.TryGetValue(
                button,
                out var state))
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

        if (ButtonStates.TryGetValue(
                button,
                out var state))
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

        button.Cursor =
            button.Enabled
                ? Cursors.Hand
                : Cursors.Default;

        button.Invalidate();
    }

    private static void Button_Disposed(
        object? sender,
        EventArgs e)
    {
        if (sender is Button button)
            ButtonStates.Remove(button);
    }

    private static void Button_Paint(
        object? sender,
        PaintEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (!ButtonStates.TryGetValue(
                button,
                out var state))
            return;

        var palette =
            state.Palette;

        if (palette == null)
            return;

        var bounds =
            button.ClientRectangle;

        if (bounds.Width < 4 ||
            bounds.Height < 4)
            return;

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.HighQuality;

        int radius =
            Math.Min(
                11,
                Math.Max(
                    7,
                    bounds.Height / 4));

        var rect =
            new Rectangle(
                1,
                1,
                bounds.Width - 2,
                bounds.Height - 2);

        using var path =
            CreateRoundedRectangle(
                rect,
                radius);

        Color start;
        Color end;
        Color border;
        Color text;

        if (!button.Enabled)
        {
            start =
                Blend(
                    palette.SurfaceSecondary,
                    palette.Background,
                    0.25f);

            end =
                Blend(
                    palette.SurfaceSecondary,
                    palette.Background,
                    0.60f);

            border =
                palette.BorderSoft;

            text =
                palette.TextSecondary;
        }
        else if (state.Pressed)
        {
            start =
                Darken(
                    palette.AccentHover,
                    0.20f);

            end =
                Darken(
                    palette.Accent,
                    0.18f);

            border =
                Darken(
                    palette.Accent,
                    0.24f);

            text =
                Color.White;
        }
        else if (state.Hovered)
        {
            start =
                Lighten(
                    palette.AccentHover,
                    0.08f);

            end =
                palette.AccentHover;

            border =
                Lighten(
                    palette.AccentHover,
                    0.10f);

            text =
                Color.White;
        }
        else
        {
            start =
                palette.Accent;

            end =
                palette.AccentHover;

            border =
                Blend(
                    palette.Accent,
                    palette.Border,
                    0.25f);

            text =
                Color.White;
        }

        // ------------------------------------------------------------
        // BUTTON SHADOW
        // ------------------------------------------------------------

        if (button.Enabled)
        {
            var shadowRect =
                new Rectangle(
                    rect.X,
                    rect.Y + 2,
                    rect.Width,
                    rect.Height);

            using var shadowPath =
                CreateRoundedRectangle(
                    shadowRect,
                    radius);

            using var shadowBrush =
                new SolidBrush(
                    Color.FromArgb(
                        IsDarkPalette(palette)
                            ? 75
                            : 35,
                        0,
                        0,
                        0));

            e.Graphics.FillPath(
                shadowBrush,
                shadowPath);
        }

        // ------------------------------------------------------------
        // REAL LINEAR GRADIENT
        // ------------------------------------------------------------

        using (var gradient =
               new LinearGradientBrush(
                   rect,
                   start,
                   end,
                   LinearGradientMode.Horizontal))
        {
            e.Graphics.FillPath(
                gradient,
                path);
        }

        // ------------------------------------------------------------
        // TOP LIGHT
        // ------------------------------------------------------------

        if (button.Enabled &&
            !state.Pressed)
        {
            var highlightRect =
                new Rectangle(
                    rect.X + 1,
                    rect.Y + 1,
                    rect.Width - 2,
                    Math.Max(
                        1,
                        rect.Height / 2));

            using var highlightPath =
                CreateRoundedRectangle(
                    highlightRect,
                    Math.Max(
                        4,
                        radius - 2));

            using var highlight =
                new LinearGradientBrush(
                    highlightRect,
                    Color.FromArgb(
                        state.Hovered ? 30 : 18,
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
                highlight,
                highlightPath);
        }

        // ------------------------------------------------------------
        // BORDER
        // ------------------------------------------------------------

        using var borderPen =
            new Pen(
                border,
                1f);

        e.Graphics.DrawPath(
            borderPen,
            path);

        // ------------------------------------------------------------
        // TEXT
        // ------------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                button.Text))
        {
            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                rect,
                text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(
        Rectangle rectangle,
        int radius)
    {
        var path =
            new GraphicsPath();

        radius =
            Math.Max(
                1,
                radius);

        int diameter =
            Math.Min(
                radius * 2,
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

    private static Color Lighten(
        Color color,
        float amount)
    {
        amount =
            Math.Clamp(
                amount,
                0f,
                1f);

        return Color.FromArgb(
            color.A,
            color.R +
                (int)((255 - color.R) * amount),
            color.G +
                (int)((255 - color.G) * amount),
            color.B +
                (int)((255 - color.B) * amount));
    }

    private static Color Darken(
        Color color,
        float amount)
    {
        amount =
            Math.Clamp(
                amount,
                0f,
                1f);

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
        amount =
            Math.Clamp(
                amount,
                0f,
                1f);

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
            const string path =
                "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

            var value =
                Registry.GetValue(
                    path,
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