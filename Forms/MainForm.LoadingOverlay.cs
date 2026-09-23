using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا
    // -------------------------------------------------------------------------
    private void EnsureGlobalLoadingOverlay()
    {
        if (_globalLoadingOverlay != null && !_globalLoadingOverlay.IsDisposed)
        {
            return;
        }

        _globalLoadingOverlay = new Panel
        {
            Name = "GlobalLoadingOverlay",
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Color.FromArgb(245, 247, 250),
            BorderStyle = BorderStyle.None,
            Enabled = false
        };

        _globalLoadingLabel = new Label
        {
            Name = "GlobalLoadingLabel",
            AutoSize = true,
            Text = "Loading...",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _globalLoadingOverlay.Controls.Add(_globalLoadingLabel);
        _globalLoadingOverlay.Resize += (_, _) =>
        {
            if (_globalLoadingLabel != null && _globalLoadingOverlay != null)
            {
                _globalLoadingLabel.Location = new Point(
                    (_globalLoadingOverlay.Width - _globalLoadingLabel.Width) / 2,
                    (_globalLoadingOverlay.Height - _globalLoadingLabel.Height) / 2);
            }
        };

        Controls.Add(_globalLoadingOverlay);
        _globalLoadingOverlay.BringToFront();
    }

    private void ShowGlobalLoadingOverlay(string message)
    {
        EnsureGlobalLoadingOverlay();
        if (_globalLoadingLabel != null)
        {
            _globalLoadingLabel.Text = message;
        }

        if (_globalLoadingOverlay != null)
        {
            var snapshot = CaptureBlurredBackgroundForOverlay(this);
            _globalLoadingOverlay.BackgroundImage = snapshot;
            _globalLoadingOverlay.BackgroundImageLayout = ImageLayout.Stretch;
            _globalLoadingOverlay.Visible = true;
            _globalLoadingOverlay.Enabled = true;
            _globalLoadingOverlay.BringToFront();
            _globalLoadingOverlay.Refresh();
            _loadingSpinnerTimer.Start();
        }
    }

    private void HideGlobalLoadingOverlay()
    {
        _loadingSpinnerTimer.Stop();
        if (_globalLoadingOverlay != null)
        {
            _globalLoadingOverlay.Visible = false;
            _globalLoadingOverlay.Enabled = false;
            _globalLoadingOverlay.BackgroundImage = null;
        }
    }

    private static IEnumerable<Label> GetVisibleLoadingSpinners()
    {
        foreach (var control in Application.OpenForms.Cast<Form>().SelectMany(form => form.Controls.Cast<Control>()))
        {
            if (control is Label label && label.Name is "Step4LoadingSpinner")
            {
                yield return label;
            }
        }
    }

    private static string GetLoadingSpinnerGlyph(float angle)
    {
        var frames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var index = (int)((angle / 36f) % frames.Length);
        return frames[index];
    }

    private static Bitmap? CaptureBlurredBackgroundForOverlay(Control target)
    {
        if (target == null || target.IsDisposed || target.Width <= 0 || target.Height <= 0)
        {
            return null;
        }

        try
        {
            var image = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(image);
            g.CopyFromScreen(target.PointToScreen(new Point(0, 0)), new Point(0, 0), target.Size);

            var blurred = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using var blurredGraphics = Graphics.FromImage(blurred);
            var preview = new Rectangle(0, 0, image.Width, image.Height);
            var attrs = new ImageAttributes();
            var matrix = new ColorMatrix(new[]
            {
                new[] { 0.7f, 0, 0, 0, 0 },
                new[] { 0, 0.7f, 0, 0, 0 },
                new[] { 0, 0, 0.7f, 0, 0 },
                new[] { 0, 0, 0, 1f, 0 },
                new[] { 0, 0, 0, 0, 1f }
            });
            attrs.SetColorMatrix(matrix);
            blurredGraphics.DrawImage(image, preview, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
            return blurred;
        }
        catch
        {
            return null;
        }
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
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

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.LoadingOverlay.cs
    // -------------------------------------------------------------------------
}
