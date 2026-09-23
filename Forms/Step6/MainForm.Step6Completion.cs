using System;
using System.Diagnostics;
using System.Windows.Forms;
using GtaSaModManager.Services;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا
    // -------------------------------------------------------------------------
    private Panel CreateWizardStep6()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Text = _localizationService.GetString("Completed", "Completed"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Text = _localizationService.GetString("InstallationComplete", "Installation complete."), AutoSize = true, Font = new Font("Segoe UI", 11F) };
        var countdown = new Label { Name = "CountdownLabel", AutoSize = true, Font = new Font("Segoe UI", 10F) };
        var openButton = new Button { Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder"), Width = 180, Height = 42 };
        var runButton = new Button { Text = _localizationService.GetString("RunGame", "Run Game"), Width = 150, Height = 42 };
        var gallery = new FlowLayoutPanel
        {
            Name = "CompletionImageGallery",
            AutoScroll = true,
            WrapContents = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(255, 255, 255)
        };

        openButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_selectedGamePath) && Directory.Exists(_selectedGamePath))
            {
                Process.Start(new ProcessStartInfo { FileName = _selectedGamePath, UseShellExecute = true, Verb = "open" });
            }
        };

        runButton.Click += (_, _) => GameService.LaunchGame(_selectedGamePath);

        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(openButton);
        flow.Controls.Add(runButton);

        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Controls.Add(flow);
        panel.Controls.Add(countdown);
        panel.Controls.Add(gallery);
        title.Location = new Point(18, 18);
        description.Location = new Point(18, 58);
        flow.Location = new Point(18, 90);
        countdown.Location = new Point(18, 160);
        gallery.Location = new Point(18, 190);
        gallery.Size = new Size(panel.Width - 36, panel.Height - 220);
        panel.AutoScroll = true;

        var optionalHost = new FlowLayoutPanel
        {
            Name = "OptionalInstallHost",
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Visible = false,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(optionalHost);
        optionalHost.Location = new Point(18, 130);
        return panel;
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل Forms/Step6/MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------
    private async Task InstallOptionalPackageAsync(string packageRoot)
    {
        if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
        {
            return;
        }

        var payloadPath = ModPackageService.GetPayloadDirectory(packageRoot);
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            payloadPath = packageRoot;
        }

        _selectedModName = ResolveDirectoryName(packageRoot);
        _selectedModPayloadPath = payloadPath;
        _selectedModPackageRoot = packageRoot;
        _selectedModManifest = ModPackageService.ResolveManifest(packageRoot);
        _selectedAssetForInstall = null;

        await InstallSelectedModAsync();
    }

    private List<string> GetOptionalPackageRootsForCurrentInstall()
    {
        var roots = new List<string>();
        var candidateRoots = new List<string>
        {
            !string.IsNullOrWhiteSpace(_selectedModPackageRoot) && Directory.Exists(_selectedModPackageRoot)
                ? _selectedModPackageRoot
                : string.Empty,
            !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath)
                ? _selectedModPayloadPath
                : string.Empty,
            !string.IsNullOrWhiteSpace(_selectedGamePath) && Directory.Exists(_selectedGamePath)
                ? Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), _selectedModName)
                : string.Empty
        };

        foreach (var root in candidateRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var folderName in new[] { "optional", "optionals" })
            {
                var optionalRoot = Path.Combine(root, folderName);
                if (!Directory.Exists(optionalRoot))
                {
                    continue;
                }

                var childPackages = Directory.GetDirectories(optionalRoot, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => Directory.Exists(path))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (childPackages.Count > 0)
                {
                    roots.AddRange(childPackages);
                    continue;
                }

                roots.Add(optionalRoot);
            }
        }

        return roots
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RefreshStep6OptionalActions()
    {
        if (!_wizardPanels.TryGetValue(WizardStep.Step6, out var panel) || panel.IsDisposed)
        {
            return;
        }

        var host = panel.Controls.OfType<FlowLayoutPanel>().FirstOrDefault(x => x.Name == "OptionalInstallHost");
        if (host == null)
        {
            return;
        }

        host.Controls.Clear();
        host.Visible = false;

        var packages = GetOptionalPackageRootsForCurrentInstall();
        if (packages.Count == 0)
        {
            return;
        }

        var tooltip = new ToolTip();
        host.Visible = true;
        host.Width = 420;
        host.AutoSize = true;

        if (packages.Count == 1)
        {
            var packageRoot = packages[0];
            var button = new Button
            {
                Text = _localizationService.GetString("InstallOptionalMod", "Install optional mod"),
                AutoSize = true,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 0, 10)
            };
            var packageName = Path.GetFileName(packageRoot);
            tooltip.SetToolTip(button, string.IsNullOrWhiteSpace(packageName) ? packageRoot : packageName);
            button.Click += async (_, _) => await InstallOptionalPackageAsync(packageRoot);
            host.Controls.Add(button);
            return;
        }

        foreach (var packageRoot in packages)
        {
            var packageName = Path.GetFileName(packageRoot);
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.Transparent
            };

            var checkBox = new CheckBox
            {
                Text = packageName,
                AutoSize = true,
                Tag = packageRoot,
                Margin = new Padding(0, 0, 8, 0)
            };
            var installButton = new Button
            {
                Text = _localizationService.GetString("Install", "Install"),
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Tag = packageRoot
            };

            tooltip.SetToolTip(checkBox, packageRoot);
            installButton.Click += async (_, _) => await InstallOptionalPackageAsync((string)installButton.Tag!);
            row.Controls.Add(checkBox);
            row.Controls.Add(installButton);
            host.Controls.Add(row);
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step6/MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------
}
