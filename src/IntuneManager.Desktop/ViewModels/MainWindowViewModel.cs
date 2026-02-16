using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IntuneManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly IntuneGraphClientFactory _graphClientFactory;
    private readonly IExportService _exportService;

    private GraphServiceClient? _graphClient;
    private IConfigurationProfileService? _configProfileService;
    private IImportService? _importService;
    private ICompliancePolicyService? _compliancePolicyService;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _windowTitle = "IntuneManager";

    [ObservableProperty]
    private TenantProfile? _activeProfile;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not connected";

    // --- Navigation ---
    [ObservableProperty]
    private NavCategory? _selectedCategory;

    public ObservableCollection<NavCategory> NavCategories { get; } =
    [
        new NavCategory { Name = "Device Configurations", Icon = "⚙" },
        new NavCategory { Name = "Compliance Policies", Icon = "✓" }
    ];

    // --- Device Configurations ---
    [ObservableProperty]
    private ObservableCollection<DeviceConfiguration> _deviceConfigurations = [];

    [ObservableProperty]
    private DeviceConfiguration? _selectedConfiguration;

    // --- Compliance Policies ---
    [ObservableProperty]
    private ObservableCollection<DeviceCompliancePolicy> _compliancePolicies = [];

    [ObservableProperty]
    private DeviceCompliancePolicy? _selectedCompliancePolicy;

    // --- Profile switcher ---
    [ObservableProperty]
    private TenantProfile? _selectedSwitchProfile;

    public ObservableCollection<TenantProfile> SwitcherProfiles { get; } = [];

    public event Func<TenantProfile, Task<bool>>? SwitchProfileRequested;

    public LoginViewModel LoginViewModel { get; }

    public MainWindowViewModel(
        ProfileService profileService,
        IntuneGraphClientFactory graphClientFactory,
        IExportService exportService)
    {
        _profileService = profileService;
        _graphClientFactory = graphClientFactory;
        _exportService = exportService;

        LoginViewModel = new LoginViewModel(profileService, graphClientFactory);
        LoginViewModel.LoginSucceeded += OnLoginSucceeded;

        CurrentView = LoginViewModel;

        // Load profiles asynchronously — never block the UI thread
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            await _profileService.LoadAsync();
            LoginViewModel.PopulateSavedProfiles();
            LoginViewModel.SelectActiveProfile();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load profiles: {ex.Message}");
        }
    }

    // Design-time constructor
    public MainWindowViewModel()
    {
        _profileService = null!;
        _graphClientFactory = null!;
        _exportService = null!;
        LoginViewModel = null!;
    }

    // --- Navigation ---

    // Computed visibility helpers for the view
    public bool IsDeviceConfigCategory => SelectedCategory?.Name == "Device Configurations";
    public bool IsCompliancePolicyCategory => SelectedCategory?.Name == "Compliance Policies";

    partial void OnSelectedCategoryChanged(NavCategory? value)
    {
        // Clear selections when switching categories
        SelectedConfiguration = null;
        SelectedCompliancePolicy = null;
        OnPropertyChanged(nameof(IsDeviceConfigCategory));
        OnPropertyChanged(nameof(IsCompliancePolicyCategory));
    }

    // --- Connection ---

    private async void OnLoginSucceeded(object? sender, TenantProfile profile)
    {
        await ConnectToProfile(profile);
    }

    private async Task ConnectToProfile(TenantProfile profile)
    {
        ClearError();
        IsBusy = true;
        StatusText = $"Connecting to {profile.Name}...";

        try
        {
            ActiveProfile = profile;
            IsConnected = true;
            WindowTitle = $"IntuneManager - {profile.Name}";
            CurrentView = null;

            _graphClient = await _graphClientFactory.CreateClientAsync(profile);
            _configProfileService = new ConfigurationProfileService(_graphClient);
            _compliancePolicyService = new CompliancePolicyService(_graphClient);
            _importService = new ImportService(_configProfileService, _compliancePolicyService);

            RefreshSwitcherProfiles();
            SelectedSwitchProfile = profile;

            // Default to first nav category
            SelectedCategory = NavCategories.FirstOrDefault();

            StatusText = $"Connected to {profile.Name}";
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            SetError($"Connection failed: {ex.Message}");
            StatusText = "Connection failed";
            DisconnectInternal();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshSwitcherProfiles()
    {
        SwitcherProfiles.Clear();
        foreach (var p in _profileService.Profiles)
            SwitcherProfiles.Add(p);
    }

    partial void OnSelectedSwitchProfileChanged(TenantProfile? value)
    {
        if (value is null || !IsConnected || value.Id == ActiveProfile?.Id)
            return;

        _ = RequestSwitchAsync(value);
    }

    private async Task RequestSwitchAsync(TenantProfile target)
    {
        if (SwitchProfileRequested is not null)
        {
            var confirmed = await SwitchProfileRequested.Invoke(target);
            if (confirmed)
            {
                DisconnectInternal();
                await ConnectToProfile(target);
                target.LastUsed = DateTime.UtcNow;
                _profileService.SetActiveProfile(target.Id);
                await _profileService.SaveAsync();
            }
            else
            {
                SelectedSwitchProfile = ActiveProfile;
            }
        }
    }

    // --- Refresh (loads data for the selected category) ---

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            if (_configProfileService != null)
            {
                StatusText = "Loading device configurations...";
                var configs = await _configProfileService.ListDeviceConfigurationsAsync(cancellationToken);
                DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);
            }

            if (_compliancePolicyService != null)
            {
                StatusText = "Loading compliance policies...";
                var policies = await _compliancePolicyService.ListCompliancePoliciesAsync(cancellationToken);
                CompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(policies);
            }

            var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count;
            StatusText = $"Loaded {totalItems} item(s) ({DeviceConfigurations.Count} configs, {CompliancePolicies.Count} policies)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load data: {ex.Message}");
            StatusText = "Error loading data";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Export ---

    [RelayCommand]
    private async Task ExportSelectedAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();

            if (IsDeviceConfigCategory && SelectedConfiguration != null)
            {
                StatusText = $"Exporting {SelectedConfiguration.DisplayName}...";
                await _exportService.ExportDeviceConfigurationAsync(
                    SelectedConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsCompliancePolicyCategory && SelectedCompliancePolicy != null)
            {
                StatusText = $"Exporting {SelectedCompliancePolicy.DisplayName}...";
                var assignments = _compliancePolicyService != null && SelectedCompliancePolicy.Id != null
                    ? await _compliancePolicyService.GetAssignmentsAsync(SelectedCompliancePolicy.Id, cancellationToken)
                    : [];
                await _exportService.ExportCompliancePolicyAsync(
                    SelectedCompliancePolicy, assignments, outputPath, migrationTable, cancellationToken);
            }
            else
            {
                StatusText = "Nothing selected to export";
                return;
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();
            var count = 0;

            // Export device configs
            if (DeviceConfigurations.Any())
            {
                StatusText = "Exporting device configurations...";
                foreach (var config in DeviceConfigurations)
                {
                    await _exportService.ExportDeviceConfigurationAsync(config, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export compliance policies with assignments
            if (CompliancePolicies.Any() && _compliancePolicyService != null)
            {
                StatusText = "Exporting compliance policies...";
                foreach (var policy in CompliancePolicies)
                {
                    var assignments = policy.Id != null
                        ? await _compliancePolicyService.GetAssignmentsAsync(policy.Id, cancellationToken)
                        : [];
                    await _exportService.ExportCompliancePolicyAsync(policy, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported {count} item(s) to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Import ---

    [RelayCommand]
    private async Task ImportFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (_importService == null) return;

        ClearError();
        IsBusy = true;
        StatusText = "Importing...";

        try
        {
            var migrationTable = await _importService.ReadMigrationTableAsync(folderPath, cancellationToken);
            var imported = 0;

            // Import device configurations
            var configs = await _importService.ReadDeviceConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var config in configs)
            {
                await _importService.ImportDeviceConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import compliance policies
            var policies = await _importService.ReadCompliancePoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in policies)
            {
                await _importService.ImportCompliancePolicyAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Save updated migration table
            await _exportService.SaveMigrationTableAsync(migrationTable, folderPath, cancellationToken);
            StatusText = $"Imported {imported} item(s)";

            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            SetError($"Import failed: {ex.Message}");
            StatusText = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Disconnect ---

    [RelayCommand]
    private void Disconnect()
    {
        DisconnectInternal();
        CurrentView = LoginViewModel;
        LoginViewModel.PopulateSavedProfiles();
        LoginViewModel.SelectActiveProfile();
    }

    private void DisconnectInternal()
    {
        IsConnected = false;
        ActiveProfile = null;
        WindowTitle = "IntuneManager";
        StatusText = "Not connected";
        SelectedCategory = null;
        DeviceConfigurations.Clear();
        SelectedConfiguration = null;
        CompliancePolicies.Clear();
        SelectedCompliancePolicy = null;
        _graphClient = null;
        _configProfileService = null;
        _compliancePolicyService = null;
        _importService = null;
    }
}
