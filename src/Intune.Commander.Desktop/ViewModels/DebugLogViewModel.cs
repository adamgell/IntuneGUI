using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Desktop.Services;

namespace Intune.Commander.Desktop.ViewModels;

public partial class DebugLogViewModel : ObservableObject
{
    public ObservableCollection<string> LogEntries => DebugLogService.Instance.Entries;

    [RelayCommand]
    private void ClearLog()
    {
        DebugLogService.Instance.Clear();
    }
}
