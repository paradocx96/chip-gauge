using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ChipGauge.App.ViewModels;

namespace ChipGauge.App;

/// <summary>
/// Maps a view-model instance to its view by naming convention
/// (<c>…ViewModels.FooViewModel</c> → <c>…Views.FooView</c>).
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "null" };
        }

        string name = param.GetType().FullName!
            .Replace("ViewModels", "Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        Type? type = Type.GetType(name);
        if (type is not null && Activator.CreateInstance(type) is Control control)
        {
            return control;
        }

        return new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is PageViewModel;
}
