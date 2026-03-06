using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.ViewModels.Settings;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class SettingsPolicyEditorViewModel : ViewModelBase
{
    private readonly ISettingsCatalogService _settingsCatalogService;
    private readonly DeviceManagementConfigurationPolicy _policy;
    private List<DeviceManagementConfigurationSetting>? _originalSettings;

    [ObservableProperty]
    private string _policyName = string.Empty;

    [ObservableProperty]
    private string? _policyDescription;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private CategoryNodeViewModel? _selectedCategory;

    public ObservableCollection<CategoryNodeViewModel> CategoryTree { get; } = [];

    public ObservableCollection<SettingViewModelBase> CurrentSettings { get; } = [];

    public string PolicyId => _policy.Id ?? string.Empty;

    public SettingsPolicyEditorViewModel(
        ISettingsCatalogService settingsCatalogService,
        DeviceManagementConfigurationPolicy policy)
    {
        _settingsCatalogService = settingsCatalogService;
        _policy = policy;
        PolicyName = policy.Name ?? string.Empty;
        PolicyDescription = policy.Description;
    }

    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_policy.Id)) return;

        IsBusy = true;
        ClearError();

        try
        {
            var settings = await _settingsCatalogService.GetPolicySettingsAsync(_policy.Id, ct);
            _originalSettings = settings;

            var vms = settings.Select(SettingViewModelFactory.Create).ToList();

            // Subscribe to IsModified changes on each VM
            foreach (var vm in vms)
                vm.PropertyChanged += OnSettingPropertyChanged;

            BuildCategoryTree(vms);
            HasUnsavedChanges = false;
        }
        catch (OperationCanceledException)
        {
            // Cancelled — no error to show
        }
        catch (Exception ex)
        {
            SetError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryNodeViewModel? value)
    {
        CurrentSettings.Clear();
        if (value is null) return;

        foreach (var s in value.Settings)
            CurrentSettings.Add(s);
    }

    [RelayCommand]
    private async Task SaveMetadataAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_policy.Id)) return;

        IsBusy = true;
        ClearError();

        try
        {
            var patch = new DeviceManagementConfigurationPolicy
            {
                Name = PolicyName,
                Description = PolicyDescription
            };
            await _settingsCatalogService.UpdateSettingsCatalogPolicyMetadataAsync(_policy.Id, patch, ct);

            // Reflect changes back to the source policy object
            _policy.Name = PolicyName;
            _policy.Description = PolicyDescription;

            DebugLog.Log("SettingsEditor", $"Metadata saved for policy '{PolicyName}'");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Failed to save metadata: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_policy.Id)) return;

        IsBusy = true;
        ClearError();

        try
        {
            var allVms = CategoryTree
                .SelectMany(GetAllSettings)
                .ToList();

            var updatedSettings = allVms.Select(vm => vm.ToGraphSetting()).ToList();

            await _settingsCatalogService.UpdatePolicySettingsAsync(_policy.Id, updatedSettings, ct);

            // Reset IsModified on all VMs after successful save
            foreach (var vm in allVms)
                vm.IsModified = false;

            _originalSettings = updatedSettings;
            HasUnsavedChanges = false;

            DebugLog.Log("SettingsEditor", $"Settings saved for policy '{PolicyName}'");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DiscardChangesAsync(CancellationToken ct)
    {
        // Reload from Graph to discard all edits
        await LoadSettingsAsync(ct);
    }

    private void BuildCategoryTree(List<SettingViewModelBase> vms)
    {
        CategoryTree.Clear();

        // Group settings by their category using the definition registry
        var grouped = new Dictionary<string, List<SettingViewModelBase>>();

        foreach (var vm in vms)
        {
            var categoryName = ResolveCategoryForSetting(vm.SettingDefinitionId);
            if (!grouped.TryGetValue(categoryName, out var list))
            {
                list = [];
                grouped[categoryName] = list;
            }
            list.Add(vm);
        }

        // Sort categories alphabetically, but put "General" first
        var sortedKeys = grouped.Keys
            .OrderBy(k => k == "General" ? 0 : 1)
            .ThenBy(k => k)
            .ToList();

        foreach (var key in sortedKeys)
        {
            var node = new CategoryNodeViewModel { DisplayName = key };
            foreach (var vm in grouped[key])
                node.Settings.Add(vm);
            CategoryTree.Add(node);
        }

        SelectedCategory = CategoryTree.FirstOrDefault();
    }

    private static string ResolveCategoryForSetting(string? settingDefinitionId)
    {
        if (string.IsNullOrEmpty(settingDefinitionId))
            return "General";

        if (!SettingsCatalogDefinitionRegistry.Definitions.TryGetValue(settingDefinitionId, out var def)
            || string.IsNullOrEmpty(def.CategoryId))
            return "General";

        return SettingsCatalogDefinitionRegistry.ResolveCategoryName(def.CategoryId);
    }

    private static IEnumerable<SettingViewModelBase> GetAllSettings(CategoryNodeViewModel node)
    {
        foreach (var s in node.Settings)
            yield return s;
        foreach (var child in node.Children)
        {
            foreach (var s in GetAllSettings(child))
                yield return s;
        }
    }

    private void OnSettingPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingViewModelBase.IsModified))
            HasUnsavedChanges = CategoryTree.SelectMany(GetAllSettings).Any(vm => vm.IsModified);
    }
}
