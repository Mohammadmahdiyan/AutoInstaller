using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GtaSaModManager.UI;

namespace GtaSaModManager.Controls;

public sealed class AccordionControl : UserControl
{
    private const int HeaderHeight = 40;
    private readonly FlowLayoutPanel _sectionsHost;
    private readonly List<AccordionSection> _sections = new();
    private ThemePalette _palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(null));

    public AccordionControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = _palette.Surface;
        _sectionsHost = new FlowLayoutPanel
        {
            Name = "AccordionSectionsHost",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4),
            Margin = new Padding(0),
            BackColor = _palette.Surface
        };
        Controls.Add(_sectionsHost);
        _sectionsHost.SizeChanged += (_, _) => UpdateSectionWidths();
    }

    public void AddSection(string title, string body)
    {
        var isExpanded = _sections.Count == 0;
        var card = new Panel
        {
            Height = HeaderHeight,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0)
        };
        var header = new Button
        {
            Height = HeaderHeight - 2,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            TextAlign = RightToLeft == RightToLeft.Yes ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        header.FlatAppearance.BorderSize = 0;
        var bodyPanel = new Panel
        {
            Padding = new Padding(8),
            Margin = new Padding(0),
            Visible = isExpanded
        };
        var bodyControl = new RichTextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = true,
            DetectUrls = false,
            TabStop = false,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0),
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            RightToLeft = RightToLeft,
            Visible = isExpanded
        };
        bodyPanel.Controls.Add(bodyControl);
        var section = new AccordionSection(title, body, card, header, bodyPanel, bodyControl, isExpanded);
        header.Click += (_, _) => ToggleSection(section);
        card.Controls.Add(bodyPanel);
        card.Controls.Add(header);
        _sections.Add(section);
        _sectionsHost.Controls.Add(card);
        ApplySectionPalette(section);
        RenderBody(section);
        UpdateSectionLayout(section);
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        _sectionsHost.BackColor = palette.Surface;
        foreach (var section in _sections)
        {
            ApplySectionPalette(section);
            RenderBody(section);
            UpdateSectionLayout(section);
        }
    }

    protected override void OnRightToLeftChanged(EventArgs e)
    {
        base.OnRightToLeftChanged(e);
        foreach (var section in _sections)
        {
            section.Header.TextAlign = RightToLeft == RightToLeft.Yes
                ? ContentAlignment.MiddleRight
                : ContentAlignment.MiddleLeft;
            section.BodyPanel.RightToLeft = RightToLeft;
            section.BodyControl.RightToLeft = RightToLeft;
            ApplySectionPalette(section);
            RenderBody(section);
        }
    }

    private void ToggleSection(AccordionSection section)
    {
        section.IsExpanded = !section.IsExpanded;
        section.BodyPanel.Visible = section.IsExpanded;
        section.BodyControl.Visible = section.IsExpanded;
        ApplySectionPalette(section);
        UpdateSectionLayout(section);
        _sectionsHost.PerformLayout();
    }

    private void UpdateSectionWidths()
    {
        foreach (var section in _sections)
        {
            UpdateSectionLayout(section);
        }
    }

    private void UpdateSectionLayout(AccordionSection section)
    {
        var width = Math.Max(180, _sectionsHost.ClientSize.Width - _sectionsHost.Padding.Horizontal);
        section.Card.Width = width;
        section.Header.SetBounds(0, 0, width - 2, HeaderHeight - 2);

        var bodyWidth = Math.Max(120, width - 20);
        var bodyHeight = MeasureBodyHeight(section.BodyText, bodyWidth - section.BodyPanel.Padding.Horizontal);
        section.BodyPanel.SetBounds(0, HeaderHeight, width - 2, bodyHeight + section.BodyPanel.Padding.Vertical);
        section.BodyControl.Width = bodyWidth - section.BodyPanel.Padding.Horizontal;
        section.BodyControl.Height = bodyHeight;
        section.BodyControl.Location = Point.Empty;
        section.Card.Height = section.IsExpanded
            ? HeaderHeight + section.BodyPanel.Height + 2
            : HeaderHeight;
        section.Header.Text = GetHeaderText(section);
    }

    private void ApplySectionPalette(AccordionSection section)
    {
        section.Card.BackColor = _palette.Card;
        section.Card.ForeColor = _palette.TextPrimary;
        section.Header.BackColor = section.IsExpanded ? _palette.AccentSoft : _palette.Card;
        section.Header.ForeColor = _palette.TextPrimary;
        section.BodyPanel.BackColor = _palette.Surface;
        section.BodyControl.BackColor = _palette.Surface;
        section.BodyControl.ForeColor = _palette.TextPrimary;
    }

    private string GetHeaderText(AccordionSection section)
    {
        var arrow = section.IsExpanded ? "▼" : "▶";
        return RightToLeft == RightToLeft.Yes
            ? section.Title + "  " + arrow
            : arrow + "  " + section.Title;
    }

    private void RenderBody(AccordionSection section)
    {
        var richTextBox = section.BodyControl;
        richTextBox.Clear();
        richTextBox.RightToLeft = RightToLeft;
        var isRtl = RightToLeft == RightToLeft.Yes;
        var inCodeBlock = false;
        var lines = section.BodyText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                using var codeFont = new Font("Consolas", 9.5F);
                AppendStyledText(richTextBox, line + Environment.NewLine, codeFont, _palette.TextPrimary, _palette.SurfaceSecondary, HorizontalAlignment.Left);
                continue;
            }

            var headingMatch = Regex.Match(line, @"^[ \t]*(#{1,3})[ \t]*(.*)$");
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Length;
                var size = level switch { 1 => 18F, 2 => 15F, _ => 13F };
                using var headingFont = new Font("Segoe UI", size, FontStyle.Bold);
                AppendStyledText(richTextBox, headingMatch.Groups[2].Value + Environment.NewLine, headingFont, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                continue;
            }

            var text = line;
            if (text.StartsWith("- ", StringComparison.Ordinal) || text.StartsWith("* ", StringComparison.Ordinal))
            {
                AppendStyledText(richTextBox, "• ", richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                text = text.Substring(2);
            }

            AppendInlineMarkdown(richTextBox, text, isRtl);
            AppendStyledText(richTextBox, Environment.NewLine, richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
        }

        richTextBox.SelectionStart = 0;
        richTextBox.SelectionLength = 0;
    }

    private void AppendInlineMarkdown(RichTextBox richTextBox, string text, bool isRtl)
    {
        var matches = Regex.Matches(text, @"\*\*.+?\*\*|`[^`]+`");
        var position = 0;
        foreach (Match match in matches)
        {
            if (match.Index > position)
            {
                AppendStyledText(richTextBox, text.Substring(position, match.Index - position), richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            }

            if (match.Value.StartsWith("**", StringComparison.Ordinal))
            {
                using var boldFont = new Font(richTextBox.Font, FontStyle.Bold);
                AppendStyledText(richTextBox, match.Value.Substring(2, match.Value.Length - 4), boldFont, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            }
            else
            {
                using var codeFont = new Font("Consolas", richTextBox.Font.Size);
                AppendStyledText(richTextBox, match.Value.Substring(1, match.Value.Length - 2), codeFont, _palette.TextPrimary, _palette.SurfaceSecondary, HorizontalAlignment.Left);
            }

            position = match.Index + match.Length;
        }

        if (position < text.Length)
        {
            AppendStyledText(richTextBox, text.Substring(position), richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
        }
    }

    private int MeasureBodyHeight(string body, int width)
    {
        var totalHeight = 8;
        var inCodeBlock = false;
        var isRtl = RightToLeft == RightToLeft.Yes;
        foreach (var line in body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            var heading = Regex.Match(line, @"^[ \t]*(#{1,3})[ \t]*(.*)$");
            var fontFamily = inCodeBlock ? "Consolas" : "Segoe UI";
            var fontSize = inCodeBlock ? 9.5F : heading.Success ? heading.Groups[1].Length switch { 1 => 18F, 2 => 15F, _ => 13F } : 10F;
            using var font = new Font(fontFamily, fontSize, heading.Success && !inCodeBlock ? FontStyle.Bold : FontStyle.Regular);
            var measureText = heading.Success && !inCodeBlock ? heading.Groups[2].Value : line;
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;
            if (isRtl && !inCodeBlock)
            {
                flags |= TextFormatFlags.RightToLeft;
            }

            var measured = TextRenderer.MeasureText(string.IsNullOrEmpty(measureText) ? " " : measureText, font, new Size(Math.Max(100, width), int.MaxValue), flags);
            totalHeight += Math.Max(font.Height, measured.Height) + 2;
        }

        return totalHeight + 8;
    }

    private static void AppendStyledText(RichTextBox richTextBox, string text, Font font, Color foreColor, Color backColor, HorizontalAlignment alignment)
    {
        richTextBox.SelectionStart = richTextBox.TextLength;
        richTextBox.SelectionLength = 0;
        richTextBox.SelectionFont = font;
        richTextBox.SelectionColor = foreColor;
        richTextBox.SelectionBackColor = backColor;
        richTextBox.SelectionAlignment = alignment;
        richTextBox.SelectedText = text;
    }

    private sealed class AccordionSection
    {
        public AccordionSection(string title, string bodyText, Panel card, Button header, Panel bodyPanel, RichTextBox bodyControl, bool isExpanded)
        {
            Title = title;
            BodyText = bodyText;
            Card = card;
            Header = header;
            BodyPanel = bodyPanel;
            BodyControl = bodyControl;
            IsExpanded = isExpanded;
        }

        public string Title { get; }
        public string BodyText { get; }
        public Panel Card { get; }
        public Button Header { get; }
        public Panel BodyPanel { get; }
        public RichTextBox BodyControl { get; }
        public bool IsExpanded { get; set; }
    }
}
