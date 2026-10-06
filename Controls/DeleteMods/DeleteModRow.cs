using System.Drawing.Drawing2D;

namespace GtaSaModManager.Controls.DeleteMods;

/// <summary>
/// One custom drawn row of the delete window:
/// text + trash icon -> "Are you sure? Yes/No" -> progress ring with file counter -> green check -> collapse.
/// </summary>
internal sealed class DeleteModRow : Control
{
    private enum RowState { Idle, Confirm, Deleting, Done, Removing }

    internal static readonly Color ItemColor = Color.FromArgb(0x12, 0x15, 0x1A);
    internal static readonly Color HoverColor = Color.FromArgb(0x18, 0x1C, 0x22);
    internal static readonly Color BorderColor = Color.FromArgb(0x27, 0x2C, 0x34);
    internal static readonly Color TextColor = Color.FromArgb(0xDF, 0xE3, 0xE8);
    internal static readonly Color MutedColor = Color.FromArgb(0x85, 0x8D, 0x99);
    internal static readonly Color GreenColor = Color.FromArgb(0x35, 0xD0, 0x7F);
    internal static readonly Color AccentColor = Color.FromArgb(0x38, 0xBD, 0xF8);

    private const int FullHeight = 58;
    private const int SidePadding = 20;
    private const int IconSize = 32;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly DeleteModEntry _entry;
    private readonly string _title;
    private RowState _state = RowState.Idle;
    private bool _hovered;
    private bool _trashHovered;
    private int _hoverConfirm; // 0 none, 1 yes, 2 no
    private float _lid;        // 0..1 lid animation
    private float _confirmAnim; // 0..1
    private float _progress;   // 0..1
    private int _done;
    private int _total;
    private float _checkAnim;
    private int _doneHoldTicks;
    private float _collapse = 1f;

    public event EventHandler? ConfirmOpened;
    public event EventHandler<DeleteModEntry>? DeleteRequested;
    public event EventHandler? RightClickedRow;
    public event EventHandler? RemovedFromList;

    public DeleteModEntry Entry => _entry;

    public bool IsRightToLeft { get; set; }

