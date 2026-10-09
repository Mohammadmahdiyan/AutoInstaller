using System;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Models;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    private Panel CreateWizardStep4()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        _step4ProgressView = new InstallProgressView();
        var layout = new TableLayoutPanel
        {
            Name = "Step4Layout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        var loadingOverlay = new Panel
        {
            Name = "Step4LoadingOverlay",
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(18),
            BorderStyle = BorderStyle.None
        };
        var loadingSpinner = new Label
        {
            Name = "Step4LoadingSpinner",
            Text = "⏳",
            AutoSize = true,
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            TextAlign = ContentAlignment.MiddleCenter
        };
        var loadingText = new Label
        {
            Name = "Step4LoadingText",
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = _localizationService.GetString("LoadingVehicles", "Loading vehicles...")
        };
        loadingSpinner.Anchor = AnchorStyles.None;
        loadingText.Anchor = AnchorStyles.None;
        loadingOverlay.Controls.Add(loadingText);
        loadingOverlay.Controls.Add(loadingSpinner);
        loadingOverlay.Resize += (_, _) =>
        {
            if (loadingOverlay.Width > 0)
            {
                loadingSpinner.Location = new Point((loadingOverlay.Width - loadingSpinner.Width) / 2, (loadingOverlay.Height - loadingSpinner.Height - loadingText.Height - 16) / 2);
                loadingText.Location = new Point((loadingOverlay.Width - loadingText.Width) / 2, loadingSpinner.Bottom + 12);
            }
        };
        var readmeButton = new Button
        {
            Name = "Step4ReadmeButton",
            Text = _localizationService.GetString("OpenReadmeFile", "Open README.txt"),
            AutoSize = true,
            Visible = false,
            Dock = DockStyle.Bottom,
            Height = 36,
            Margin = new Padding(0, 10, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        readmeButton.FlatAppearance.BorderSize = 0;
        readmeButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedReadmePath) && File.Exists(_selectedReadmePath))
            {
                ShowReadmeDialog(_selectedReadmePath);
            }
        };

        layout.Controls.Add(_step4ProgressView, 0, 0);
        layout.Controls.Add(readmeButton, 0, 1);
        panel.Controls.Add(layout);
        panel.Controls.Add(loadingOverlay);
        loadingOverlay.BringToFront();
        return panel;
    }

    private string GetStep4LoadingText(string? assetType)
    {
        var normalizedType = assetType?.Trim();
        return normalizedType switch
        {
            "Vehicle" => _localizationService.GetString("LoadingVehicles", "Loading vehicles..."),
            "Weapon" => _localizationService.GetString("LoadingWeapons", "Loading weapons..."),
            "Skin" => _localizationService.GetString("LoadingSkins", "Loading skins..."),
            _ => _localizationService.GetString("LoadingVehicles", "Loading vehicles...")
        };
    }

    private void ShowStep4LoadingOverlay(string? assetType)
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var overlay = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4LoadingOverlay");
        var loadingText = overlay?.Controls.OfType<Label>().FirstOrDefault(control => control.Name == "Step4LoadingText");
        var spinner = overlay?.Controls.OfType<Label>().FirstOrDefault(control => control.Name == "Step4LoadingSpinner");
        if (overlay == null || loadingText == null || spinner == null)
        {
            return;
        }

        loadingText.Text = GetStep4LoadingText(assetType);
        overlay.BackgroundImage = CaptureBlurredBackgroundForOverlay(panel);
        overlay.BackgroundImageLayout = ImageLayout.Stretch;
        overlay.BringToFront();
        overlay.Visible = true;
        overlay.Enabled = true;
        overlay.Refresh();
        spinner.Text = "⏳";
        spinner.Refresh();
        _loadingSpinnerTimer.Start();
    }

    private void HideStep4LoadingOverlay()
    {
        _loadingSpinnerTimer.Stop();
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var overlay = panel.Controls.OfType<Panel>().FirstOrDefault(control => control.Name == "Step4LoadingOverlay");
        if (overlay == null)
        {
            return;
        }

        overlay.Visible = false;
        overlay.Enabled = false;
        overlay.BackgroundImage = null;
    }

    private async Task ShowStep4LoadingTransitionAsync(string? assetType)
    {
        ShowStep4LoadingOverlay(assetType);
        await Task.Yield();
        HideStep4LoadingOverlay();
    }

    private void RefreshStep4Preview()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step4, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var readmeButton = panel.Controls.OfType<Button>().FirstOrDefault(control => control.Name == "Step4ReadmeButton");
        if (_step4ProgressView == null || _step4ProgressView.IsDisposed)
        {
            return;
        }

        if (readmeButton != null)
        {
            readmeButton.Visible = false;
            readmeButton.Enabled = false;
        }

        if (_lastStep4FileNames.Count > 0 && !_step4ProgressView.HasFileRows)
        {
            _step4ProgressView.ShowCompletedFiles(_lastStep4FileNames, _step4ProgressTitle);
        }
        panel.PerformLayout();
    }

    private Panel CreateStep4ImagePanel(List<string> imageFiles)
    {
        var basePanel = new Panel { Name = "Step4ImagePanel", BackColor = Color.FromArgb(255, 255, 255), Padding = new Padding(0) };
        if (imageFiles.Count == 0)
        {
            return basePanel;
        }

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        if (imageFiles.Count > 4)
        {
            var slideHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0), BackColor = Color.White };
            var slideView = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = Color.White };
            var prev = new Button { Text = "◀", Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Dock = DockStyle.Left };
            var next = new Button { Text = "▶", Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Cursor = Cursors.Hand, Dock = DockStyle.Right };
            var index = 0;

            void RenderCurrentSlide()
            {
                slideView.Controls.Clear();
                var preview = CreateImagePreviewCard(imageFiles[index], index, imageFiles);
                preview.Dock = DockStyle.Fill;
                slideView.Controls.Add(preview);
            }

            RenderCurrentSlide();
            prev.Click += (_, _) =>
            {
                index = (index - 1 + imageFiles.Count) % imageFiles.Count;
                RenderCurrentSlide();
            };
            next.Click += (_, _) =>
            {
                index = (index + 1) % imageFiles.Count;
                RenderCurrentSlide();
            };

            if (isRtl)
            {
                prev.Dock = DockStyle.Right;
                next.Dock = DockStyle.Left;
            }

            slideHost.Controls.Add(slideView);
            slideHost.Controls.Add(prev);
            slideHost.Controls.Add(next);
            prev.BringToFront();
            next.BringToFront();
            basePanel.Controls.Add(slideHost);
            return basePanel;
        }

        var grid = new FlowLayoutPanel
        {
            Name = "Step4ImageGrid",
            Dock = DockStyle.Fill,
            WrapContents = true,
            AutoScroll = false,
            FlowDirection = FlowDirection.LeftToRight,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
            Padding = new Padding(0),
            BackColor = Color.White
        };

        var columns = imageFiles.Count switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 4
        };

        foreach (var (imageFile, index) in imageFiles.Select((file, i) => (file, i)))
        {
            var card = CreateImagePreviewCard(imageFile, index, imageFiles);
            var width = imageFiles.Count == 1 ? Math.Max(220, basePanel.Width - 20) : (basePanel.Width - (columns - 1) * 8) / columns;
            card.Width = Math.Max(120, width);
            card.Height = imageFiles.Count switch
            {
                1 => Math.Max(220, basePanel.Height - 20),
                2 => Math.Max(120, (basePanel.Height - 12) / 1),
                _ => Math.Max(120, (basePanel.Height - 12) / Math.Max(1, (imageFiles.Count + columns - 1) / columns))
            };
            card.Margin = new Padding(4);
            card.Tag = imageFile;
            grid.Controls.Add(card);
        }

        basePanel.Controls.Add(grid);
        return basePanel;
    }

    private static void OpenImageViewerFromPictureBox(PictureBox pictureBox, List<string> imageFiles, string? fallbackTitle = null)
    {
        if (pictureBox == null || pictureBox.IsDisposed)
        {
            return;
        }

        var validPaths = imageFiles
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            return;
        }

        var currentIndex = Math.Clamp(Math.Max(0, imageFiles.FindIndex(path => string.Equals(path, pictureBox.Tag?.ToString(), StringComparison.OrdinalIgnoreCase))), 0, validPaths.Count - 1);
        if (pictureBox.Tag is string selectedPath && validPaths.Contains(selectedPath, StringComparer.OrdinalIgnoreCase))
        {
            currentIndex = validPaths.FindIndex(path => string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase));
        }

        var mainForm = pictureBox.FindForm();
        if (mainForm is MainForm parentForm)
        {
            parentForm.OpenFullImageViewer(validPaths, currentIndex, fallbackTitle ?? Path.GetFileName(validPaths[currentIndex]));
        }
    }

    private PictureBox CreateImagePreviewCard(string imagePath, int index, List<string>? imageList = null)
    {
        var items = imageList is { Count: > 0 } ? imageList : new List<string> { imagePath };
        var box = new PictureBox
        {
            Name = $"Step4Image_{index}",
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 247, 250),
            Cursor = Cursors.Hand,
            Margin = new Padding(4),
            Padding = new Padding(0),
            Dock = DockStyle.Fill,
            Image = TryLoadImage(imagePath),
            Tag = imagePath
        };

        void OpenFromRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            if (sender is Control source && !ReferenceEquals(source, box))
            {
                var parent = box.Parent ?? source;
                if (parent is not null)
                {
                    var mousePosition = parent.PointToClient(Control.MousePosition);
                    if (!box.Bounds.Contains(mousePosition))
                    {
                        return;
                    }
                }
            }

            OpenImageViewerFromPictureBox(box, items, imagePath);
        }

        box.Click += (_, _) => OpenImageViewerFromPictureBox(box, items, imagePath);
        box.MouseUp += OpenFromRightClick;

        if (box.Parent != null)
        {
            box.Parent.MouseUp += OpenFromRightClick;
        }

        return box;
    }

    private void OpenFullImageViewer(List<string> imagePaths, int selectedIndex, string? fallbackTitle = null)
    {
        var validPaths = imagePaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            return;
        }

        var currentIndex = Math.Clamp(selectedIndex, 0, validPaths.Count - 1);

        using var viewer = new Form
        {
            Text = fallbackTitle ?? Path.GetFileName(validPaths[currentIndex]),
            StartPosition = FormStartPosition.CenterParent,
            WindowState = FormWindowState.Normal,
            Width = 1100,
            Height = 760,
            MinimumSize = new Size(640, 420),
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = true,
            MinimizeBox = true,
            ShowIcon = false,
            BackColor = Color.Black,
            KeyPreview = true
        };

        var closeButton = new Button
        {
            Text = _localizationService.GetString("Close", "Close"),
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        var prevButton = new Button
        {
            Text = "◀",
            Width = 44,
            Height = 44,
            Visible = validPaths.Count > 1,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        var nextButton = new Button
        {
            Text = "▶",
            Width = 44,
            Height = 44,
            Visible = validPaths.Count > 1,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };

        var soundButton = new Button
        {
            Text = "🔇",
            Width = 44,
            Height = 44,
            Visible = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        soundButton.FlatAppearance.BorderSize = 0;
        var soundToolTip = new ToolTip();
        soundToolTip.SetToolTip(soundButton, "Toggle sound");

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Padding = new Padding(10) };
        Control? mediaView = null;

        void RenderCurrentImage()
        {
            var selectedPath = validPaths[currentIndex];
            viewer.Text = fallbackTitle ?? Path.GetFileName(selectedPath);
            if (mediaView != null)
            {
                content.Controls.Remove(mediaView);
                mediaView.Dispose();
                mediaView = null;
            }

            if (MediaPreviewControl.IsVideoPath(selectedPath))
            {
                var videoView = new MediaPreviewControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };
                if (videoView.LoadMedia(selectedPath))
                {
                    videoView.SetSoundEnabled(true);
                    mediaView = videoView;
                    videoView.SoundStateChanged += (_, _) =>
                    {
                        soundButton.Text = videoView.SoundEnabled ? "🔊" : "🔇";
                    };
                }
                else
                {
                    videoView.Dispose();
                }
            }
            else
            {
                var imageBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.Black,
                    Image = TryLoadImage(selectedPath),
                    Cursor = Cursors.Hand
                };
                imageBox.Click += (_, _) => viewer.Close();
                mediaView = imageBox;
            }

            if (mediaView != null)
            {
                content.Controls.Add(mediaView);
            }

            soundButton.Visible = mediaView is MediaPreviewControl;
            soundButton.Text = mediaView is MediaPreviewControl video && video.SoundEnabled ? "🔊" : "🔇";

            prevButton.Visible = validPaths.Count > 1;
            nextButton.Visible = validPaths.Count > 1;
        }

        closeButton.Click += (_, _) => viewer.Close();
        prevButton.Click += (_, _) =>
        {
            currentIndex = (currentIndex - 1 + validPaths.Count) % validPaths.Count;
            RenderCurrentImage();
        };
        nextButton.Click += (_, _) =>
        {
            currentIndex = (currentIndex + 1) % validPaths.Count;
            RenderCurrentImage();
        };
        soundButton.Click += (_, _) =>
        {
            if (mediaView is MediaPreviewControl video)
            {
                video.ToggleSound();
            }
        };
        viewer.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                viewer.Close();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.M && mediaView is MediaPreviewControl video)
            {
                video.ToggleSound();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(15, 23, 42),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        topBar.Controls.Add(closeButton);
        topBar.Controls.Add(soundButton);
        topBar.Controls.Add(nextButton);
        topBar.Controls.Add(prevButton);

        viewer.Controls.Add(content);
        viewer.Controls.Add(topBar);
        viewer.CancelButton = closeButton;
        topBar.BringToFront();
        RenderCurrentImage();
        viewer.ShowDialog(this);
    }

    private static Image? TryLoadImage(string path)
    {
        return TryLoadBitmap(path);
    }

    private static Bitmap? TryLoadBitmap(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var image = System.Drawing.Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            try
            {
                using var webpImage = SixLabors.ImageSharp.Image.Load(path);
                using var pngStream = new MemoryStream();
                webpImage.Save(pngStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                pngStream.Position = 0;
                using var converted = System.Drawing.Image.FromStream(pngStream);
                return new Bitmap(converted);
            }
            catch
            {
                return null;
            }
        }
    }


    private static Color GetAssetTypeColor(string assetType, ThemePalette? palette = null)
    {
        var resolvedPalette = palette ?? ThemeManager.ResolvePalette(AppTheme.LightBlue);
        return assetType.ToLowerInvariant() switch
        {
            "vehicle" => resolvedPalette.Accent,
            "skin" => resolvedPalette.Success,
            "weapon" => resolvedPalette.Error,
            _ => resolvedPalette.TextSecondary
        };
    }



    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step4/MainForm.Step4UI.cs
    // -------------------------------------------------------------------------
}
