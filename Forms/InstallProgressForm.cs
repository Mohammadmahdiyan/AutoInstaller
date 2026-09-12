using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public class InstallProgressForm : Form
{
    private readonly LocalizationService _localizationService;
    private readonly Label _titleLabel;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Label _currentFileLabel;

    public InstallProgressForm(LocalizationService localizationService, string modName)
    {
        _localizationService = localizationService;

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        Width = 540;
        Height = 220;
        Text = localizationService.GetString("InstallingTitle", "Installing") + " " + modName;
        Padding = new Padding(18);

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
            Value = 0
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
            Text = ""
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

    public void UpdateProgress(string fileName)
    {
        _currentFileLabel.Text = fileName;
        var current = _progressBar.Value + 10;
        if (current > 100)
        {
            current = 100;
        }

        _progressBar.Value = current;
        _statusLabel.Text = _localizationService.GetString("Installing", "Installing") + " " + fileName;
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