    public DeleteModRow(DeleteModEntry entry, int number)
    {
        _entry = entry;
        _title = number + ". " + entry.Name;
        _total = entry.Files.Count;
        Height = FullHeight;
        Width = 498;
        Margin = new Padding(0);
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public void CloseConfirm()
    {
        if (_state == RowState.Confirm)
        {
            _state = RowState.Idle;
        }
    }

    public void ReportProgress(int deleted)
    {
        _done = Math.Clamp(deleted, 0, Math.Max(_total, deleted));
        if (_total > 0)
        {
            _progress = Math.Clamp(_done / (float)_total, 0f, 1f);
        }

        Invalidate();
    }

    public void Complete()
    {
        _progress = 1f;
        _done = _total;
        _state = RowState.Done;
        _checkAnim = 0f;
        _doneHoldTicks = 0;
    }

    public void Fail()
    {
        _state = RowState.Idle;
        _progress = 0f;
        _done = 0;
        Invalidate();
    }

    private Rectangle IconRect => new(
        IsRightToLeft ? SidePadding : Width - SidePadding - IconSize,
        (Height - IconSize) / 2,
        IconSize,
        IconSize);

    private Rectangle ConfirmRect
    {
        get
        {
            var width = 172;
            var height = 34;
            var left = IsRightToLeft ? SidePadding : Width - SidePadding - width;
            return new Rectangle(left, (Height - height) / 2, width, height);
        }
    }

    private Rectangle YesRect
    {
        get
        {
            var box = ConfirmRect;
            var left = IsRightToLeft ? box.Left + 3 : box.Right - 3 - 40 - 5 - 40;
            return new Rectangle(left, box.Top + 4, 40, 26);
        }
    }

    private Rectangle NoRect
    {
        get
        {
            var box = ConfirmRect;
            var left = IsRightToLeft ? box.Left + 48 : box.Right - 3 - 40;
            return new Rectangle(left, box.Top + 4, 40, 26);
        }
    }

    private void Tick()
    {
        var changed = false;
        var targetLid = _hovered && _state == RowState.Idle ? 1f : 0f;
        if (Math.Abs(_lid - targetLid) > 0.001f)
        {
            _lid += (targetLid - _lid) * 0.25f;
            changed = true;
        }

        var targetConfirm = _state == RowState.Confirm ? 1f : 0f;
        if (Math.Abs(_confirmAnim - targetConfirm) > 0.001f)
        {
            _confirmAnim += (targetConfirm - _confirmAnim) * 0.3f;
            changed = true;
        }

        if (_state == RowState.Done)
        {
            _checkAnim = Math.Min(1f, _checkAnim + 0.08f);
            _doneHoldTicks++;
            changed = true;
            if (_doneHoldTicks > 75)
            {
                _state = RowState.Removing;
            }
        }
        else if (_state == RowState.Removing)
        {
            _collapse -= 0.06f;
            if (_collapse <= 0f)
            {
                _timer.Stop();
                RemovedFromList?.Invoke(this, EventArgs.Empty);
                return;
            }

            Height = Math.Max(1, (int)(FullHeight * _collapse));
            changed = true;
        }

        if (changed)
        {
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        base.OnMouseEnter(e);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _trashHovered = false;
        _hoverConfirm = 0;
        base.OnMouseLeave(e);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _trashHovered = _state == RowState.Idle && IconRect.Contains(e.Location);
        _hoverConfirm = _state == RowState.Confirm
            ? YesRect.Contains(e.Location) ? 1 : NoRect.Contains(e.Location) ? 2 : 0
            : 0;
        Cursor = _trashHovered || _hoverConfirm != 0 ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Right)
        {
            if (_state is RowState.Idle or RowState.Confirm)
            {
                RightClickedRow?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_state == RowState.Idle && IconRect.Contains(e.Location))
        {
            ConfirmOpened?.Invoke(this, EventArgs.Empty);
            _state = RowState.Confirm;
            Invalidate();
            return;
        }

        if (_state == RowState.Confirm)
        {
            if (YesRect.Contains(e.Location))
            {
                _state = RowState.Deleting;
                _progress = 0f;
                _done = 0;
                DeleteRequested?.Invoke(this, _entry);
            }
            else if (NoRect.Contains(e.Location))
            {
                _state = RowState.Idle;
            }

            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using (var back = new SolidBrush(_hovered && _state != RowState.Removing ? HoverColor : ItemColor))
        {
            g.FillRectangle(back, ClientRectangle);
        }

        using (var border = new Pen(BorderColor))
        {
            g.DrawLine(border, 0, Height - 1, Width, Height - 1);
        }

        var reservedWidth = IconSize + 12 + (_state is RowState.Deleting or RowState.Done ? 56 : 0);
        var textRect = IsRightToLeft
            ? new Rectangle(SidePadding + reservedWidth, 0, Math.Max(10, Width - SidePadding * 2 - reservedWidth), FullHeight)
            : new Rectangle(SidePadding, 0, Math.Max(10, Width - SidePadding * 2 - reservedWidth), FullHeight);
        var textOpacity = _state == RowState.Done ? 0.55f : 1f;
        using (var textBrush = new SolidBrush(Color.FromArgb((int)(255 * textOpacity), _hovered ? Color.White : TextColor)))
        using (var format = new StringFormat
        {
            Alignment = IsRightToLeft ? StringAlignment.Far : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap | (IsRightToLeft ? StringFormatFlags.DirectionRightToLeft : 0)
        })
        {
            g.DrawString(_title, Font, textBrush, textRect, format);
        }

        switch (_state)
        {
            case RowState.Idle:
                DrawTrash(g, IconRect, _lid, _trashHovered);
                break;
            case RowState.Confirm:
                DrawTrash(g, IconRect, 0f, false);
                DrawConfirm(g);
                break;
            case RowState.Deleting:
                DrawProgress(g);
                break;
            case RowState.Done:
            case RowState.Removing:
                DrawCheck(g);
                break;
        }
    }

    private static void DrawTrash(Graphics g, Rectangle bounds, float lid, bool hovered)
    {
        if (hovered)
        {
            using var hoverBrush = new SolidBrush(Color.FromArgb(0x20, 0x24, 0x2B));
            using var path = RoundedRect(bounds, 8);
            g.FillPath(hoverBrush, path);
        }

        var color = hovered ? Color.White : MutedColor;
        using var pen = new Pen(color, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var scale = 22f / 24f;
        var originX = bounds.Left + (bounds.Width - 22) / 2f;
        var originY = bounds.Top + (bounds.Height - 22) / 2f;
        PointF P(float x, float y) => new(originX + x * scale, originY + y * scale);

        // body
        var body = new GraphicsPath();
        body.AddLines(new[] { P(6.5f, 7), P(7.25f, 18.8f) });
        body.AddBezier(P(7.25f, 18.8f), P(7.32f, 20.02f), P(8.33f, 21), P(9.55f, 21));
        body.AddLine(P(9.55f, 21), P(14.45f, 21));
        body.AddBezier(P(14.45f, 21), P(15.67f, 21), P(16.68f, 20.02f), P(16.75f, 18.8f));
        body.AddLine(P(16.75f, 18.8f), P(17.5f, 7));
        g.DrawPath(pen, body);
        using var thin = new Pen(color, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(thin, P(9.5f, 10.5f), P(9.5f, 17.5f));
        g.DrawLine(thin, P(14.5f, 10.5f), P(14.5f, 17.5f));

        // lid (rotates around its left corner on hover, like the CSS)
        var state = g.Save();
        var pivot = P(12, 6);
        g.TranslateTransform(pivot.X + 2f * lid, pivot.Y - 2f * lid);
        g.RotateTransform(14f * lid);
        g.TranslateTransform(-pivot.X, -pivot.Y);
        g.DrawLine(pen, P(4, 7), P(20, 7));
        var handle = new GraphicsPath();
        handle.AddLine(P(9, 7), P(9, 5.5f));
        handle.AddBezier(P(9, 5.5f), P(9, 4.67f), P(9.67f, 4), P(10.5f, 4));
        handle.AddLine(P(10.5f, 4), P(13.5f, 4));
        handle.AddBezier(P(13.5f, 4), P(14.33f, 4), P(15, 4.67f), P(15, 5.5f));
        handle.AddLine(P(15, 5.5f), P(15, 7));
        g.DrawPath(pen, handle);
        g.Restore(state);
    }

    private void DrawConfirm(Graphics g)
    {
        var alpha = (int)(255 * Math.Clamp(_confirmAnim, 0f, 1f));
        if (alpha <= 0)
        {
            return;
        }

        var box = ConfirmRect;
        using (var path = RoundedRect(box, 8))
        using (var fill = new SolidBrush(Color.FromArgb(alpha, 0x15, 0x18, 0x1D)))
        using (var border = new Pen(Color.FromArgb(alpha, 0x30, 0x36, 0x40)))
        {
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        using var smallFont = new Font("Segoe UI", 8F);
        var textRect = IsRightToLeft
            ? new Rectangle(box.Right - 78, box.Top, 70, box.Height)
            : new Rectangle(box.Left + 8, box.Top, 70, box.Height);
        using (var textBrush = new SolidBrush(Color.FromArgb(alpha, 0xCD, 0xD2, 0xD8)))
        using (var format = new StringFormat
        {
            Alignment = IsRightToLeft ? StringAlignment.Far : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap | (IsRightToLeft ? StringFormatFlags.DirectionRightToLeft : 0)
        })
        {
            g.DrawString(ConfirmText, smallFont, textBrush, textRect, format);
        }

        DrawSmallButton(g, YesRect, YesText, _hoverConfirm == 1, true, alpha, smallFont);
        DrawSmallButton(g, NoRect, NoText, _hoverConfirm == 2, false, alpha, smallFont);
    }

    private static void DrawSmallButton(Graphics g, Rectangle rect, string text, bool hovered, bool isYes, int alpha, Font font)
    {
        var back = hovered ? Color.FromArgb(0x25, 0x2A, 0x31) : Color.FromArgb(0x1B, 0x1F, 0x25);
        var border = hovered && isYes ? Color.FromArgb(0x30, 0x6F, 0x52) : Color.FromArgb(0x30, 0x36, 0x40);
        var fore = hovered ? (isYes ? GreenColor : Color.White) : Color.FromArgb(0xCD, 0xD2, 0xD8);
        using var path = RoundedRect(rect, 5);
        using var fill = new SolidBrush(Color.FromArgb(alpha, back));
        using var pen = new Pen(Color.FromArgb(alpha, border));
        using var brush = new SolidBrush(Color.FromArgb(alpha, fore));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
        g.DrawString(text, font, brush, rect, format);
    }

    private void DrawProgress(Graphics g)
    {
        var icon = IconRect;
        var ring = new Rectangle(icon.Left + 3, icon.Top + 3, 26, 26);
        using (var track = new Pen(Color.FromArgb(0x2A, 0x2F, 0x37), 2.5f))
        {
            g.DrawEllipse(track, ring);
        }

        using (var bar = new Pen(AccentColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            var sweep = Math.Max(2f, 360f * _progress);
            g.DrawArc(bar, ring, -90f, sweep);
        }

        DrawCounter(g, icon.Left - 8);
    }

    private void DrawCounter(Graphics g, int right)
    {
        var text = _total > 0 ? _done + "/" + _total : _done.ToString();
        using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var brush = new SolidBrush(MutedColor);
        using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, brush, new Rectangle(right - 60, 0, 60, FullHeight), format);
    }

    private void DrawCheck(Graphics g)
    {
        var icon = IconRect;
        var ring = new Rectangle(icon.Left + 3, icon.Top + 3, 26, 26);
        var alpha = (int)(255 * Math.Clamp(_checkAnim, 0f, 1f));
        using (var fill = new SolidBrush(Color.FromArgb((int)(alpha * 0.14f), GreenColor)))
        {
            g.FillEllipse(fill, ring);
        }

        using var pen = new Pen(Color.FromArgb(alpha, GreenColor), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawEllipse(pen, ring);
        var cx = ring.Left + 13f;
        var cy = ring.Top + 13f;
        var t = Math.Clamp(_checkAnim, 0f, 1f);
        var p1 = new PointF(cx - 5f, cy + 0.5f);
        var p2 = new PointF(cx - 1.5f, cy + 4f);
        var p3 = new PointF(cx + 5.5f, cy - 4f);
        if (t < 0.5f)
        {
            var k = t / 0.5f;
            g.DrawLine(pen, p1, new PointF(p1.X + (p2.X - p1.X) * k, p1.Y + (p2.Y - p1.Y) * k));
        }
        else
        {
            var k = (t - 0.5f) / 0.5f;
            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen, p2, new PointF(p2.X + (p3.X - p2.X) * k, p2.Y + (p3.Y - p2.Y) * k));
        }

        if (_total > 0 || _done > 0)
        {
            DrawCounter(g, icon.Left - 8);
        }
    }

    internal string ConfirmText { get; set; } = "Are you sure?";
    internal string YesText { get; set; } = "Yes";
    internal string NoText { get; set; } = "No";

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
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
}
