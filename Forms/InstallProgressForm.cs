using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public class InstallProgressPanel : Panel
{
    private readonly LocalizationService _localizationService;
    private readonly Label _titleLabel;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Label _currentFileLabel;

    public InstallProgressPanel(LocalizationService localizationService, string modName)
    {
        _localizationService = localizationService;

        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.FromArgb(248, 250, 252);
        Dock = DockStyle.Fill;
        Padding = new Padding(16);
        Visible = true;

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Text = localizationService.GetString("Installing", "Installing") + " " + modName
        };

        _progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Continuous,
            Dock = DockStyle.Top,
            Height = 28,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Margin = new Padding(0, 12, 0, 0)
        };

        _statusLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 10, 0, 0),
            Text = localizationService.GetString("Installing", "Installing")
        };

        _currentFileLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Dock = DockStyle.Top,
            Margin = new Padding(0, 8, 0, 0),
            Text = string.Empty,
            MaximumSize = new Size(600, 0)
        };

        Controls.Add(_currentFileLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
        Controls.Add(_titleLabel);
    }

    public void SetStatus(string status)
    {
        _statusLabel.Text = status;
    }

    public void UpdateProgress(int percent, string fileName = "")
    {
        var safePercent = Math.Clamp(percent, 0, 100);
        _progressBar.Value = safePercent;

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            _currentFileLabel.Text = fileName;
            _statusLabel.Text = _localizationService.GetString("Installing", "Installing") + " " + fileName;
        }
        else
        {
            _statusLabel.Text = _localizationService.GetString("Installing", "Installing");
        }
    }

    public void Complete()
    {
        _progressBar.Value = 100;
        _statusLabel.Text = _localizationService.GetString("InstallationCompleted", "Installation completed successfully.");
        _currentFileLabel.Text = _localizationService.GetString("InstallationCompleted", "Installation completed successfully.");
    }

    public void Fail()
    {
        _statusLabel.Text = _localizationService.GetString("InstallationFailed", "The mod could not be installed.");
    }
}
