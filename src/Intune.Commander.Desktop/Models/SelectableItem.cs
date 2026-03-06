using CommunityToolkit.Mvvm.ComponentModel;

namespace Intune.Commander.Desktop.Models;

/// <summary>
/// Wraps any item with an IsSelected property for DataGrid checkbox multi-select.
/// </summary>
public partial class SelectableItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public object Item { get; }

    public SelectableItem(object item) => Item = item;
}
