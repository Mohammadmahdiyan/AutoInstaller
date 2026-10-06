namespace GtaSaModManager.Controls.DeleteMods;

/// <summary>Large viewer: Prev/Next over all media, close button, sound button for videos.</summary>
internal sealed class ModMediaViewerForm : Form
{
    private readonly IReadOnlyList<string> _media;
    private readonly DeleteModsTexts _texts;
    private readonly Panel _stage = new() { Dock = DockStyle.Fill, BackColor = Color.Black };
    private readonly Button _prev = CreateNavButton("‹");
    private readonly Button _next = CreateNavButton("›");
    private readonly Button _close = CreateNavButton("×");
    private readonly Button _mute = new()
    {
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        BackColor = Color.FromArgb(40, 44, 52),
        Height = 34,
        Width = 130,
        Visible = false
    };
    private MediaPreviewControl? _current;
    private int _index;

    public ModMediaViewerForm(IReadOnlyList<string> media, int startIndex, DeleteModsTexts texts)
    {
        _media = media;
        _texts = texts;
        _index = Math.Clamp(startIndex, 0, Math.Max(0, media.Count - 1));
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(8, 9, 11);
        KeyPreview = true;
        ShowInTaskbar = false;
        Size = new Size(1000, 620);
        _mute.FlatAppearance.BorderColor = Color.FromArgb(60, 66, 76);

        var isRtl = texts.IsRightToLeft;
        RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;
        _close.Location = new Point(isRtl ? 8 : Width - 56, 12);
        _close.Anchor = AnchorStyles.Top | (isRtl ? AnchorStyles.Left : AnchorStyles.Right);
        _prev.Text = isRtl ? "›" : "‹";
        _prev.Location = new Point(isRtl ? Width - 62 : 14, Height / 2 - 24);
        _prev.Anchor = isRtl ? AnchorStyles.Right : AnchorStyles.Left;
        _next.Text = isRtl ? "‹" : "›";
        _next.Location = new Point(isRtl ? 14 : Width - 62, Height / 2 - 24);
        _next.Anchor = isRtl ? AnchorStyles.Left : AnchorStyles.Right;
        _mute.Location = new Point(Width / 2 - 65, Height - 52);
        _mute.Anchor = AnchorStyles.Bottom;

        Controls.Add(_close);
        Controls.Add(_prev);
        Controls.Add(_next);
        Controls.Add(_mute);
        Controls.Add(_stage);
        _stage.Margin = new Padding(0);
        _stage.Padding = new Padding(70, 20, 70, 60);
        _close.BringToFront();
        _prev.BringToFront();
        _next.BringToFront();
        _mute.BringToFront();

        _close.Click += (_, _) => Close();
        _prev.Click += (_, _) => NavigateMedia(-1);
        _next.Click += (_, _) => NavigateMedia(1);
        _mute.Click += (_, _) => ToggleSound();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.KeyCode == Keys.Left) NavigateMedia(isRtl ? 1 : -1);
            else if (e.KeyCode == Keys.Right) NavigateMedia(isRtl ? -1 : 1);
        };
        Shown += (_, _) => Render();
        FormClosed += (_, _) => _current?.Dispose();
    }

    private static Button CreateNavButton(string text) => new()
    {
        Text = text,
        Width = 48,
        Height = 48,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 18F, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.FromArgb(30, 34, 40),
        TabStop = false
    };

    private void NavigateMedia(int delta)
    {
        var next = _index + delta;
        if (next < 0 || next >= _media.Count)
        {
            return;
        }

        _index = next;
        Render();
    }

    private void Render()
    {
        _current?.Dispose();
        _stage.Controls.Clear();
        _current = null;
        _mute.Visible = false;
        _prev.Enabled = _index > 0;
        _next.Enabled = _index < _media.Count - 1;
        if (_media.Count == 0)
        {
            return;
        }

        var control = new MediaPreviewControl { Dock = DockStyle.Fill, BackColor = Color.Black };
        if (!control.LoadMedia(_media[_index]))
        {
            control.Dispose();
            return;
        }

        _current = control;
        _stage.Controls.Add(control);
        if (control.IsVideo)
        {
            control.SetSoundEnabled(true);
            control.RightClicked += (_, _) =>
            {
                control.SetSoundEnabled(false);
                UpdateMuteText();
            };
            _mute.Visible = true;
            UpdateMuteText();
        }
    }

    private void ToggleSound()
    {
        if (_current is { IsVideo: true })
        {
            _current.ToggleSound();
            UpdateMuteText();
        }
    }

    private void UpdateMuteText()
    {
        _mute.Text = _current?.SoundEnabled == true ? _texts.SoundOn : _texts.SoundOff;
    }
}
