using System;
using System.Windows.Forms;
using GtaSaModManager.Controls;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
	private Panel CreateWizardStep0()
	{
		var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
		var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
		var panel = new Panel
		{
			Name = "Step0Panel",
			BackColor = palette.Surface,
			BorderStyle = BorderStyle.FixedSingle,
			Padding = new Padding(18)
		};
		var title = new Label
		{
			Name = "Step0Title",
			Text = _localizationService.GetString("Guide", "Guide"),
			Font = new Font("Segoe UI", 18F, FontStyle.Bold),
			ForeColor = palette.TextPrimary,
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 12),
			Dock = DockStyle.Fill,
			TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
		};
		var accordion = new AccordionControl
		{
			Name = "Step0Accordion",
			Dock = DockStyle.Fill,
			RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
			Margin = new Padding(0)
		};
		accordion.ApplyPalette(palette);
		foreach (var section in new Step0ContentService().Load(_settings.Language))
		{
			accordion.AddSection(section.Title, section.Body);
		}

		var layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			Padding = new Padding(0),
			Margin = new Padding(0),
			BackColor = Color.Transparent
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		layout.Controls.Add(title, 0, 0);
		layout.Controls.Add(accordion, 0, 1);
		panel.Controls.Add(layout);
		return panel;
	}
}
