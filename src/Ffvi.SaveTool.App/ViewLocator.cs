using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Ffvi.SaveTool.App.ViewModels;

namespace Ffvi.SaveTool.App;

public class ViewLocator : IDataTemplate
{
    public bool Match(object? data) => data is TabViewModel;

    public Control Build(object? param)
    {
        var name = param!.GetType().FullName!.Replace("ViewModels", "Views").Replace("ViewModel", "View");
        var type = Type.GetType(name);
        return type is null ? new TextBlock { Text = "View not found: " + name } : (Control)Activator.CreateInstance(type)!;
    }
}
