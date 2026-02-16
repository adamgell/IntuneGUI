using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using IntuneManager.Core.Models;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Graph.Models;
using SkiaSharp;

namespace IntuneManager.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Overview/Dashboard tab.
/// All data is computed from existing loaded collections — no extra Graph calls.
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    // --- Tenant Info ---
    [ObservableProperty]
    private string _tenantName = "";

    [ObservableProperty]
    private string _tenantId = "";

    [ObservableProperty]
    private string _cloudEnvironment = "";

    [ObservableProperty]
    private string _profileName = "";

    // --- Summary counts ---
    [ObservableProperty]
    private int _totalDeviceConfigs;

    [ObservableProperty]
    private int _totalCompliancePolicies;

    [ObservableProperty]
    private int _totalApplications;

    [ObservableProperty]
    private int _totalAppAssignmentRows;

    [ObservableProperty]
    private int _unassignedAppCount;

    // --- Charts ---
    [ObservableProperty]
    private ISeries[] _appsByPlatformSeries = [];

    [ObservableProperty]
    private ISeries[] _configsByPlatformSeries = [];

    [ObservableProperty]
    private ISeries[] _topGroupsSeries = [];

    [ObservableProperty]
    private Axis[] _topGroupsXAxes = [];

    [ObservableProperty]
    private Axis[] _topGroupsYAxes = [];

    // --- Recently modified ---
    public ObservableCollection<RecentItem> RecentlyModified { get; } = [];

    // --- Palette ---
    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#2196F3"), // Blue
        SKColor.Parse("#4CAF50"), // Green
        SKColor.Parse("#FF9800"), // Orange
        SKColor.Parse("#9C27B0"), // Purple
        SKColor.Parse("#F44336"), // Red
        SKColor.Parse("#00BCD4"), // Cyan
        SKColor.Parse("#795548"), // Brown
        SKColor.Parse("#607D8B")  // Blue Grey
    ];

    public void Update(
        TenantProfile? profile,
        IReadOnlyList<DeviceConfiguration> configs,
        IReadOnlyList<DeviceCompliancePolicy> policies,
        IReadOnlyList<MobileApp> apps,
        IReadOnlyList<AppAssignmentRow> assignmentRows)
    {
        // Tenant info
        TenantName = profile?.Name ?? "";
        TenantId = profile?.TenantId ?? "";
        CloudEnvironment = profile?.Cloud.ToString() ?? "";
        ProfileName = profile?.Name ?? "";

        // Summary counts
        TotalDeviceConfigs = configs.Count;
        TotalCompliancePolicies = policies.Count;
        TotalApplications = apps.Count;
        TotalAppAssignmentRows = assignmentRows.Count;

        // Unassigned apps
        var appsWithAssignments = new HashSet<string>(
            assignmentRows
                .Where(r => r.AssignmentType != "None" && !string.IsNullOrEmpty(r.AppId))
                .Select(r => r.AppId));
        UnassignedAppCount = apps.Count(a => !string.IsNullOrEmpty(a.Id) && !appsWithAssignments.Contains(a.Id!));

        // Platform breakdown for apps
        BuildAppsByPlatformChart(apps);

        // Platform breakdown for configs
        BuildConfigsByPlatformChart(configs);

        // Top assigned groups
        BuildTopGroupsChart(assignmentRows);

        // Recently modified
        BuildRecentlyModified(configs, policies, apps);
    }

    private void BuildAppsByPlatformChart(IReadOnlyList<MobileApp> apps)
    {
        var groups = apps
            .GroupBy(a => MainWindowViewModel.InferPlatform(a.OdataType))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderByDescending(g => g.Count())
            .ToList();

        var series = new List<ISeries>();
        for (var i = 0; i < groups.Count; i++)
        {
            var color = Palette[i % Palette.Length];
            series.Add(new PieSeries<int>
            {
                Values = [groups[i].Count()],
                Name = $"{groups[i].Key} ({groups[i].Count()})",
                Fill = new SolidColorPaint(color),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => groups[i].Key
            });
        }

        AppsByPlatformSeries = series.ToArray();
    }

    private void BuildConfigsByPlatformChart(IReadOnlyList<DeviceConfiguration> configs)
    {
        var groups = configs
            .GroupBy(c => MainWindowViewModel.InferPlatform(c.OdataType))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderByDescending(g => g.Count())
            .ToList();

        var series = new List<ISeries>();
        for (var i = 0; i < groups.Count; i++)
        {
            var color = Palette[i % Palette.Length];
            series.Add(new PieSeries<int>
            {
                Values = [groups[i].Count()],
                Name = $"{groups[i].Key} ({groups[i].Count()})",
                Fill = new SolidColorPaint(color),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => groups[i].Key
            });
        }

        ConfigsByPlatformSeries = series.ToArray();
    }

    private void BuildTopGroupsChart(IReadOnlyList<AppAssignmentRow> rows)
    {
        var topGroups = rows
            .Where(r => r.AssignmentType == "Group" && !string.IsNullOrEmpty(r.TargetName))
            .GroupBy(r => r.TargetName)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .ToList();

        if (topGroups.Count == 0)
        {
            TopGroupsSeries = [];
            TopGroupsXAxes = [];
            TopGroupsYAxes = [];
            return;
        }

        var labels = topGroups.Select(g => TruncateLabel(g.Key, 20)).ToArray();
        var values = topGroups.Select(g => g.Count()).ToArray();

        TopGroupsSeries =
        [
            new RowSeries<int>
            {
                Values = values,
                Name = "Assignments",
                Fill = new SolidColorPaint(SKColor.Parse("#2196F3")),
                DataLabelsSize = 11,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => p.Model.ToString()
            }
        ];

        TopGroupsYAxes =
        [
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#33888888"))
            }
        ];

        TopGroupsXAxes =
        [
            new Axis
            {
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#33888888"))
            }
        ];
    }

    private void BuildRecentlyModified(
        IReadOnlyList<DeviceConfiguration> configs,
        IReadOnlyList<DeviceCompliancePolicy> policies,
        IReadOnlyList<MobileApp> apps)
    {
        RecentlyModified.Clear();

        var items = new List<RecentItem>();

        foreach (var c in configs.Where(x => x.LastModifiedDateTime.HasValue))
            items.Add(new RecentItem
            {
                Name = c.DisplayName ?? "(unnamed)",
                Category = "Device Configuration",
                Modified = c.LastModifiedDateTime!.Value
            });

        foreach (var p in policies.Where(x => x.LastModifiedDateTime.HasValue))
            items.Add(new RecentItem
            {
                Name = p.DisplayName ?? "(unnamed)",
                Category = "Compliance Policy",
                Modified = p.LastModifiedDateTime!.Value
            });

        foreach (var a in apps.Where(x => x.LastModifiedDateTime.HasValue))
            items.Add(new RecentItem
            {
                Name = a.DisplayName ?? "(unnamed)",
                Category = "Application",
                Modified = a.LastModifiedDateTime!.Value
            });

        foreach (var item in items.OrderByDescending(i => i.Modified).Take(10))
            RecentlyModified.Add(item);
    }

    private static string TruncateLabel(string label, int max)
        => label.Length <= max ? label : label[..(max - 1)] + "…";
}

/// <summary>
/// Display model for the Recently Modified list.
/// </summary>
public class RecentItem
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required DateTimeOffset Modified { get; init; }
    public string ModifiedText => Modified.LocalDateTime.ToString("g");
}
