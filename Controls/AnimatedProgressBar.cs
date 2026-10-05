using System.Drawing.Drawing2D;
using GtaSaModManager.Models;
using GtaSaModManager.UI;

namespace GtaSaModManager.Controls;

public sealed class AnimatedProgressBar : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 40 };
    private ThemePalette _palette = ThemeManager.ResolvePalette(AppTheme.System);
    private float _displayValue;
    private float _targetValue;
    private float _shineOffset;
    private bool _isRunning;
    private bool _isComplete;
    private bool _hasFailed;

    public AnimatedProgressBar()
    {
        SetStyle(ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        MinimumSize = new Size(80, 30);
        Height = 36;
        _timer.Tick += Timer_Tick;
        VisibleChanged += (_, _) => UpdateTimerState();
        RightToLeftChanged += (_, _) => Invalidate();
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        Invalidate();
    }

    public void Reset()
    {
        _displayValue = 0;
        _targetValue = 0;
        _shineOffset = 0;
        _isRunning = true;
        _isComplete = false;
        _hasFailed = false;
        UpdateTimerState();
        Invalidate();
    }

    public void SetProgress(int value)
    {
        _targetValue = Math.Clamp(value, 0, 100);
        if (!_isComplete && !_hasFailed)
        {
            _isRunning = true;
            UpdateTimerState();
        }
        Invalidate();
    }

    public void Complete()
    {
        _targetValue = 100;
        _isComplete = true;
        _hasFailed = false;
        _isRunning = false;
        UpdateTimerState();
        Invalidate();
    }

    public void Fail()
    {
        _hasFailed = true;
        _isComplete = false;
        _isRunning = false;
        UpdateTimerState();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var bounds = new Rectangle(1, 3, Math.Max(1, ClientSize.Width - 2), Math.Max(1, ClientSize.Height - 6));
        var radius = Math.Min(bounds.Height / 2, 14);
        using var trackPath = CreateRoundedPath(bounds, radius);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var trackBrush = new SolidBrush(_palette.SurfaceSecondary))
        {
            e.Graphics.FillPath(trackBrush, trackPath);
        }

        var displayedValue = _hasFailed ? 100 : Math.Clamp(_displayValue, 0, 100);
        var fillWidth = (int)Math.Round(bounds.Width * displayedValue / 100f);
        var fillBounds = RightToLeft == RightToLeft.Yes
            ? new Rectangle(bounds.Right - fillWidth, bounds.Y, fillWidth, bounds.Height)
            : new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
        if (fillWidth > 0)
        {
            using var fillPath = CreateRoundedPath(fillBounds, Math.Min(radius, Math.Max(1, fillWidth / 2)));
            if (_hasFailed)
            {
                using var failureBrush = new SolidBrush(_palette.Error);
                e.Graphics.FillPath(failureBrush, fillPath);
            }
            else if (_isComplete)
            {
                using var completeBrush = new SolidBrush(_palette.Success);
                e.Graphics.FillPath(completeBrush, fillPath);
            }
            else
            {
                using var fillBrush = new LinearGradientBrush(
                    fillBounds,
                    ControlPaint.Light(_palette.Success, 0.26f),
                    _palette.Success,
                    LinearGradientMode.Vertical);
                e.Graphics.FillPath(fillBrush, fillPath);

                if (_isRunning)
                {
                    var graphicsState = e.Graphics.Save();
                    e.Graphics.SetClip(fillPath);
                    using var shinePen = new Pen(Color.FromArgb(48, Color.White), Math.Max(2, bounds.Height / 3f));
                    for (var offset = -bounds.Height; offset < bounds.Width + bounds.Height; offset += 18)
                    {
                        var x = bounds.X + (int)((offset + _shineOffset) % (bounds.Width + bounds.Height));
                        e.Graphics.DrawLine(shinePen, x, bounds.Bottom, x + bounds.Height, bounds.Top);
                    }
                    e.Graphics.Restore(graphicsState);
                }
            }
        }

        var percentage = Math.Clamp((int)Math.Round(_displayValue), 0, 100) + "%";
        var textBounds = ClientRectangle;
        var textFlags = TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(e.Graphics, percentage, Font, textBounds, _palette.TextSecondary, textFlags);
        if (fillWidth > 0)
        {
            using var textClip = CreateRoundedPath(fillBounds, Math.Min(radius, Math.Max(1, fillWidth / 2)));
            var graphicsState = e.Graphics.Save();
            e.Graphics.SetClip(textClip);
            TextRenderer.DrawText(e.Graphics, percentage, Font, textBounds, Color.White, textFlags);
            e.Graphics.Restore(graphicsState);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var difference = _targetValue - _displayValue;
        if (Math.Abs(difference) < 0.35f)
        {
            _displayValue = _targetValue;
            if (!_isRunning)
            {
                _timer.Stop();
            }
        }
        else
        {
            _displayValue += difference * 0.22f;
        }

        if (_isRunning)
        {
            _shineOffset = (_shineOffset + 7f) % Math.Max(1, Width + Height);
        }
        Invalidate();
    }

    private void UpdateTimerState()
    {
        if (_isRunning || Math.Abs(_targetValue - _displayValue) >= 0.35f)
        {
            if (Visible && !_timer.Enabled)
            {
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
        }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var diameter = Math.Clamp(radius * 2, 1, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}