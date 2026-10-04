using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Ffvi.SaveTool.App.Views;

public enum DialogResult { Cancel, Ok, Yes, No }

public sealed class MessageDialog : Window
{
    private MessageDialog(string title, string message, params DialogResult[] buttons)
    {
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var b in buttons)
        {
            var btn = new Button { Content = b.ToString(), MinWidth = 72, HorizontalContentAlignment = HorizontalAlignment.Center };
            btn.Click += (_, _) => Close(b);
            row.Children.Add(btn);
        }
        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, row },
        };
    }

    public static Task<DialogResult> ShowOk(Window owner, string title, string message) =>
        new MessageDialog(title, message, DialogResult.Ok).ShowDialog<DialogResult>(owner);

    public static Task<DialogResult> ShowYesNoCancel(Window owner, string title, string message) =>
        new MessageDialog(title, message, DialogResult.Yes, DialogResult.No, DialogResult.Cancel).ShowDialog<DialogResult>(owner);
}
