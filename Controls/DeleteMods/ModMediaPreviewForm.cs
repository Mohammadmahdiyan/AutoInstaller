namespace GtaSaModManager.Controls.DeleteMods;

/// <summary>
/// Right-click preview of a mod: description on one side, paged media gallery (4 per page) on the other.
/// Text only / media only / empty layouts like the HTML design. Everything comes from the MOD folder.
/// </summary>
internal sealed class ModMediaPreviewForm : Form
{
    private const int MediaPerPage = 4;
    private readonly DeleteModEntry _entry;
    private readonly DeleteModsTexts _texts;
    private readonly TableLayoutPanel _grid = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 2,
        BackColor = Color.Transparent
    };
    private readonly Label _counter = new() { AutoSize = true, ForeColor = DeleteModRow.MutedColor, Font = new Font("Segoe UI", 9F) };
    private readonly Button _prev = CreateSmallButton("‹");
    private readonly Button _next = CreateSmallButton("›");
    private readonly List<MediaPreviewControl> _cards = new();
    private readonly System.Windows.Forms.Timer _clickTimer = new() { Interval = 230 };
    private int _page;
    private int _pendingIndex = -1;
    private bool _viewerOpen;

    public ModMediaPreviewForm(DeleteModEntry entry, DeleteModsTexts texts)
    {
        _entry = entry;
        _texts = texts;
        var hasText = !string.IsNullOrWhiteSpace(entry.Description);
        var hasMedia = entry.MediaFiles.Count > 0;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(0x10, 0x12, 0x16);
        ForeColor = DeleteModRow.TextColor;
        ShowInTaskbar = false;
        KeyPreview = true;
        Padding = new Padding(1);
        Size = hasText && hasMedia ? new Size(980, 560) : hasMedia ? new Size(620, 560) : new Size(560, 380);
        Paint += (_, e) =>
        {
            using var pen = new Pen(DeleteModRow.BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
        };
        Deactivate += (_, _) =>
        {
            if (!_viewerOpen)
            {
                Close();
            }
        };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = hasText && hasMedia ? 2 : 1, RowCount = 1, BackColor = Color.Transparent };
        root.RightToLeft = texts.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        if (hasText && hasMedia)
        {
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        }
        else
        {
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        if (!hasText && !hasMedia)
        {
            root.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = texts.NoPhotoNoText,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F),
                ForeColor = DeleteModRow.MutedColor
            }, 0, 0);
        }
        else
        {
            if (hasText)
            {
                root.Controls.Add(BuildDescription(), 0, 0);
            }

            if (hasMedia)
            {
                root.Controls.Add(BuildGallery(), hasText ? 1 : 0, 0);
            }
        }

        Controls.Add(root);
        _clickTimer.Tick += (_, _) =>
        {
            _clickTimer.Stop();
            if (_pendingIndex >= 0)
            {
                OpenViewer(_pendingIndex);
            }

            _pendingIndex = -1;
        };
        FormClosed += (_, _) =>
        {
            _clickTimer.Dispose();
            DisposeCards();
        };
    }

    private Control BuildDescription()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22), BackColor = Color.Transparent };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Text = _entry.Name,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoEllipsis = true,
            TextAlign = _texts.IsRightToLeft ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
        };
        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(0x12, 0x15, 0x1A),
            ForeColor = DeleteModRow.TextColor,
            Font = new Font("Segoe UI", 10F),
            Text = _entry.Description,
            RightToLeft = _texts.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No
        };
        panel.Controls.Add(box);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildGallery()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18), BackColor = Color.Transparent };
        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = _texts.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        _prev.Text = _texts.IsRightToLeft ? "›" : "‹";
        _next.Text = _texts.IsRightToLeft ? "‹" : "›";
        _counter.Margin = new Padding(0, 10, 18, 0);
        controls.Controls.Add(_prev);
        controls.Controls.Add(_next);
        controls.Controls.Add(_counter);
        _prev.Click += (_, _) => { if (_page > 0) { _page--; RenderGallery(); } };
        _next.Click += (_, _) =>
        {
            if ((_page + 1) * MediaPerPage < _entry.MediaFiles.Count)
            {
                _page++;
                RenderGallery();
            }
        };
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        panel.Controls.Add(_grid);
        panel.Controls.Add(controls);
        Shown += (_, _) => RenderGallery();
        return panel;
    }

    private static Button CreateSmallButton(string text) => new()
    {
        Text = text,
        Width = 40,
        Height = 32,
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        BackColor = Color.FromArgb(0x1B, 0x1F, 0x25),
        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
        Margin = new Padding(0, 0, 8, 0)
    };

    private void DisposeCards()
    {
        foreach (var card in _cards)
        {
            card.Dispose();
        }

        _cards.Clear();
        _grid.Controls.Clear();
    }

    private void RenderGallery()
    {
        DisposeCards();
        var media = _entry.MediaFiles;
        var start = _page * MediaPerPage;
        var end = Math.Min(start + MediaPerPage, media.Count);
        for (var index = start; index < end; index++)
        {
            var realIndex = index;
            var card = new MediaPreviewControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(0x12, 0x15, 0x1A)
            };
            if (!card.LoadMedia(media[index]))
            {
                card.Dispose();
                continue;
            }

            card.Clicked += (_, _) =>
            {
                // A double click on a video only toggles the sound, a single click opens the viewer.
                _pendingIndex = realIndex;
                _clickTimer.Stop();
                _clickTimer.Start();
            };
            card.SoundStateChanged += (_, _) =>
            {
                _pendingIndex = -1;
                _clickTimer.Stop();
            };
            _cards.Add(card);
            var local = index - start;
            _grid.Controls.Add(card, local % 2, local / 2);
        }

        _counter.Text = media.Count == 0 ? string.Empty : $"{start + 1} - {end} / {media.Count}";
        _prev.Enabled = _page > 0;
        _next.Enabled = end < media.Count;
    }

    private void OpenViewer(int index)
    {
        _viewerOpen = true;
        foreach (var card in _cards)
        {
            card.SetSoundEnabled(false);
        }

        try
        {
            using var viewer = new ModMediaViewerForm(_entry.MediaFiles, index, _texts);
            viewer.ShowDialog(this);
        }
        finally
        {
            _viewerOpen = false;
        }
    }
}
