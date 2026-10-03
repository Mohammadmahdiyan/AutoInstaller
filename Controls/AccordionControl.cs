using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GtaSaModManager.UI;
using Svg;

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
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var header = new AccordionHeaderButton(
            title,
            isExpanded,
            RightToLeft == RightToLeft.Yes,
            Path.Combine(AppContext.BaseDirectory, "Assets", "Forms", "Step0", "arrow-right.svg"))
        {
            Height = HeaderHeight - 2,
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        if (_sections.Count > 0)
        {
            _sections[^1].Card.Margin = new Padding(0, 0, 0, 8);
        }

        var bodyPanel = new Panel
        {
            Padding = new Padding(8, 8, 8, 0),
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
            section.Header.SetRightToLeft(RightToLeft == RightToLeft.Yes);
            section.BodyPanel.RightToLeft = RightToLeft;
            section.BodyControl.RightToLeft = RightToLeft;
            ApplySectionPalette(section);
            RenderBody(section);
        }
    }

    private void ToggleSection(AccordionSection section)
    {
        var shouldExpand = !section.IsExpanded;
        foreach (var currentSection in _sections)
        {
            currentSection.IsExpanded = shouldExpand && ReferenceEquals(currentSection, section);
            currentSection.BodyPanel.Visible = currentSection.IsExpanded;
            currentSection.BodyControl.Visible = currentSection.IsExpanded;
            currentSection.Header.SetExpanded(currentSection.IsExpanded);
            ApplySectionPalette(currentSection);
            UpdateSectionLayout(currentSection);
        }

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
        section.BodyControl.Width = bodyWidth - section.BodyPanel.Padding.Horizontal;
        section.BodyControl.Height = 32767;
        var bodyHeight = MeasureRenderedBodyHeight(section.BodyControl);
        section.BodyPanel.SetBounds(0, HeaderHeight, width - 2, bodyHeight + section.BodyPanel.Padding.Vertical);
        section.BodyControl.Height = bodyHeight;
        section.BodyControl.Location = Point.Empty;
        section.Card.Height = section.IsExpanded
            ? HeaderHeight + section.BodyPanel.Height + 2
            : HeaderHeight;
    }

    private void ApplySectionPalette(AccordionSection section)
    {
        section.Card.BackColor = _palette.Card;
        section.Card.ForeColor = _palette.TextPrimary;
        section.Header.BackColor = section.IsExpanded ? _palette.AccentSoft : _palette.Card;
        section.Header.ForeColor = _palette.TextPrimary;
        section.Header.SetIconColor(_palette.TextPrimary);
        section.BodyPanel.BackColor = _palette.Surface;
        section.BodyControl.BackColor = _palette.Surface;
        section.BodyControl.ForeColor = _palette.TextPrimary;
    }

    private void RenderBody(AccordionSection section)
    {
        var richTextBox = section.BodyControl;
        richTextBox.Clear();
        richTextBox.RightToLeft = RightToLeft;
        var isRtl = RightToLeft == RightToLeft.Yes;
        var inCodeBlock = false;
        var lines = GetBodyLines(section.BodyText);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var lineEnding = index < lines.Length - 1 ? Environment.NewLine : string.Empty;
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                using var codeFont = new Font("Consolas", 9.5F);
                AppendStyledText(richTextBox, line + lineEnding, codeFont, _palette.TextPrimary, _palette.SurfaceSecondary, HorizontalAlignment.Left);
                continue;
            }

            var headingMatch = Regex.Match(line, @"^[ \t]*(#{1,3})[ \t]*(.*)$");
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Length;
                var size = level switch { 1 => 18F, 2 => 15F, _ => 13F };
                using var headingFont = new Font("Segoe UI", size, FontStyle.Bold);
                AppendStyledText(richTextBox, headingMatch.Groups[2].Value + lineEnding, headingFont, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                continue;
            }

            var text = line;
            if (text.StartsWith("- ", StringComparison.Ordinal) || text.StartsWith("* ", StringComparison.Ordinal))
            {
                AppendStyledText(richTextBox, "• ", richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                text = text.Substring(2);
            }

            AppendInlineMarkdown(richTextBox, text, isRtl);
            AppendStyledText(richTextBox, lineEnding, richTextBox.Font, _palette.TextPrimary, _palette.Surface, isRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left);
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

    private static int MeasureRenderedBodyHeight(RichTextBox bodyControl)
    {
        if (bodyControl.TextLength == 0)
        {
            return bodyControl.Font.Height;
        }

        bodyControl.CreateControl();
        var lastCharacterIndex = bodyControl.TextLength - 1;
        var lastCharacterPosition = bodyControl.GetPositionFromCharIndex(lastCharacterIndex);
        bodyControl.Select(lastCharacterIndex, 1);
        var lastLineHeight = bodyControl.SelectionFont?.Height ?? bodyControl.Font.Height;
        bodyControl.Select(0, 0);

        return Math.Max(lastLineHeight, lastCharacterPosition.Y + lastLineHeight);
    }

    private static string[] GetBodyLines(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineCount = lines.Length;
        while (lineCount > 0 && string.IsNullOrWhiteSpace(lines[lineCount - 1]))
        {
            lineCount--;
        }

        return lines.Take(lineCount).ToArray();
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
        public AccordionSection(string title, string bodyText, Panel card, AccordionHeaderButton header, Panel bodyPanel, RichTextBox bodyControl, bool isExpanded)
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
        public AccordionHeaderButton Header { get; }
        public Panel BodyPanel { get; }
        public RichTextBox BodyControl { get; }
        public bool IsExpanded { get; set; }
    }

    private sealed class AccordionHeaderButton : Button
    {
        private const int IconSize = 20;
        private readonly string _svgPath;
        private readonly string _title;
        private readonly System.Windows.Forms.Timer _rotationTimer;
        private Bitmap? _icon;
        private bool _isRightToLeft;
        private float _rotationStart;
        private float _rotationTarget;
        private int _animationElapsed;

        public AccordionHeaderButton(string title, bool isExpanded, bool isRightToLeft, string svgPath)
        {
            _title = title;
            _svgPath = svgPath;
            _isRightToLeft = isRightToLeft;
            _rotationStart = GetCollapsedAngle();
            _rotationTarget = isExpanded ? 90 : _rotationStart;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Text = string.Empty;
            AccessibleName = title;
            Padding = new Padding(10, 0, 10, 0);
            _rotationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _rotationTimer.Tick += AdvanceRotation;
        }

        public void SetRightToLeft(bool isRightToLeft)
        {
            if (_isRightToLeft == isRightToLeft)
            {
                return;
            }

            _isRightToLeft = isRightToLeft;
            SetExpanded(_rotationTarget == 90);
        }

        public void SetIconColor(Color color)
        {
            _icon?.Dispose();
            using var source = SvgDocument.Open(_svgPath).Draw(IconSize, IconSize);
            _icon = new Bitmap(source.Width, source.Height);
            for (var x = 0; x < source.Width; x++)
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var alpha = source.GetPixel(x, y).A;
                    _icon.SetPixel(x, y, Color.FromArgb(alpha, color));
                }
            }

            Invalidate();
        }

        public void SetExpanded(bool isExpanded)
        {
            _rotationStart = CurrentAngle;
            _rotationTarget = isExpanded ? 90 : GetCollapsedAngle();
            _animationElapsed = 0;
            _rotationTimer.Stop();
            _rotationTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var iconX = _isRightToLeft ? Padding.Left : Width - Padding.Right - IconSize;
            var textLeft = _isRightToLeft ? iconX + IconSize + 8 : Padding.Left;
            var textRight = _isRightToLeft ? Width - Padding.Right : iconX - 8;
            var textBounds = Rectangle.FromLTRB(textLeft, 0, Math.Max(textLeft, textRight), Height);
            var textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            textFlags |= _isRightToLeft ? TextFormatFlags.RightToLeft | TextFormatFlags.Right : TextFormatFlags.Left;
            TextRenderer.DrawText(graphics, _title, Font, textBounds, ForeColor, textFlags);

            if (_icon is null)
            {
                return;
            }

            var centerX = iconX + IconSize / 2F;
            var centerY = Height / 2F;
            var state = graphics.Save();
            graphics.TranslateTransform(centerX, centerY);
            graphics.RotateTransform(CurrentAngle);
            graphics.DrawImage(_icon, -IconSize / 2F, -IconSize / 2F, IconSize, IconSize);
            graphics.Restore(state);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rotationTimer.Dispose();
                _icon?.Dispose();
            }

            base.Dispose(disposing);
        }

        private float CurrentAngle => _rotationStart + (_rotationTarget - _rotationStart) * AnimationProgress;

        private float AnimationProgress
        {
            get
            {
                var progress = Math.Min(1F, _animationElapsed / 220F);
                return (1F - MathF.Cos(MathF.PI * progress)) / 2F;
            }
        }

        private float GetCollapsedAngle() => _isRightToLeft ? 180 : 0;

        private void AdvanceRotation(object? sender, EventArgs e)
        {
            _animationElapsed += _rotationTimer.Interval;
            if (_animationElapsed >= 220)
            {
                _animationElapsed = 220;
                _rotationTimer.Stop();
            }

            Invalidate();
        }
    }
}
