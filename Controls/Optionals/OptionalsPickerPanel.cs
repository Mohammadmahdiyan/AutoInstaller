using System.Drawing.Drawing2D;
using GtaSaModManager.Controls.DeleteMods;

namespace GtaSaModManager.Controls.Optionals;

/// <summary>Localizable texts of the optionals window (defaults are English).</summary>
public sealed class OptionalsTexts
{
    public bool IsRightToLeft { get; set; }
    public string Title { get; set; } = "Optional mods";
    public string Hint { get; set; } = "Click a mod to install it. Right-click it to see its details.";
    public string Install { get; set; } = "Install";
    public string Installed { get; set; } = "Installed";
    public string Finish { get; set; } = "Finish";
    public string Empty { get; set; } = "There are no optional mods in this package.";
}

/// <summary>One row of the optionals window.</summary>
public sealed class OptionalItem
{
    /// <summary>Folder of the optional package inside the mod folder (also its unique key).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Name, README and media (read from the mod folder) used for the row and its preview.</summary>
    public DeleteModEntry Entry { get; set; } = new();

    public bool Installed { get; set; }
}

/// <summary>
/// Step 5 window for "optionals": lists every folder of the optionals folder. Clicking a row asks the
/// owner to install it; installed rows are shown faded. Same look as the Delete Mods window.
/// </summary>
public sealed class OptionalsPickerPanel : Panel
{
    private const int WindowWidth = 500;

