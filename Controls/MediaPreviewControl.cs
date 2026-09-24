using System.Drawing.Imaging;
using System.Windows.Forms.Integration;
using SixLabors.ImageSharp.Formats.Png;
using WpfMediaElement = System.Windows.Controls.MediaElement;
using WpfMediaState = System.Windows.Controls.MediaState;

namespace GtaSaModManager.Controls;

public sealed class MediaPreviewControl : Panel
{
    private static readonly string[] VideoExtensions =
    {
        ".avi", ".m4v", ".mkv", ".mov", ".mp4", ".webm", ".wmv"
    };

    private PictureBox? _imageBox;
    private ElementHost? _mediaHost;
    private WpfMediaElement? _mediaElement;
    private bool _soundEnabled;

    public string MediaPath { get; private set; } = string.Empty;

    public bool IsVideo => _mediaElement != null;

    public bool SoundEnabled => _soundEnabled;

    public event EventHandler? RightClicked;
    public event EventHandler? SoundStateChanged;

    public MediaPreviewControl()
    {
        BackColor = Color.FromArgb(245, 247, 250);
        Margin = new Padding(0);
    }

    public bool LoadMedia(string path)
    {
        ClearMedia();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        MediaPath = path;
        if (IsVideoPath(path))
        {
            return LoadVideo(path);
        }

        return LoadImage(path);
    }

    private bool LoadVideo(string path)
    {
        try
        {
            var mediaElement = new WpfMediaElement
            {
                LoadedBehavior = WpfMediaState.Manual,
                UnloadedBehavior = WpfMediaState.Stop,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Volume = 0,
                IsMuted = true,
                Source = new Uri(path, UriKind.Absolute)
            };
            _mediaElement = mediaElement;
            mediaElement.MediaOpened += (_, _) =>
            {
                if (ReferenceEquals(_mediaElement, mediaElement))
                {
                    mediaElement.Play();
                }
            };
            mediaElement.MediaEnded += (_, _) =>
            {
                if (ReferenceEquals(_mediaElement, mediaElement))
                {
                    mediaElement.Position = TimeSpan.Zero;
                    mediaElement.Play();
                }
            };
            mediaElement.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount >= 2)
                {
                    ToggleSound();
                    e.Handled = true;
                }
            };

            _mediaHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = _mediaElement,
                BackColor = BackColor
            };
            mediaElement.MouseRightButtonUp += (_, e) =>
            {
                RightClicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            };
            Controls.Add(_mediaHost);
            _mediaElement.Play();
            return true;
        }
        catch
        {
            ClearMedia();
            return false;
        }
    }

    public void ToggleSound()
    {
        if (_mediaElement == null)
        {
            return;
        }

        SetSoundEnabled(!_soundEnabled);
    }

    public void SetSoundEnabled(bool enabled)
    {
        if (_mediaElement == null)
        {
            return;
        }

        _soundEnabled = enabled;
        _mediaElement.IsMuted = !enabled;
        _mediaElement.Volume = enabled ? 1 : 0;
        SoundStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool LoadImage(string path)
    {
        try
        {
            var image = TryLoadImage(path);
            if (image == null)
            {
                return false;
            }

            _imageBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BackColor,
                Image = image
            };
            _imageBox.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    RightClicked?.Invoke(this, EventArgs.Empty);
                }
            };
            Controls.Add(_imageBox);
            return true;
        }
        catch
        {
            ClearMedia();
            return false;
        }
    }

    public void ClearMedia()
    {
        var mediaElement = _mediaElement;
        _mediaElement = null;
        mediaElement?.Stop();
        mediaElement?.Close();
        _mediaHost?.Dispose();
        _mediaHost = null;
        _soundEnabled = false;

        if (_imageBox != null)
        {
            _imageBox.Image?.Dispose();
            _imageBox.Dispose();
            _imageBox = null;
        }

        Controls.Clear();
        MediaPath = string.Empty;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearMedia();
        }

        base.Dispose(disposing);
    }

    public static bool IsVideoPath(string path)
    {
        return VideoExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsSupportedMediaPath(string path)
    {
        var extension = Path.GetExtension(path);
        return IsVideoPath(path)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static Image? TryLoadImage(string path)
    {
        if (Path.GetExtension(path).Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            using var webpImage = SixLabors.ImageSharp.Image.Load(path);
            using var pngStream = new MemoryStream();
            webpImage.Save(pngStream, new PngEncoder());
            pngStream.Position = 0;
            using var decoded = Image.FromStream(pngStream);
            return new Bitmap(decoded);
        }

        return Image.FromFile(path);
    }
}
