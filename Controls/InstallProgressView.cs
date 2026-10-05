using GtaSaModManager.Services;
using GtaSaModManager.Models;
using GtaSaModManager.UI;

namespace GtaSaModManager.Controls;

public sealed class InstallProgressView : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _statusLabel;
    private readonly AnimatedProgressBar _progressBar;
    private readonly ProgressFileList _fileList;
    private readonly TableLayoutPanel _layout;
    private ThemePalette _palette = ThemeManager.ResolvePalette(AppTheme.System);
    private int _activeFileIndex = -1;

    public InstallProgressView()
    {
        Name = "Step4InstallProgressView";
        Dock = DockStyle.Fill;
        MinimumSize = new Size(0, 150);
        Padding = new Padding(12);
        BackColor = _palette.Surface;

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = _palette.TextPrimary,
            Margin = new Padding(0, 0, 0, 2)
        };
        _statusLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = _palette.TextSecondary,
            Margin = new Padding(0, 0, 0, 2),
            AutoEllipsis = true
        };
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(_titleLabel, 0, 0);
        header.Controls.Add(_statusLabel, 0, 1);

        _progressBar = new AnimatedProgressBar
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        _fileList = new ProgressFileList
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            IntegralHeight = false
        };

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(header, 0, 0);
        _layout.Controls.Add(_progressBar, 0, 1);
        _layout.Controls.Add(_fileList, 0, 2);
        Controls.Add(_layout);
        RightToLeftChanged += (_, _) => ApplyDirection();
        ApplyPalette(_palette);
    }

    public bool IsRunning { get; private set; }

    public bool HasFileRows => _fileList.Items.Count > 0;

    public string TitleText => _titleLabel.Text;

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        _titleLabel.ForeColor = palette.TextPrimary;
        _statusLabel.ForeColor = palette.TextSecondary;
        _fileList.ApplyPalette(palette);
        _progressBar.ApplyPalette(palette);
        Invalidate(true);
    }

    public void Reset(string title)
    {
        _titleLabel.Text = title;
        _statusLabel.Text = title;
        _fileList.Items.Clear();
        _activeFileIndex = -1;
        IsRunning = true;
        _progressBar.Reset();
    }

    public int BeginFile(string relativeName)
    {
        if (_activeFileIndex >= 0)
        {
            CompleteFile();
        }

        var safeName = string.IsNullOrWhiteSpace(relativeName) ? Path.GetFileName(relativeName) : relativeName;
        _activeFileIndex = _fileList.AddFile(safeName);
        _statusLabel.Text = safeName;
        return _activeFileIndex;
    }

    public void ReportFileProgress(int percent)
    {
        if (_activeFileIndex < 0)
        {
            return;
        }
        _fileList.UpdateFile(_activeFileIndex, Math.Clamp(percent, 0, 100), active: true);
    }

    public void ReportFileProgress(int fileIndex, int percent)
    {
        if (_activeFileIndex != fileIndex)
        {
            return;
        }
        _fileList.UpdateFile(fileIndex, Math.Clamp(percent, 0, 100), active: true);
    }

    public void CompleteFile()
    {
        if (_activeFileIndex < 0)
        {
            return;
        }
        _fileList.UpdateFile(_activeFileIndex, null, active: false);
        _activeFileIndex = -1;
    }

    public void CompleteFile(int fileIndex)
    {
        if (_activeFileIndex != fileIndex)
        {
            return;
        }
        CompleteFile();
    }

    public void SetOverallProgress(int percent)
    {
        _progressBar.SetProgress(percent);
    }

    public void Complete(string message)
    {
        CompleteFile();
        IsRunning = false;
        _statusLabel.Text = message;
        _progressBar.Complete();
    }

    public void Fail(string message)
    {
        IsRunning = false;
        _statusLabel.Text = message;
        _progressBar.Fail();
    }

    public void ShowCompletedFiles(IEnumerable<string> relativeNames, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            _titleLabel.Text = title;
            _statusLabel.Text = title;
        }
        _fileList.Items.Clear();
        _activeFileIndex = -1;
        foreach (var relativeName in relativeNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            _fileList.AddCompletedFile(relativeName);
        }
        IsRunning = false;
        _progressBar.Complete();
    }

    private void ApplyDirection()
    {
        var rtl = RightToLeft == RightToLeft.Yes;
        _titleLabel.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _statusLabel.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _fileList.RightToLeft = RightToLeft;
        _progressBar.RightToLeft = RightToLeft;
    }

    private sealed class ProgressFileList : ListBox
    {
        private ThemePalette _palette = ThemeManager.ResolvePalette(AppTheme.System);

        public ProgressFileList()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            BorderStyle = BorderStyle.FixedSingle;
            IntegralHeight = false;
            ItemHeight = Math.Max(26, (int)Math.Round(28 * DeviceDpi / 96d));
            HorizontalScrollbar = true;
            SelectionMode = SelectionMode.None;
        }

        public void ApplyPalette(ThemePalette palette)
        {
            _palette = palette;
            BackColor = palette.SurfaceSecondary;
            ForeColor = palette.TextPrimary;
            Invalidate();
        }

        public int AddFile(string relativeName)
        {
            var index = Items.Add(new FileRow(relativeName, null, Active: true));
            ScrollToBottom();
            return index;
        }

        public void AddCompletedFile(string relativeName)
        {
            Items.Add(new FileRow(relativeName, null, Active: false));
            ScrollToBottom();
        }

        public void UpdateFile(int index, int? percent, bool active)
        {
            if ((uint)index >= (uint)Items.Count)
            {
                return;
            }

            if (Items[index] is FileRow row)
            {
                Items[index] = row with { Percent = percent, Active = active };
                if (active)
                {
                    ScrollToBottom();
                }
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not FileRow row)
            {
                e.DrawBackground();
                return;
            }

            var background = row.Active ? _palette.AccentSoft : _palette.SurfaceSecondary;
            using (var brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            var text = $"{e.Index + 1}. {row.Name}";
            if (row.Percent.HasValue)
            {
                text += $" ({row.Percent.Value}%)";
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            flags |= RightToLeft == RightToLeft.Yes
                ? TextFormatFlags.RightToLeft | TextFormatFlags.Right
                : TextFormatFlags.Left;
            var textBounds = Rectangle.Inflate(e.Bounds, -10, 0);
            TextRenderer.DrawText(e.Graphics, text, Font, textBounds, _palette.TextPrimary, flags);
            if (e.Index < Items.Count - 1)
            {
                using var pen = new Pen(_palette.BorderSoft);
                e.Graphics.DrawLine(pen, e.Bounds.Left + 8, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);
            }
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ItemHeight = Math.Max(26, (int)Math.Round(28 * DeviceDpi / 96d));
        }

        private void ScrollToBottom()
        {
            if (Items.Count == 0)
            {
                return;
            }
            TopIndex = Math.Max(0, Items.Count - Math.Max(1, ClientSize.Height / Math.Max(1, ItemHeight)));
        }

        private sealed record FileRow(string Name, int? Percent, bool Active);
    }
}