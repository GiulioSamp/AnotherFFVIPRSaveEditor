using Avalonia.Controls;
using Ffvi.SaveTool.App.ViewModels;

namespace Ffvi.SaveTool.App.Views;

public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || DataContext is not MainViewModel vm || !vm.IsDirty) return;
        e.Cancel = true;
        _ = ConfirmAndCloseAsync(vm);
    }

    private async Task ConfirmAndCloseAsync(MainViewModel vm)
    {
        if (!await vm.ConfirmDiscardAsync()) return;
        _closeConfirmed = true;
        Close();
    }
}
