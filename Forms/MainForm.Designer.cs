namespace GtaSaModManager.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.MainPanel = new System.Windows.Forms.Panel();
        this.HeaderPanel = new System.Windows.Forms.Panel();
        this.HeaderControlsPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.ThemeLabel = new System.Windows.Forms.Label();
        this.ThemeComboBox = new System.Windows.Forms.ComboBox();
        this.LanguageLabel = new System.Windows.Forms.Label();
        this.LanguageComboBox = new System.Windows.Forms.ComboBox();
        this.OpenGameFolderButton = new System.Windows.Forms.Button();
        this.RunGameButton = new System.Windows.Forms.Button();
        this.GamePanel = new System.Windows.Forms.Panel();
        this.GameStatusLabel = new System.Windows.Forms.Label();
        this.GameStatusValueLabel = new System.Windows.Forms.Label();
        this.GamePathLabel = new System.Windows.Forms.Label();
        this.GamePathTextBox = new System.Windows.Forms.TextBox();
        this.GamePathSelectButton = new System.Windows.Forms.Button();
        this.GameFolderNotConfiguredLabel = new System.Windows.Forms.Label();
        this.ModLibraryPanel = new System.Windows.Forms.Panel();
        this.ModLibraryTitleLabel = new System.Windows.Forms.Label();
        this.ModLibraryPathLabel = new System.Windows.Forms.Label();
        this.ModLibraryPathTextBox = new System.Windows.Forms.TextBox();
        this.ModLibraryChangeButton = new System.Windows.Forms.Button();
        this.ModLibraryOpenButton = new System.Windows.Forms.Button();
        this.ModLibraryListBox = new System.Windows.Forms.ListBox();
        this.ModLoaderPanel = new System.Windows.Forms.Panel();
        this.ModLoaderTitleLabel = new System.Windows.Forms.Label();
        this.InstallModButton = new System.Windows.Forms.Button();
        this.ModLoaderFlowPanel = new System.Windows.Forms.FlowLayoutPanel();

        this.MainPanel.SuspendLayout();
        this.HeaderPanel.SuspendLayout();
        this.HeaderControlsPanel.SuspendLayout();
        this.GamePanel.SuspendLayout();
        this.ModLibraryPanel.SuspendLayout();
        this.ModLoaderPanel.SuspendLayout();
        this.SuspendLayout();

        this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.MainPanel.Padding = new System.Windows.Forms.Padding(24);
        this.MainPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
        this.MainPanel.Controls.Add(this.HeaderPanel);
        this.MainPanel.Controls.Add(this.GamePanel);
        this.MainPanel.Controls.Add(this.ModLibraryPanel);
        this.MainPanel.Controls.Add(this.ModLoaderPanel);

        this.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.HeaderPanel.Height = 74;
        this.HeaderPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
        this.HeaderPanel.BackColor = System.Drawing.Color.Transparent;
        this.HeaderPanel.Controls.Add(this.HeaderControlsPanel);

        this.HeaderControlsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        this.HeaderControlsPanel.Dock = System.Windows.Forms.DockStyle.Right;
        this.HeaderControlsPanel.AutoSize = true;
        this.HeaderControlsPanel.WrapContents = false;
        this.HeaderControlsPanel.Controls.Add(this.RunGameButton);
        this.HeaderControlsPanel.Controls.Add(this.OpenGameFolderButton);
        this.HeaderControlsPanel.Controls.Add(this.LanguageComboBox);
        this.HeaderControlsPanel.Controls.Add(this.LanguageLabel);
        this.HeaderControlsPanel.Controls.Add(this.ThemeComboBox);
        this.HeaderControlsPanel.Controls.Add(this.ThemeLabel);

        this.ThemeLabel.AutoSize = true;
        this.ThemeLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.ThemeLabel.Text = "Theme";

        this.ThemeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.ThemeComboBox.Items.AddRange(new object[] { "System", "Light Blue", "Light Purple", "Light Green", "Light Orange", "Dark Blue", "Dark Purple", "Dark Green", "Dark Red" });
        this.ThemeComboBox.SelectedIndexChanged += new System.EventHandler(this.ThemeComboBox_SelectedIndexChanged);
        this.ThemeComboBox.Width = 150;

        this.LanguageLabel.AutoSize = true;
        this.LanguageLabel.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.LanguageLabel.Text = "Language";

        this.LanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.LanguageComboBox.Items.AddRange(new object[] { "English", "فارسی" });
        this.LanguageComboBox.SelectedIndexChanged += new System.EventHandler(this.LanguageComboBox_SelectedIndexChanged);
        this.LanguageComboBox.Width = 120;

        this.OpenGameFolderButton.Width = 150;
        this.OpenGameFolderButton.Text = "Open Game Folder";
        this.OpenGameFolderButton.Click += new System.EventHandler(this.OpenGameFolderButton_Click);

        this.RunGameButton.Width = 120;
        this.RunGameButton.Text = "Run Game";
        this.RunGameButton.Click += new System.EventHandler(this.RunGameButton_Click);

        this.GamePanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.GamePanel.Height = 170;
        this.GamePanel.Margin = new System.Windows.Forms.Padding(0, 12, 0, 12);
        this.GamePanel.Padding = new System.Windows.Forms.Padding(18);
        this.GamePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
        this.GamePanel.Controls.Add(this.GameStatusLabel);
        this.GamePanel.Controls.Add(this.GameStatusValueLabel);
        this.GamePanel.Controls.Add(this.GamePathLabel);
        this.GamePanel.Controls.Add(this.GamePathTextBox);
        this.GamePanel.Controls.Add(this.GamePathSelectButton);
        this.GamePanel.Controls.Add(this.GameFolderNotConfiguredLabel);

        this.GameStatusLabel.AutoSize = true;
        this.GameStatusLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.GameStatusLabel.Location = new System.Drawing.Point(18, 18);
        this.GameStatusLabel.Text = "Game Status";

        this.GameStatusValueLabel.AutoSize = true;
        this.GameStatusValueLabel.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.GameStatusValueLabel.Location = new System.Drawing.Point(146, 18);
        this.GameStatusValueLabel.Text = "Not configured";

        this.GamePathLabel.AutoSize = true;
        this.GamePathLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.GamePathLabel.Location = new System.Drawing.Point(18, 60);
        this.GamePathLabel.Text = "Game Path";

        this.GamePathTextBox.Location = new System.Drawing.Point(18, 86);
        this.GamePathTextBox.Width = 560;
        this.GamePathTextBox.ReadOnly = true;

        this.GamePathSelectButton.Location = new System.Drawing.Point(600, 84);
        this.GamePathSelectButton.Width = 120;
        this.GamePathSelectButton.Text = "Browse";
        this.GamePathSelectButton.Click += new System.EventHandler(this.GamePathSelectButton_Click);

        this.GameFolderNotConfiguredLabel.AutoSize = true;
        this.GameFolderNotConfiguredLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
        this.GameFolderNotConfiguredLabel.Location = new System.Drawing.Point(18, 145);
        this.GameFolderNotConfiguredLabel.Text = "Game folder not configured";
        this.GameFolderNotConfiguredLabel.ForeColor = System.Drawing.Color.DarkRed;

        this.ModLibraryPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.ModLibraryPanel.Height = 210;
        this.ModLibraryPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.ModLibraryPanel.Padding = new System.Windows.Forms.Padding(18);
        this.ModLibraryPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
        this.ModLibraryPanel.Controls.Add(this.ModLibraryTitleLabel);
        this.ModLibraryPanel.Controls.Add(this.ModLibraryPathLabel);
        this.ModLibraryPanel.Controls.Add(this.ModLibraryPathTextBox);
        this.ModLibraryPanel.Controls.Add(this.ModLibraryChangeButton);
        this.ModLibraryPanel.Controls.Add(this.ModLibraryOpenButton);
        this.ModLibraryPanel.Controls.Add(this.ModLibraryListBox);

        this.ModLibraryTitleLabel.AutoSize = true;
        this.ModLibraryTitleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
        this.ModLibraryTitleLabel.Location = new System.Drawing.Point(18, 12);
        this.ModLibraryTitleLabel.Text = "Mod Library";

        this.ModLibraryPathLabel.AutoSize = true;
        this.ModLibraryPathLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.ModLibraryPathLabel.Location = new System.Drawing.Point(18, 50);
        this.ModLibraryPathLabel.Text = "Mods Folder";

        this.ModLibraryPathTextBox.Location = new System.Drawing.Point(18, 74);
        this.ModLibraryPathTextBox.Width = 560;
        this.ModLibraryPathTextBox.ReadOnly = true;

        this.ModLibraryChangeButton.Location = new System.Drawing.Point(600, 72);
        this.ModLibraryChangeButton.Width = 110;
        this.ModLibraryChangeButton.Text = "Change";
        this.ModLibraryChangeButton.Click += new System.EventHandler(this.ModLibraryChangeButton_Click);

        this.ModLibraryOpenButton.Location = new System.Drawing.Point(720, 72);
        this.ModLibraryOpenButton.Width = 110;
        this.ModLibraryOpenButton.Text = "Open Folder";
        this.ModLibraryOpenButton.Click += new System.EventHandler(this.ModLibraryOpenButton_Click);

        this.ModLibraryListBox.Location = new System.Drawing.Point(18, 110);
        this.ModLibraryListBox.Width = 812;
        this.ModLibraryListBox.Height = 70;
        this.ModLibraryListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

        this.ModLoaderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ModLoaderPanel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 18);
        this.ModLoaderPanel.BackColor = System.Drawing.Color.Transparent;
        this.ModLoaderPanel.Controls.Add(this.ModLoaderTitleLabel);
        this.ModLoaderPanel.Controls.Add(this.InstallModButton);
        this.ModLoaderPanel.Controls.Add(this.ModLoaderFlowPanel);

        this.ModLoaderTitleLabel.AutoSize = true;
        this.ModLoaderTitleLabel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
        this.ModLoaderTitleLabel.Location = new System.Drawing.Point(18, 12);
        this.ModLoaderTitleLabel.Text = "ModLoader Mods";

        this.InstallModButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        this.InstallModButton.Location = new System.Drawing.Point(742, 8);
        this.InstallModButton.Width = 180;
        this.InstallModButton.Height = 42;
        this.InstallModButton.Text = "Install Mod";
        this.InstallModButton.Click += new System.EventHandler(this.InstallModButton_Click);

        this.ModLoaderFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.ModLoaderFlowPanel.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
        this.ModLoaderFlowPanel.AutoScroll = true;
        this.ModLoaderFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(980, 760);
        this.Controls.Add(this.MainPanel);
        this.Name = "MainForm";
        this.Text = "GTA San Andreas Mod Manager";

        this.MainPanel.ResumeLayout(false);
        this.HeaderPanel.ResumeLayout(false);
        this.HeaderPanel.PerformLayout();
        this.HeaderControlsPanel.ResumeLayout(false);
        this.GamePanel.ResumeLayout(false);
        this.GamePanel.PerformLayout();
        this.ModLibraryPanel.ResumeLayout(false);
        this.ModLibraryPanel.PerformLayout();
        this.ModLoaderPanel.ResumeLayout(false);
        this.ModLoaderPanel.PerformLayout();
        this.ResumeLayout(false);
    }

    private System.Windows.Forms.Panel MainPanel;
    private System.Windows.Forms.Panel HeaderPanel;
    private System.Windows.Forms.FlowLayoutPanel HeaderControlsPanel;
    private System.Windows.Forms.Label ThemeLabel;
    private System.Windows.Forms.ComboBox ThemeComboBox;
    private System.Windows.Forms.Label LanguageLabel;
    private System.Windows.Forms.ComboBox LanguageComboBox;
    private System.Windows.Forms.Button OpenGameFolderButton;
    private System.Windows.Forms.Button RunGameButton;
    private System.Windows.Forms.Panel GamePanel;
    private System.Windows.Forms.Label GameStatusLabel;
    private System.Windows.Forms.Label GameStatusValueLabel;
    private System.Windows.Forms.Label GamePathLabel;
    private System.Windows.Forms.TextBox GamePathTextBox;
    private System.Windows.Forms.Button GamePathSelectButton;
    private System.Windows.Forms.Label GameFolderNotConfiguredLabel;
    private System.Windows.Forms.Panel ModLibraryPanel;
    private System.Windows.Forms.Label ModLibraryTitleLabel;
    private System.Windows.Forms.Label ModLibraryPathLabel;
    private System.Windows.Forms.TextBox ModLibraryPathTextBox;
    private System.Windows.Forms.Button ModLibraryChangeButton;
    private System.Windows.Forms.Button ModLibraryOpenButton;
    private System.Windows.Forms.ListBox ModLibraryListBox;
    private System.Windows.Forms.Panel ModLoaderPanel;
    private System.Windows.Forms.Label ModLoaderTitleLabel;
    private System.Windows.Forms.Button InstallModButton;
    private System.Windows.Forms.FlowLayoutPanel ModLoaderFlowPanel;
}
