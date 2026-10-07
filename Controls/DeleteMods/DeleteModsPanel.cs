namespace GtaSaModManager.Controls.DeleteMods;

/// <summary>Localizable texts of the delete window (defaults are English).</summary>
public sealed class DeleteModsTexts
{
    public bool IsRightToLeft { get; set; }
    public string Title { get; set; } = "Delete Mods";
    public string AreYouSure { get; set; } = "Are you sure?";
    public string Yes { get; set; } = "Yes";
    public string No { get; set; } = "No";
    public string NothingLeft { get; set; } = "Nothing left to delete.";
    public string DeleteFailed { get; set; } = "The selected item could not be deleted.";
    public string NoPhotoNoText { get; set; } = "NO Photo and No Text for this Mod";
    public string SoundOn { get; set; } = "🔊 Sound On";
    public string SoundOff { get; set; } = "🔇 Sound Off";
}

/// <summary>
/// The "Delete Mods" window (port of DeleteSomeModUi.html). It is used by Step 5 for
/// Mixed mods and for "Delete some mods" opened from Step 1.
/// </summary>
public sealed class DeleteModsPanel : Panel
{
    private const int WindowWidth = 500;
    private const int HeaderHeight = 64;

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
    private readonly FlowLayoutPanel _table = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0),
        Padding = new Padding(0)
    };
    private readonly Label _emptyLabel = new() { Width = WindowWidth - 2, Height = 80, Margin = new Padding(0), TextAlign = ContentAlignment.MiddleCenter, Visible = false };
    private readonly List<DeleteModRow> _rows = new();
    private DeleteModHandler? _handler;
    private int _busyCount;

    public DeleteModsTexts Texts { get; set; } = new();

    /// <summary>True while at least one item is being deleted (the wizard should block navigation).</summary>
    public bool IsBusy => _busyCount > 0;

    /// <summary>True after at least one entry has been successfully removed.</summary>
    public bool HasRemovedEntry { get; private set; }

    public event EventHandler? BusyChanged;
    public event EventHandler<DeleteModEntry>? EntryRemoved;
    public event EventHandler? AllRemoved;

    public DeleteModsPanel()
    {
        AutoScroll = true;
        BackColor = Color.Transparent;
        DoubleBuffered = true;

        var header = new Panel { Width = WindowWidth - 2, Height = HeaderHeight, Margin = new Padding(0), BackColor = Color.Transparent };
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

        _emptyLabel.ForeColor = DeleteModRow.MutedColor;
        _emptyLabel.Font = new Font("Segoe UI", 10F);

        _window.Controls.Add(header);
        _window.Controls.Add(_table);
        _window.Controls.Add(_emptyLabel);
        _window.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(0x22, 0x26, 0x2D));
            e.Graphics.DrawRectangle(pen, 0, 0, _window.Width - 1, _window.Height - 1);
        };
        Controls.Add(_window);
        Resize += (_, _) => CenterWindow();
        RightToLeftChanged += (_, _) => ApplyDirection();
        _window.SizeChanged += (_, _) => CenterWindow();
        Click += (_, _) => CloseAllConfirms(null);
    }

    public void SetEntries(IReadOnlyList<DeleteModEntry> entries, DeleteModHandler handler)
    {
        _handler = handler;
        _title.Text = Texts.Title;
        ApplyDirection();
        foreach (var row in _rows)
        {
            row.Dispose();
        }

        _rows.Clear();
        _table.Controls.Clear();
        _busyCount = 0;
        HasRemovedEntry = false;

        for (var index = 0; index < entries.Count; index++)
        {
            var row = new DeleteModRow(entries[index], index + 1)
            {
                ConfirmText = Texts.AreYouSure,
                YesText = Texts.Yes,
                NoText = Texts.No,
                IsRightToLeft = Texts.IsRightToLeft
            };
            row.ConfirmOpened += (_, _) => CloseAllConfirms(row);
            row.RightClickedRow += (_, _) => ShowPreview(row.Entry);
            row.DeleteRequested += async (_, entry) => await DeleteAsync(row, entry);
            row.RemovedFromList += (_, _) => RemoveRow(row);
            _rows.Add(row);
            _table.Controls.Add(row);
        }

        UpdateEmptyState();
        CenterWindow();
    }

    private void ApplyDirection()
    {
        var isRtl = Texts.IsRightToLeft;
        RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;
        _title.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _window.RightToLeft = RightToLeft;
        _table.RightToLeft = RightToLeft;
        _emptyLabel.RightToLeft = RightToLeft;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
    }

    private void CloseAllConfirms(DeleteModRow? except)
    {
        foreach (var row in _rows)
        {
            if (!ReferenceEquals(row, except))
            {
                row.CloseConfirm();
            }
        }
    }

    private void ShowPreview(DeleteModEntry entry)
    {
        using var preview = new ModMediaPreviewForm(entry, Texts);
        preview.ShowDialog(FindForm());
    }

    private async Task DeleteAsync(DeleteModRow row, DeleteModEntry entry)
    {
        if (_handler == null)
        {
            row.Fail();
            return;
        }

        SetBusy(+1);
        try
        {
            var progress = new Progress<int>(done => row.ReportProgress(done));
            var ok = await _handler(entry, progress);
            if (ok)
            {
                row.ReportProgress(entry.Files.Count);
                row.Complete();
            }
            else
            {
                row.Fail();
                MessageBox.Show(FindForm(), Texts.DeleteFailed, Texts.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            row.Fail();
            MessageBox.Show(FindForm(), ex.Message, Texts.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(-1);
        }
    }

    private void SetBusy(int delta)
    {
        _busyCount = Math.Max(0, _busyCount + delta);
        BusyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveRow(DeleteModRow row)
    {
        _rows.Remove(row);
        _table.Controls.Remove(row);
        HasRemovedEntry = true;
        EntryRemoved?.Invoke(this, row.Entry);
        row.Dispose();
        UpdateEmptyState();
        if (_rows.Count == 0)
        {
            AllRemoved?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateEmptyState()
    {
        _emptyLabel.Text = Texts.NothingLeft;
        _emptyLabel.Visible = _rows.Count == 0;
    }

    private void CenterWindow()
    {
        var x = Math.Max(0, (ClientSize.Width - _window.Width) / 2);
        _window.Location = new Point(x + AutoScrollPosition.X, 12 + AutoScrollPosition.Y);
        AutoScrollMinSize = new Size(0, _window.Height + 24);
    }
}
