namespace QuickTheme.UI;

internal static class UiDialogs
{
    public static TaskDialogButton Show(TaskDialogPage page) => TaskDialog.ShowDialog(page);

    public static TaskDialogButton Show(
        string heading,
        string text,
        TaskDialogIcon icon,
        params TaskDialogButton[] buttons)
    {
        var page = new TaskDialogPage
        {
            Caption = "Quick Theme",
            Heading = heading,
            Text = text,
            Icon = icon,
            AllowCancel = true,
        };

        foreach (TaskDialogButton button in buttons)
        {
            page.Buttons.Add(button);
        }

        return Show(page);
    }
}
