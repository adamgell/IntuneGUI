using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class BaselineViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBaselineService _baselineService;
    private readonly ISettingsCatalogService _settingsCatalogService;
    private readonly IEndpointSecurityService _endpointSecurityService;
    private readonly ICompliancePolicyService _compliancePolicyService;
    private readonly IGroupService _groupService;

    [ObservableProperty]
    private BaselinePolicyType _activeBaselineType = BaselinePolicyType.SettingsCatalog;

    public ObservableCollection<BaselinePolicy> Baselines { get; } = [];

    [ObservableProperty]
    private BaselinePolicy? _selectedBaseline;

    [ObservableProperty]
    private string _categoryFilter = "All";

    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty]
    private BaselineComparisonResult? _comparisonResult;

    // TODO: ES/Compliance inline editing
    // TODO: Deploy to Existing for ES/Compliance types
    public bool IsCompareAvailable => ActiveBaselineType == BaselinePolicyType.SettingsCatalog;

    public BaselineViewModel(
        IBaselineService baselineService,
        ISettingsCatalogService settingsCatalogService,
        IEndpointSecurityService endpointSecurityService,
        ICompliancePolicyService compliancePolicyService,
        IGroupService groupService)
    {
        _baselineService = baselineService;
        _settingsCatalogService = settingsCatalogService;
        _endpointSecurityService = endpointSecurityService;
        _compliancePolicyService = compliancePolicyService;
        _groupService = groupService;

        LoadBaselines();
    }

    partial void OnActiveBaselineTypeChanged(BaselinePolicyType value)
    {
        LoadBaselines();
        ComparisonResult = null;
        OnPropertyChanged(nameof(IsCompareAvailable));
    }

    partial void OnCategoryFilterChanged(string value)
    {
        RefreshFilteredBaselines();
    }

    [RelayCommand]
    private async Task DeployAsNewAsync(CancellationToken ct)
    {
        if (SelectedBaseline is null) return;

        IsBusy = true;
        ClearError();

        try
        {
            switch (SelectedBaseline.PolicyType)
            {
                case BaselinePolicyType.SettingsCatalog:
                    await DeploySettingsCatalogAsync(SelectedBaseline, ct);
                    break;
                case BaselinePolicyType.EndpointSecurity:
                    await DeployEndpointSecurityAsync(SelectedBaseline, ct);
                    break;
                case BaselinePolicyType.Compliance:
                    await DeployComplianceAsync(SelectedBaseline, ct);
                    break;
            }

            DebugLog.Log("Baseline", $"Deployed baseline '{SelectedBaseline.Name}' as new policy");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Deploy failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CompareWithTenantPolicyAsync(string? tenantPolicyId, CancellationToken ct)
    {
        if (SelectedBaseline is null || string.IsNullOrEmpty(tenantPolicyId)) return;
        if (ActiveBaselineType != BaselinePolicyType.SettingsCatalog) return;

        IsBusy = true;
        ClearError();
        ComparisonResult = null;

        try
        {
            var tenantSettings = await _settingsCatalogService.GetPolicySettingsAsync(tenantPolicyId, ct);
            var tenantPolicy = await _settingsCatalogService.GetSettingsCatalogPolicyAsync(tenantPolicyId, ct);

            ComparisonResult = _baselineService.CompareSettingsCatalog(
                SelectedBaseline,
                tenantSettings,
                tenantPolicyId,
                tenantPolicy?.Name);

            DebugLog.Log("Baseline",
                $"Compared '{SelectedBaseline.Name}' with '{tenantPolicy?.Name}': " +
                $"{ComparisonResult.Matching.Count} match, {ComparisonResult.Drifted.Count} drift, " +
                $"{ComparisonResult.Missing.Count} missing, {ComparisonResult.Extra.Count} extra");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Compare failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadBaselines()
    {
        Categories.Clear();
        Categories.Add("All");

        foreach (var cat in _baselineService.GetCategories(ActiveBaselineType))
            Categories.Add(cat);

        CategoryFilter = "All";
        RefreshFilteredBaselines();
    }

    private void RefreshFilteredBaselines()
    {
        Baselines.Clear();
        SelectedBaseline = null;

        var items = CategoryFilter == "All"
            ? _baselineService.GetBaselinesByType(ActiveBaselineType)
            : _baselineService.GetBaselinesByCategory(CategoryFilter, ActiveBaselineType);

        foreach (var b in items)
            Baselines.Add(b);
    }

    private async Task DeploySettingsCatalogAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        var export = JsonSerializer.Deserialize<SettingsCatalogExport>(
            baseline.RawJson.GetRawText(), JsonOptions);

        if (export is null)
            throw new InvalidOperationException("Failed to deserialize Settings Catalog baseline");

        // Clear IDs for creation
        export.Policy.Id = null;
        export.Policy.CreatedDateTime = null;
        export.Policy.LastModifiedDateTime = null;

        var created = await _settingsCatalogService.CreateSettingsCatalogPolicyAsync(export.Policy, ct);

        if (created.Id is not null && export.Settings.Count > 0)
        {
            // Use CancellationToken.None: once the policy is created, we must finish
            // adding settings to avoid leaving an orphaned empty policy in the tenant
            await _settingsCatalogService.UpdatePolicySettingsAsync(
                created.Id, export.Settings, CancellationToken.None);
        }
    }

    private async Task DeployEndpointSecurityAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        var export = JsonSerializer.Deserialize<EndpointSecurityExport>(
            baseline.RawJson.GetRawText(), JsonOptions);

        if (export is null)
            throw new InvalidOperationException("Failed to deserialize Endpoint Security baseline");

        export.Intent.Id = null;
        await _endpointSecurityService.CreateEndpointSecurityIntentAsync(export.Intent, ct);
    }

    private async Task DeployComplianceAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        var export = JsonSerializer.Deserialize<CompliancePolicyExport>(
            baseline.RawJson.GetRawText(), JsonOptions);

        if (export is null)
            throw new InvalidOperationException("Failed to deserialize Compliance baseline");

        export.Policy.Id = null;
        export.Policy.CreatedDateTime = null;
        export.Policy.LastModifiedDateTime = null;
        export.Policy.Version = null;
        await _compliancePolicyService.CreateCompliancePolicyAsync(export.Policy, ct);
    }
}
