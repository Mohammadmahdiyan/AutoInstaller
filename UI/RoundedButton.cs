using System.Drawing.Drawing2D;

namespace GtaSaModManager.UI;

public class RoundedButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocused;

    public RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(37, 99, 235);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        Padding = new Padding(8, 0, 8, 0);
        Margin = new Padding(10, 0, 0, 0);
        Height = 42;
        Width = 140;
        Font = new Font("Segoe UI", 9.25F, FontStyle.Bold);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(1, 1, Width - 2, Height - 2);
        var rounded = GetRoundedRectangle(bounds, 12);
        this.Region = new Region(rounded);

        var fillColor = Enabled ? (_isPressed ? Color.FromArgb(30, 64, 175)
            : _isHovered ? Color.FromArgb(59, 130, 246)
            : BackColor)
            : Color.FromArgb(203, 213, 225);
        var topColor = ControlPaint.Light(fillColor, 0.12f);
        var bottomColor = ControlPaint.Dark(fillColor, 0.08f);

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
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
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
        Invalidate();
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
}