    private readonly FlowLayoutPanel _window = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(1),
        BackColor = Color.FromArgb(0x10, 0x12, 0x16)
    };
    private readonly Label _title = new();
    private readonly Label _hint = new();
    private readonly Label _empty = new();
    private readonly FlowLayoutPanel _table = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0),
        Padding = new Padding(0)
    };
    private readonly Button _finish = new();
    private readonly List<OptionalRow> _rows = new();

    public OptionalsTexts Texts { get; set; } = new();

    /// <summary>Texts for the right-click preview window.</summary>
    public DeleteModsTexts PreviewTexts { get; set; } = new();

    public event EventHandler<OptionalItem>? InstallRequested;
    public event EventHandler? FinishRequested;

    public bool AllInstalled => _rows.Count > 0 && _rows.All(row => row.Item.Installed);

    public OptionalsPickerPanel()
    {
        AutoScroll = true;
        BackColor = Color.Transparent;
        DoubleBuffered = true;

        var header = new Panel { Width = WindowWidth - 2, Height = 64, Margin = new Padding(0), BackColor = Color.Transparent };
        _title.Dock = DockStyle.Fill;
        _title.Padding = new Padding(20, 0, 20, 0);
        _title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _title.ForeColor = Color.FromArgb(0xF1, 0xF3, 0xF5);
        header.Controls.Add(_title);
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(DeleteModRow.BorderColor);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        _hint.Width = WindowWidth - 2;
        _hint.Height = 34;
        _hint.Margin = new Padding(0);
        _hint.Padding = new Padding(20, 8, 20, 0);
        _hint.ForeColor = DeleteModRow.MutedColor;
        _hint.Font = new Font("Segoe UI", 8.5F);

        _empty.Width = WindowWidth - 2;
        _empty.Height = 80;
        _empty.Margin = new Padding(0);
        _empty.TextAlign = ContentAlignment.MiddleCenter;
        _empty.ForeColor = DeleteModRow.MutedColor;
        _empty.Visible = false;

        var footer = new Panel { Width = WindowWidth - 2, Height = 64, Margin = new Padding(0), BackColor = Color.Transparent };
        _finish.Size = new Size(120, 36);
        _finish.FlatStyle = FlatStyle.Flat;
        _finish.FlatAppearance.BorderColor = Color.FromArgb(0x30, 0x36, 0x40);
        _finish.BackColor = Color.FromArgb(0x1B, 0x1F, 0x25);
        _finish.ForeColor = Color.White;
        _finish.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _finish.Click += (_, _) => FinishRequested?.Invoke(this, EventArgs.Empty);
        footer.Controls.Add(_finish);
        footer.Resize += (_, _) => PlaceFinishButton(footer);
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(DeleteModRow.BorderColor);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };
        footer.Tag = "footer";

        _window.Controls.Add(header);
        _window.Controls.Add(_hint);
        _window.Controls.Add(_table);
        _window.Controls.Add(_empty);
        _window.Controls.Add(footer);
        _window.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(0x22, 0x26, 0x2D));
            e.Graphics.DrawRectangle(pen, 0, 0, _window.Width - 1, _window.Height - 1);
        };
        Controls.Add(_window);
        Resize += (_, _) => CenterWindow();
        _window.SizeChanged += (_, _) => CenterWindow();
    }

    private void PlaceFinishButton(Control footer)
    {
        _finish.Location = new Point(
            Texts.IsRightToLeft ? 20 : footer.Width - _finish.Width - 20,
            (footer.Height - _finish.Height) / 2);
    }

    public void SetItems(IReadOnlyList<OptionalItem> items)
    {
        foreach (var row in _rows)
        {
            row.Dispose();
        }

        _rows.Clear();
        _table.Controls.Clear();

        var rtl = Texts.IsRightToLeft;
        RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
        _title.Text = Texts.Title;
        _title.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _hint.Text = Texts.Hint;
        _hint.TextAlign = rtl ? ContentAlignment.TopRight : ContentAlignment.TopLeft;
        _empty.Text = Texts.Empty;
        _finish.Text = Texts.Finish;

        for (var index = 0; index < items.Count; index++)
        {
            var row = new OptionalRow(items[index], index + 1, Texts);
            row.InstallClicked += (_, item) => InstallRequested?.Invoke(this, item);
            row.PreviewRequested += (_, item) => ShowPreview(item);
            _rows.Add(row);
            _table.Controls.Add(row);
        }

        _empty.Visible = items.Count == 0;
        foreach (var footer in _window.Controls.OfType<Panel>().Where(panel => panel.Tag as string == "footer"))
        {
            PlaceFinishButton(footer);
        }

        CenterWindow();
    }

    /// <summary>Updates the faded/installed state of the row with this key.</summary>
    public void SetInstalled(string key, bool installed)
    {
        foreach (var row in _rows.Where(row => string.Equals(row.Item.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            row.Item.Installed = installed;
            row.Invalidate();
        }
    }

    private void ShowPreview(OptionalItem item)
    {
        using var preview = new ModMediaPreviewForm(item.Entry, PreviewTexts);
        preview.ShowDialog(FindForm());
    }

    private void CenterWindow()
    {
        var x = Math.Max(0, (ClientSize.Width - _window.Width) / 2);
        _window.Location = new Point(x + AutoScrollPosition.X, 12 + AutoScrollPosition.Y);
        AutoScrollMinSize = new Size(0, _window.Height + 24);
    }
}

internal sealed class OptionalRow : Control
{
    private const int RowHeight = 58;
    private const int SidePadding = 20;
    private readonly OptionalsTexts _texts;
    private readonly string _title;
    private bool _hovered;
    private bool _pillHovered;

    public OptionalItem Item { get; }

    public event EventHandler<OptionalItem>? InstallClicked;
    public event EventHandler<OptionalItem>? PreviewRequested;

    public OptionalRow(OptionalItem item, int number, OptionalsTexts texts)
    {
        Item = item;
        _texts = texts;
        _title = number + ". " + item.Entry.Name;
        Width = 498;
        Height = RowHeight;
        Margin = new Padding(0);
        Font = new Font("Segoe UI", 10.5F);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }

    private Rectangle PillRect
    {
        get
        {
            const int width = 96;
            const int height = 30;
            var left = _texts.IsRightToLeft ? SidePadding : Width - SidePadding - width;
            return new Rectangle(left, (Height - height) / 2, width, height);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; base.OnMouseEnter(e); Invalidate(); }

    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pillHovered = false; base.OnMouseLeave(e); Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _pillHovered = PillRect.Contains(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Right)
        {
            PreviewRequested?.Invoke(this, Item);
        }
        else if (e.Button == MouseButtons.Left)
        {
            InstallClicked?.Invoke(this, Item);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var back = new SolidBrush(_hovered ? DeleteModRow.HoverColor : DeleteModRow.ItemColor))
        {
            g.FillRectangle(back, ClientRectangle);
        }

        using (var border = new Pen(DeleteModRow.BorderColor))
        {
            g.DrawLine(border, 0, Height - 1, Width, Height - 1);
        }

        var rtl = _texts.IsRightToLeft;
        var pill = PillRect;
        var textRect = rtl
            ? new Rectangle(SidePadding * 2 + pill.Width, 0, Width - SidePadding * 3 - pill.Width, RowHeight)
            : new Rectangle(SidePadding, 0, Width - SidePadding * 3 - pill.Width, RowHeight);
        var alpha = Item.Installed ? 110 : 255;
        using (var textBrush = new SolidBrush(Color.FromArgb(alpha, _hovered && !Item.Installed ? Color.White : DeleteModRow.TextColor)))
        using (var format = new StringFormat
        {
            Alignment = rtl ? StringAlignment.Far : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap | (rtl ? StringFormatFlags.DirectionRightToLeft : 0)
        })
        {
            g.DrawString(_title, Font, textBrush, textRect, format);
        }

        using var pillPath = DeleteModRow.RoundedRect(pill, 8);
        using var pillFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var pillFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        if (Item.Installed)
        {
            using var fill = new SolidBrush(Color.FromArgb(0x24, DeleteModRow.GreenColor));
            using var pen = new Pen(Color.FromArgb(0x80, DeleteModRow.GreenColor));
            using var brush = new SolidBrush(DeleteModRow.GreenColor);
            g.FillPath(fill, pillPath);
            g.DrawPath(pen, pillPath);
            g.DrawString("✓ " + _texts.Installed, pillFont, brush, pill, pillFormat);
        }
        else
        {
            using var fill = new SolidBrush(_pillHovered ? Color.FromArgb(0x1F, 0x4E, 0x6B) : Color.FromArgb(0x1B, 0x1F, 0x25));
            using var pen = new Pen(_pillHovered ? DeleteModRow.AccentColor : Color.FromArgb(0x30, 0x36, 0x40));
            using var brush = new SolidBrush(_pillHovered ? Color.White : DeleteModRow.AccentColor);
            g.FillPath(fill, pillPath);
            g.DrawPath(pen, pillPath);
            g.DrawString(_texts.Install, pillFont, brush, pill, pillFormat);
        }
    }
}
