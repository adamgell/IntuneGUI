using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Intune.Commander.Desktop.ViewModels;

namespace Intune.Commander.Desktop.Views;

public partial class AssignmentReportWindow : Window
{
    private DataGrid? _grid;

    public AssignmentReportWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _grid = this.FindControl<DataGrid>("ResultsGrid");

        if (DataContext is AssignmentReportViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            UpdateColumnVisibility(vm);
        }

        LoadWindowPlacement();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        SaveWindowPlacement();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssignmentReportViewModel.SelectedModeIndex)
            && sender is AssignmentReportViewModel vm)
        {
            UpdateColumnVisibility(vm);
        }
    }

    private void UpdateColumnVisibility(AssignmentReportViewModel vm)
    {
        if (_grid == null) return;

        // Map column header text to whether it should be visible for this mode.
        var visibility = new Dictionary<string, bool>
        {
            ["Assignments"]        = vm.ShowAssignmentSummary,
            ["Assignment Reason"]  = vm.ShowAssignmentReason,
            ["Empty Group"]        = vm.ShowGroupColumns,
            ["Group ID"]           = vm.ShowGroupColumns,
            ["Group 1 Status"]     = vm.ShowCompareColumns,
            ["Group 2 Status"]     = vm.ShowCompareColumns,
            ["Device"]             = vm.ShowFailureColumns,
            ["Status"]             = vm.ShowFailureColumns,
            ["User"]               = vm.ShowFailureColumns || vm.SelectedModeIndex == 0,
            ["Last Reported"]      = vm.ShowFailureColumns,
        };

        foreach (var col in _grid.Columns)
        {
            var header = col.Header?.ToString() ?? "";
            if (visibility.TryGetValue(header, out var isVisible))
                col.IsVisible = isVisible;
        }
    }

    // ── Keyboard shortcuts ───────────────────────────────────────────────────────

    private void OnUserInputBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ExecuteRunReport();
    }

    private void OnDeviceInputBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ExecuteRunReport();
    }

    private void OnGroupSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not AssignmentReportViewModel vm) return;
        if (vm.SearchGroupCommand.CanExecute(null))
            vm.SearchGroupCommand.Execute(null);
    }

    private void OnCompare1BoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not AssignmentReportViewModel vm) return;
        if (vm.SearchCompareGroup1Command.CanExecute(null))
            vm.SearchCompareGroup1Command.Execute(null);
    }

    private void OnCompare2BoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not AssignmentReportViewModel vm) return;
        if (vm.SearchCompareGroup2Command.CanExecute(null))
            vm.SearchCompareGroup2Command.Execute(null);
    }

    private void ExecuteRunReport()
    {
        if (DataContext is AssignmentReportViewModel vm && vm.RunReportCommand.CanExecute(null))
            vm.RunReportCommand.Execute(null);
    }

    // ── Window placement persistence ─────────────────────────────────────────────

    private static readonly string PlacementFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Intune.Commander",
        "assignment-report-window.json");

    private void SaveWindowPlacement()
    {
        try
        {
            var pos = Position;
            var size = ClientSize;
            var csv = $"{pos.X},{pos.Y},{(int)size.Width},{(int)size.Height}";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PlacementFile)!);
            System.IO.File.WriteAllText(PlacementFile, csv);
        }
        catch { /* ignore */ }
    }

    private void LoadWindowPlacement()
    {
        try
        {
            if (!System.IO.File.Exists(PlacementFile)) return;
            var parts = System.IO.File.ReadAllText(PlacementFile).Split(',');
            if (parts.Length < 4) return;
            if (int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var y)
                && int.TryParse(parts[2], out var w) && int.TryParse(parts[3], out var h))
            {
                Position = new Avalonia.PixelPoint(x, y);
                Width = w;
                Height = h;
            }
        }
        catch { /* ignore */ }
    }
}
