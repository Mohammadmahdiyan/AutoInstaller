using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GtaSaModManager.UI;

public class RoundedButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocused;
    private float _hoverProgress;
    private readonly System.Windows.Forms.Timer _hoverTimer;

    public Color AccentColor { get; set; } = Color.FromArgb(37, 99, 235);

    public RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        Padding = new Padding(8, 0, 8, 0);
        Margin = new Padding(10, 0, 0, 0);
        Height = 42;
        Width = 140;
        Font = new Font("Segoe UI", 9.25F, FontStyle.Bold);

        _hoverTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _hoverTimer.Tick += (_, _) =>
        {
            const float step = 0.09f;
            if (_isHovered)
            {
                _hoverProgress = Math.Min(1f, _hoverProgress + step);
            }
            else
            {
                _hoverProgress = Math.Max(0f, _hoverProgress - step);
            }

            if ((_isHovered && _hoverProgress >= 1f) || (!_isHovered && _hoverProgress <= 0f))
            {
                _hoverTimer.Stop();
            }

            Invalidate();
        };

        Resize += (_, _) => UpdateRoundedRegion();
        SizeChanged += (_, _) => UpdateRoundedRegion();
        Layout += (_, _) => UpdateRoundedRegion();
        ParentChanged += (_, _) => UpdateRoundedRegion();
        HandleCreated += (_, _) => UpdateRoundedRegion();
        VisibleChanged += (_, _) => UpdateRoundedRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        UpdateRoundedRegion();

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(1, 1, Width - 2, Height - 2);
        var rounded = GetRoundedRectangle(bounds, 12);

        var normalColor = AccentColor;
        var hoverColor = Lighten(normalColor, 0.12f);
        var pressedColor = Darken(normalColor, 0.18f);
        var fillColor = Enabled ? (_isPressed ? pressedColor : ColorLerp(normalColor, hoverColor, _hoverProgress)) : Color.FromArgb(203, 213, 225);
        var topColor = Lighten(fillColor, 0.12f);
        var bottomColor = Darken(fillColor, 0.08f);

        using var gradientBrush = new LinearGradientBrush(bounds, topColor, bottomColor, LinearGradientMode.Vertical);
        g.FillPath(gradientBrush, rounded);

        using var borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.25f);
        g.DrawPath(borderPen, rounded);

        if (Enabled && _isFocused)
        {
            using var focusPen = new Pen(Color.FromArgb(140, 255, 255, 255), 1.2f);
            using var focusPath = GetRoundedRectangle(Rectangle.Inflate(bounds, -3, -3), 10);
            g.DrawPath(focusPen, focusPath);
        }

        var textColor = Enabled ? ForeColor : Color.FromArgb(71, 85, 105);
        using var textBrush = new SolidBrush(textColor);
        var textSize = g.MeasureString(Text, Font);
        var textX = (Width - textSize.Width) / 2f;
        var textY = (Height - textSize.Height) / 2f;
        g.DrawString(Text, Font, textBrush, textX, textY);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _isFocused = true;
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _isFocused = false;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        if (!_hoverTimer.Enabled)
        {
            _hoverTimer.Start();
        }
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        if (!_hoverTimer.Enabled)
        {
            _hoverTimer.Start();
        }
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            _hoverProgress = 0f;
            _isHovered = false;
            _isPressed = false;
        }
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRoundedRegion();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        UpdateRoundedRegion();
    }

    private void UpdateRoundedRegion()
    {
        if (Width <= 0 || Height <= 0 || IsDisposed)
        {
            return;
        }

        var bounds = new Rectangle(0, 0, Width, Height);
        var rounded = GetRoundedRectangle(bounds, 12);
        if (Region != null)
        {
            Region.Dispose();
        }

        Region = new Region(rounded);
        Region.MakeEmpty();
        Region.Union(rounded);
    }

    private static GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
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

    private static Color ColorLerp(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        var r = (int)Math.Round(from.R + (to.R - from.R) * amount);
        var g = (int)Math.Round(from.G + (to.G - from.G) * amount);
        var b = (int)Math.Round(from.B + (to.B - from.B) * amount);
        var a = (int)Math.Round(from.A + (to.A - from.A) * amount);
        return Color.FromArgb(a, r, g, b);
    }

    private static Color Lighten(Color color, float amount)
    {
        return ColorLerp(color, Color.White, amount);
    }

    private static Color Darken(Color color, float amount)
    {
        return ColorLerp(color, Color.Black, amount);
    }
}
