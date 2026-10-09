using System;
using System.Diagnostics;
using System.Windows.Forms;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا
    // -------------------------------------------------------------------------
    private Panel CreateWizardStep6()
    {
        var panel = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var title = new Label { Name = "CompletionTitle", Text = _localizationService.GetString("Completed", "Completed"), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true };
        var description = new Label { Name = "CompletionDescription", Text = _localizationService.GetString("InstallationComplete", "Installation complete."), AutoSize = true, Font = new Font("Segoe UI", 11F) };
        var countdown = new Label { Name = "CountdownLabel", AutoSize = true, Font = new Font("Segoe UI", 10F) };
        var openButton = new Button { Name = "Step6OpenGameFolderButton", Text = _localizationService.GetString("OpenGameFolder", "Open Game Folder"), Width = 180, Height = 42 };
        var openUserFilesButton = new Button
        {
            Name = "OpenUserFilesFolderButton",
            Text = _localizationService.GetString("OpenUserFilesFolder", "Open GTA San Andreas User Files"),
            Width = 230,
            Height = 42,
            Visible = false
        };
        var runButton = new Button { Name = "Step6RunGameButton", Text = _localizationService.GetString("RunGame", "Run Game"), Width = 150, Height = 42 };
        var installMoreButton = new Button { Name = "InstallMoreModsButton", Text = _localizationService.GetString("WannaInstallMoreMods", "Wanna install more mods?"), Width = 210, Height = 42 };
        var deleteSaveMissionButton = new Button
        {
            Name = "DeleteSaveMissionButton",
            Text = _localizationService.GetString("Delete", "Delete"),
            Width = 130,
            Height = 38,
            Visible = false
        };
        var deleteSomeSaveMissionButton = new Button
        {
            Name = "DeleteSomeSaveMissionButton",
            Text = _localizationService.GetString("DeleteSomeModels", "Delete Some"),
            Width = 160,
            Height = 38,
            Visible = false
        };
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

        openUserFilesButton.Click += (_, _) =>
        {
            var userFilesPath = GetGtaUserFilesDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = userFilesPath,
                UseShellExecute = true,
                Verb = "open"
            });
        };

        runButton.Click += (_, _) => GameService.LaunchGame(_selectedGamePath);
        deleteSaveMissionButton.Click += async (_, _) => await UninstallCurrentMod();
        deleteSomeSaveMissionButton.Click += async (_, _) => await DeleteSomeSaveMissionFilesAsync();
        installMoreButton.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                });
                _completionTimer.Stop();
                _completionTimerActive = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var optionalHost = new FlowLayoutPanel
        {
            Name = "OptionalInstallHost",
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Visible = false,
            BackColor = Color.Transparent
        };

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        var openActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        openActions.Controls.Add(openButton);
        openActions.Controls.Add(openUserFilesButton);

        var gameActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        gameActions.Controls.Add(runButton);
        gameActions.Controls.Add(installMoreButton);

        var deleteActions = new FlowLayoutPanel
        {
            Name = "SaveMissionDeleteActions",
            Dock = DockStyle.Fill,
            FlowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Visible = false
        };
        deleteActions.Controls.Add(deleteSaveMissionButton);
        deleteActions.Controls.Add(deleteSomeSaveMissionButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(description, 0, 1);
        layout.Controls.Add(openActions, 0, 2);
        layout.Controls.Add(gameActions, 0, 3);
        layout.Controls.Add(deleteActions, 0, 4);
        layout.Controls.Add(optionalHost, 0, 5);
        layout.Controls.Add(countdown, 0, 6);
        layout.Controls.Add(gallery, 0, 7);
        panel.Controls.Add(layout);
        panel.AutoScroll = false;
        return panel;
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
    // از اینجا برای فایل Forms/Step6/MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------
    private async Task UninstallCurrentMod()
    {
        if (string.IsNullOrWhiteSpace(_selectedModName) || string.IsNullOrWhiteSpace(_selectedGamePath))
        {
            return;
        }

        var type = _selectedModManifest?.NormalizedType ?? "putinmodloader";
        if (MessageBox.Show(
                string.Format(_localizationService.GetString("ConfirmUninstallMod", "Are you sure you want to uninstall '{0}'?"), _selectedModName),
                _appName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        var isReplacementInstall = type is "replacing" or "putandreplace" or "putandreplaces";
        var isModLoaderInstall = type is "putinmodloader" or "vehicleandskinandweapon" or "vehiclesandskinsandweapons"
            || _selectedAssetForInstall != null;
        GoToStep(WizardStep.Step4);
        bool success;
        try
        {
            success = isReplacementInstall
                ? await RestoreReplacementInstallationsWithProgressAsync(_selectedModName)
                : isModLoaderInstall
                    ? await UninstallInstalledFolderWithProgressAsync(
                        _selectedModName,
                        Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), _selectedModName),
                        _selectedGamePath,
                        "putinmodloader")
                    : await UninstallByModIdWithProgressAsync(_selectedModName, type, null, _selectedGamePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled.") + " " + ex.Message,
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!success)
        {
            MessageBox.Show(_localizationService.GetString("UninstallModFailed", "The selected mod could not be uninstalled."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _completionTimer.Stop();
        _completionTimerActive = false;
        RefreshModList();
        MessageBox.Show(_localizationService.GetString("UninstallCompleted", "The mod was uninstalled successfully."), _appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task DeleteSomeSaveMissionFilesAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedGamePath)
            || string.IsNullOrWhiteSpace(_selectedModName)
            || _selectedModManifest?.NormalizedType is not ("saveandmission" or "savesandmissions"))
        {
            return;
        }

        var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(_selectedGamePath);
        var entry = ModLoaderService.LoadInstallationManifest(manifestPath).Entries
            .LastOrDefault(item => string.Equals(item.ModId, _selectedModName, StringComparison.OrdinalIgnoreCase)
                && item.Type is "saveandmission" or "savesandmissions");
        var files = (entry?.InstalledFiles ?? new List<string>()).Where(File.Exists).ToList();
        if (files.Count == 0)
        {
            MessageBox.Show(
                _localizationService.GetString("NoInstalledSaveMissionFiles", "No installed save or mission files were found."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        using var dialog = new Form
        {
            Text = _localizationService.GetString("DeleteSomeModels", "Delete Some"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = isRtl,
            ClientSize = new Size(480, 420)
        };
        var list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var file in files)
        {
            list.Items.Add(file, false);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            Padding = new Padding(8),
            FlowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false
        };
        var deleteButton = new Button
        {
            Text = _localizationService.GetString("DeleteSelectedModels", "Delete Selected"),
            Width = 150,
            Height = 34,
            DialogResult = DialogResult.OK
        };
        var cancelButton = new Button
        {
            Text = _localizationService.GetString("Cancel", "Cancel"),
            Width = 100,
            Height = 34,
            DialogResult = DialogResult.Cancel
        };
        buttons.Controls.Add(deleteButton);
        buttons.Controls.Add(cancelButton);
        dialog.Controls.Add(list);
        dialog.Controls.Add(buttons);
        dialog.CancelButton = cancelButton;
        ThemeManager.ApplyTheme(dialog, ThemeManager.ParseTheme(_settings.Theme));
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var selectedFiles = list.CheckedItems.Cast<string>().ToList();
        if (selectedFiles.Count == 0)
        {
            return;
        }

        GoToStep(WizardStep.Step4);
        BeginStep4Progress(
            string.Format(_localizationService.GetString("DeletingMod", "Deleting {0}"), _selectedModName),
            selectedFiles.Select(path => (path, Path.GetFileName(path))).ToList());
        try
        {
            for (var index = 0; index < selectedFiles.Count; index++)
            {
                if (File.Exists(selectedFiles[index]))
                {
                    File.Delete(selectedFiles[index]);
                }

                AddCompletedStep4File(Path.GetFileName(selectedFiles[index]), index + 1, selectedFiles.Count);
                await Task.Yield();
            }

            ModLoaderService.RemoveSelectedUserFilesInstallationFiles(
                _selectedGamePath,
                _selectedModName,
                selectedFiles);
            CompleteStep4Progress(_localizationService.GetString("DeleteProgressComplete", "Deletion complete."));
            GoToStep(WizardStep.Step6);
        }
        catch
        {
            FailStep4Progress(_localizationService.GetString("DeleteProgressFailed", "Deletion failed."));
            throw;
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل Forms/Step6/MainForm.Step6Completion.cs
    // -------------------------------------------------------------------------
}
