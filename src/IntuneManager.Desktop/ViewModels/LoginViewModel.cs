using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;

namespace IntuneManager.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly IntuneGraphClientFactory _graphClientFactory;

    [ObservableProperty]
    private string _tenantId = string.Empty;

    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private TenantProfile? _selectedProfile;

    public ObservableCollection<TenantProfile> SavedProfiles { get; } = [];

    public event EventHandler<TenantProfile>? LoginSucceeded;

    public LoginViewModel(ProfileService profileService, IntuneGraphClientFactory graphClientFactory)
    {
        _profileService = profileService;
        _graphClientFactory = graphClientFactory;
    }

    /// <summary>
    /// Called by MainWindowViewModel after profiles are loaded asynchronously.
    /// </summary>
    public void PopulateSavedProfiles()
    {
        SavedProfiles.Clear();
        foreach (var p in _profileService.Profiles)
            SavedProfiles.Add(p);
    }

    partial void OnSelectedProfileChanged(TenantProfile? value)
    {
        if (value is null) return;
        ProfileName = value.Name;
        TenantId = value.TenantId;
        ClientId = value.ClientId;
        ClientSecret = value.ClientSecret ?? string.Empty;

        LoginCommand.NotifyCanExecuteChanged();
        SaveProfileCommand.NotifyCanExecuteChanged();
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSaveProfile))]
    private async Task SaveProfileAsync(CancellationToken cancellationToken)
    {
        ClearError();
        try
        {
            if (SelectedProfile is not null)
            {
                // Update existing profile
                SelectedProfile.Name = ProfileName.Trim();
                SelectedProfile.TenantId = TenantId.Trim();
                SelectedProfile.ClientId = ClientId.Trim();
                SelectedProfile.ClientSecret = ClientSecret.Trim();
                SelectedProfile.AuthMethod = string.IsNullOrWhiteSpace(ClientSecret)
                    ? AuthMethod.Interactive
                    : AuthMethod.ClientSecret;
            }
            else
            {
                // Create new profile
                var profile = new TenantProfile
                {
                    Name = string.IsNullOrWhiteSpace(ProfileName) ? $"Tenant-{TenantId.Trim()[..Math.Min(8, TenantId.Trim().Length)]}" : ProfileName.Trim(),
                    TenantId = TenantId.Trim(),
                    ClientId = ClientId.Trim(),
                    ClientSecret = ClientSecret.Trim(),
                    Cloud = CloudEnvironment.Commercial,
                    AuthMethod = string.IsNullOrWhiteSpace(ClientSecret)
                        ? AuthMethod.Interactive
                        : AuthMethod.ClientSecret
                };
                _profileService.AddProfile(profile);
                SavedProfiles.Add(profile);
                SelectedProfile = profile;
            }

            await _profileService.SaveAsync(cancellationToken);
            StatusMessage = "Profile saved";
        }
        catch (Exception ex)
        {
            SetError($"Failed to save profile: {ex.Message}");
        }
    }

    private bool CanSaveProfile()
    {
        return !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private async Task DeleteProfileAsync(CancellationToken cancellationToken)
    {
        if (SelectedProfile is null) return;

        ClearError();
        try
        {
            _profileService.RemoveProfile(SelectedProfile.Id);
            SavedProfiles.Remove(SelectedProfile);
            await _profileService.SaveAsync(cancellationToken);

            SelectedProfile = null;
            ProfileName = string.Empty;
            TenantId = string.Empty;
            ClientId = string.Empty;
            ClientSecret = string.Empty;
            StatusMessage = "Profile deleted";
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete profile: {ex.Message}");
        }
    }

    private bool CanDeleteProfile() => SelectedProfile is not null;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;
        StatusMessage = "Authenticating...";

        try
        {
            // Save profile first if not already saved
            if (SelectedProfile is null)
                await SaveProfileAsync(cancellationToken);

            var profile = SelectedProfile!;

            // Test the connection
            var client = await _graphClientFactory.CreateClientAsync(profile, cancellationToken);
            await client.DeviceManagement.GetAsync(cancellationToken: cancellationToken);

            StatusMessage = "Connected successfully!";
            profile.LastUsed = DateTime.UtcNow;
            _profileService.SetActiveProfile(profile.Id);
            await _profileService.SaveAsync(cancellationToken);

            LoginSucceeded?.Invoke(this, profile);
        }
        catch (Exception ex)
        {
            SetError($"Authentication failed: {ex.Message}");
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId);
    }

    partial void OnTenantIdChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        SaveProfileCommand.NotifyCanExecuteChanged();
    }

    partial void OnClientIdChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        SaveProfileCommand.NotifyCanExecuteChanged();
    }

    partial void OnClientSecretChanged(string value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        SaveProfileCommand.NotifyCanExecuteChanged();
    }
}
