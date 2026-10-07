namespace GtaSaModManager.Controls.Optionals;

public enum ExistingOptionalAction
{
    Cancel,
    Reinstall,
    DeleteBaseMod,
    DeleteOptional,
    DeleteBaseModAndOptional,
    DeleteAllOptionals,
    DeleteSomeOptionals,
    DeleteBaseModAndAllOptionals,
    DeleteBaseModAndSomeOptionals
}

/// <summary>
/// Shown when a mod that has an optional\ folder is selected again while it is already installed.
/// </summary>
public sealed class ExistingOptionalActionDialog : Form
{
    public ExistingOptionalAction Choice { get; private set; } = ExistingOptionalAction.Cancel;

    public ExistingOptionalActionDialog(
        string title,
        string message,
        string reinstallText,
        string deleteText,
        string cancelText,
        string deleteOptionalText,
        string deleteBothText,
        bool hasOptionalFolder,
        bool optionalInstalled,
        bool hasOptionalsFolder,
        bool optionalsInstalled,
        string deleteAllOptionalsText,
        string deleteSomeOptionalsText,
        string deleteBaseAndAllOptionalsText,
        string deleteBaseAndSomeOptionalsText,
        bool isRightToLeft)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 0);
        RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = isRightToLeft;
        Font = new Font("Segoe UI", 9.5F);
        Padding = new Padding(16);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.Controls.Add(new Label
        {
            Text = message,
            AutoSize = false,
            Width = 388,
            Height = 64,
            Margin = new Padding(0, 0, 0, 10)
        });

        AddButton(layout, reinstallText, ExistingOptionalAction.Reinstall, true);
        AddButton(layout, deleteText, ExistingOptionalAction.DeleteBaseMod, true);
        if (hasOptionalFolder)
        {
            AddButton(layout, deleteOptionalText, ExistingOptionalAction.DeleteOptional, optionalInstalled);
            AddButton(layout, deleteBothText, ExistingOptionalAction.DeleteBaseModAndOptional, true);
        }

        if (hasOptionalsFolder)
        {
            AddButton(layout, deleteAllOptionalsText, ExistingOptionalAction.DeleteAllOptionals, optionalsInstalled);
            AddButton(layout, deleteSomeOptionalsText, ExistingOptionalAction.DeleteSomeOptionals, optionalsInstalled);
            AddButton(layout, deleteBaseAndAllOptionalsText, ExistingOptionalAction.DeleteBaseModAndAllOptionals, true);
            AddButton(layout, deleteBaseAndSomeOptionalsText, ExistingOptionalAction.DeleteBaseModAndSomeOptionals, optionalsInstalled);
        }

        AddButton(layout, cancelText, ExistingOptionalAction.Cancel, true);

        Controls.Add(layout);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = layout.Controls.OfType<Button>().Last();
    }

    private void AddButton(FlowLayoutPanel layout, string text, ExistingOptionalAction action, bool enabled)
    {
        var button = new Button
        {
            Text = text,
            Width = 388,
            Height = 36,
            Margin = new Padding(0, 0, 0, 6),
            Enabled = enabled,
            DialogResult = DialogResult.OK
        };
        button.Click += (_, _) => Choice = action;
        layout.Controls.Add(button);
    }
}
