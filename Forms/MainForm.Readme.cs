using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------
    private int CalculateReadmeViewportHeight(string content, int availableWidth, Font font, int minimumHeight, int maximumHeight)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return minimumHeight;
        }

        var textWidth = Math.Max(120, availableWidth - 24);
        var textSize = TextRenderer.MeasureText(content, font, new Size(textWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix);

        var computedHeight = textSize.Height + 20;
        if (computedHeight < minimumHeight)
        {
            return minimumHeight;
        }

        return Math.Min(Math.Max(computedHeight, minimumHeight), maximumHeight);
    }

    private TextBox CreateStep4ReadmeBox(string readmePath, int availableHeight = 220)
    {
        var box = new TextBox
        {
            Name = "Step4ReadmeBox",
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(15, 23, 42),
            WordWrap = true,
            RightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian ? RightToLeft.Yes : RightToLeft.No,
            Dock = DockStyle.Fill,
            Height = availableHeight,
            MinimumSize = new Size(0, 120)
        };

        try
        {
            var sanitized = SanitizeMarkdownReadme(File.ReadAllText(readmePath));
            box.Text = sanitized;
            var computedHeight = CalculateReadmeViewportHeight(box.Text, Math.Max(160, box.Width), box.Font, 120, Math.Max(180, availableHeight));
            box.Height = Math.Min(Math.Max(computedHeight, 120), Math.Max(180, availableHeight));
            if (!string.IsNullOrWhiteSpace(box.Text))
            {
                box.SelectionStart = 0;
                box.SelectionLength = 0;
                box.ScrollToCaret();
            }
        }
        catch
        {
            box.Text = _localizationService.GetString("ReadmeFallback", "README");
        }

        return box;
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------

    
    // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------
    private static string BuildProfileId(string gameFolder, string executable)
    {
        return System.Text.RegularExpressions.Regex.Replace(gameFolder.Trim(), "[\\/]+", "/") + "|" + executable;
    }

    private static string ResolveDirectoryName(string selectedPath)
    {
        var cleanName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return ModPackageService.NormalizeDisplayName(cleanName ?? "Mod");
    }

    private static bool IsReadmeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var normalized = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

        return normalized.Contains("readme", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetReadmePriority(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }

    private static string SanitizeMarkdownReadme(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var text = content.Replace("\r\n", "\n");
        text = Regex.Replace(text, @"```[\s\S]*?```", string.Empty, RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s{0,3}#{1,6}\s*", string.Empty, RegexOptions.Multiline);
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = Regex.Replace(text, @"\*(.+?)\*", "$1");
        text = Regex.Replace(text, @"_([^_]+)_", "$1");
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        text = Regex.Replace(text, @"^\s*[-*+]\s+", "- ", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s*\d+\.\s+", string.Empty, RegexOptions.Multiline);
        text = Regex.Replace(text, @"^>\s*", string.Empty, RegexOptions.Multiline);
        text = text.Replace("\t", "    ");
        return text.Trim();
    }

    private static string FindReadmeFile(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return string.Empty;
        }

        var candidateFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => IsReadmeFileName(Path.GetFileName(file)))
            .OrderBy(file => GetReadmePriority(file), Comparer<int>.Default)
            .ThenBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return candidateFiles.FirstOrDefault() ?? string.Empty;
    }

    private static List<string> FindImageFiles(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return new List<string>();
        }

        return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(MediaPreviewControl.IsSupportedMediaPath)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ShowReadmeDialog(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        using var form = new Form
        {
            Text = Path.GetFileName(path),
            Width = 760,
            Height = 520,
            StartPosition = FormStartPosition.CenterParent
        };

        var box = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Fill
        };

        box.Text = SanitizeMarkdownReadme(File.ReadAllText(path));
        form.Controls.Add(box);
        form.ShowDialog(this);
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------

       // -------------------------------------------------------------------------
    // از اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------
    private List<string> GetStep5SidebarImageFiles()
    {
        if (_selectedAssetForInstall == null)
        {
            return new List<string>();
        }

        var imagePath = _assetCatalogService.ResolveImagePath(_selectedAssetForInstall);
        return string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)
            ? new List<string>()
            : new List<string> { imagePath };
    }

    private string TryReadTextFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return string.Empty;
        }

        try
        {
            var content = File.ReadAllText(path);
            return SanitizeMarkdownReadme(content);
        }
        catch
        {
            return string.Empty;
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Readme.cs
    // -------------------------------------------------------------------------
}
