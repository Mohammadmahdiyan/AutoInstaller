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
    private ContextMenuStrip? _videoMenu;
    private ToolStripMenuItem? _soundMenuItem;

    public string MediaPath { get; private set; } = string.Empty;

    public bool IsVideo => _mediaElement != null;

    public event EventHandler? RightClicked;

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
            _mediaElement = new WpfMediaElement
            {
                LoadedBehavior = WpfMediaState.Manual,
                UnloadedBehavior = WpfMediaState.Stop,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Volume = 0,
                IsMuted = true,
                Source = new Uri(path, UriKind.Absolute)
            };
            _mediaElement.MediaOpened += (_, _) => _mediaElement.Play();
            _mediaElement.MediaEnded += (_, _) =>
            {
                _mediaElement.Position = TimeSpan.Zero;
                _mediaElement.Play();
            };

            _videoMenu = new ContextMenuStrip();
            _soundMenuItem = new ToolStripMenuItem("Enable sound")
            {
                CheckOnClick = true
            };
            _soundMenuItem.CheckedChanged += (_, _) =>
            {
                if (_mediaElement == null)
                {
                    return;
                }

                _mediaElement.IsMuted = !_soundMenuItem.Checked;
                _mediaElement.Volume = _soundMenuItem.Checked ? 1 : 0;
                _soundMenuItem.Text = _soundMenuItem.Checked ? "Mute sound" : "Enable sound";
            };
            _videoMenu.Items.Add(_soundMenuItem);

            _mediaHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = _mediaElement,
                BackColor = BackColor,
                ContextMenuStrip = _videoMenu
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
        _mediaElement?.Stop();
        _mediaElement?.Close();
        _mediaElement = null;
        _mediaHost?.Dispose();
        _mediaHost = null;
        _videoMenu?.Dispose();
        _videoMenu = null;
        _soundMenuItem = null;

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
