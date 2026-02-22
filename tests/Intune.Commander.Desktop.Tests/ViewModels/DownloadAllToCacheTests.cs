using System.Collections;
using System.Reflection;
using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.ViewModels;

namespace Intune.Commander.Desktop.Tests.ViewModels;

/// <summary>
/// Tests for the DownloadAllToCache command logic in MainWindowViewModel,
/// specifically the BuildDownloadTaskList helper that assembles the 32 download tasks.
/// </summary>
public class DownloadAllToCacheTests
{
    // ─── BuildDownloadTaskList Tests ──────────────────────────────────────────

    [Fact]
    public void BuildDownloadTaskList_WithNoServicesSet_ReturnsEmptyList()
    {
        var vm = new MainWindowViewModel();

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Empty(result);
    }

    [Fact]
    public void BuildDownloadTaskList_WithAllServicesSet_Returns32Tasks()
    {
        var vm = new MainWindowViewModel();
        InjectAllStubServices(vm);

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Equal(32, result.Count);
    }

    [Fact]
    public void BuildDownloadTaskList_TaskNamesMatchExpectedSet()
    {
        var vm = new MainWindowViewModel();
        InjectAllStubServices(vm);

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");
        var names = GetTaskNames(result);

        Assert.Contains("Device Configurations", names);
        Assert.Contains("Compliance Policies", names);
        Assert.Contains("Applications", names);
        Assert.Contains("Settings Catalog", names);
        Assert.Contains("Conditional Access", names);
        Assert.Contains("Assignment Filters", names);
        Assert.Contains("Policy Sets", names);
        Assert.Contains("Endpoint Security", names);
        Assert.Contains("Administrative Templates", names);
        Assert.Contains("Enrollment Configurations", names);
        Assert.Contains("App Protection Policies", names);
        Assert.Contains("Managed Device App Configurations", names);
        Assert.Contains("Targeted Managed App Configurations", names);
        Assert.Contains("Terms and Conditions", names);
        Assert.Contains("Scope Tags", names);
        Assert.Contains("Role Definitions", names);
        Assert.Contains("Intune Branding Profiles", names);
        Assert.Contains("Azure Branding Localizations", names);
        Assert.Contains("Autopilot Profiles", names);
        Assert.Contains("Device Health Scripts", names);
        Assert.Contains("Mac Custom Attributes", names);
        Assert.Contains("Feature Update Profiles", names);
        Assert.Contains("Named Locations", names);
        Assert.Contains("Authentication Strength Policies", names);
        Assert.Contains("Authentication Contexts", names);
        Assert.Contains("Terms of Use Agreements", names);
        Assert.Contains("Device Management Scripts", names);
        Assert.Contains("Device Shell Scripts", names);
        Assert.Contains("Compliance Scripts", names);
        Assert.Contains("Dynamic Groups", names);
        Assert.Contains("Assigned Groups", names);
        Assert.Contains("Users", names);
    }

    [Fact]
    public void BuildDownloadTaskList_AllTasksHaveNonNullAction()
    {
        var vm = new MainWindowViewModel();
        InjectAllStubServices(vm);

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        foreach (var task in result)
        {
            var action = task.GetType().GetProperty("Action")?.GetValue(task);
            Assert.NotNull(action);
        }
    }

    [Fact]
    public void BuildDownloadTaskList_WithOnlyCoreServicesSet_Returns4Tasks()
    {
        var vm = new MainWindowViewModel();
        SetField(vm, "_configProfileService", CreateProxy<IConfigurationProfileService>());
        SetField(vm, "_compliancePolicyService", CreateProxy<ICompliancePolicyService>());
        SetField(vm, "_applicationService", CreateProxy<IApplicationService>());
        SetField(vm, "_settingsCatalogService", CreateProxy<ISettingsCatalogService>());

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void BuildDownloadTaskList_WithOnlyGroupServiceSet_Returns2Tasks()
    {
        var vm = new MainWindowViewModel();
        SetField(vm, "_groupService", CreateProxy<IGroupService>());

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Equal(2, result.Count);
        var names = GetTaskNames(result);
        Assert.Contains("Dynamic Groups", names);
        Assert.Contains("Assigned Groups", names);
    }

    [Fact]
    public void BuildDownloadTaskList_WithOnlyUserServiceSet_Returns1Task()
    {
        var vm = new MainWindowViewModel();
        SetField(vm, "_userService", CreateProxy<IUserService>());

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Single(result);
        var names = GetTaskNames(result);
        Assert.Contains("Users", names);
    }

    [Fact]
    public void BuildDownloadTaskList_ManagedAppConfigServiceContributes2Tasks()
    {
        var vm = new MainWindowViewModel();
        SetField(vm, "_managedAppConfigurationService", CreateProxy<IManagedAppConfigurationService>());

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");

        Assert.Equal(2, result.Count);
        var names = GetTaskNames(result);
        Assert.Contains("Managed Device App Configurations", names);
        Assert.Contains("Targeted Managed App Configurations", names);
    }

    [Fact]
    public void BuildDownloadTaskList_AllTaskNamesAreDistinct()
    {
        var vm = new MainWindowViewModel();
        InjectAllStubServices(vm);

        var result = InvokeBuildDownloadTaskList(vm, "tenant-1");
        var names = GetTaskNames(result);

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    // ─── CancelDownloadAll Tests ──────────────────────────────────────────────

    [Fact]
    public void CancelDownloadAll_MethodExists_OnMainWindowViewModel()
    {
        var method = typeof(MainWindowViewModel).GetMethod("CancelDownloadAll",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void DownloadAllToCacheAsync_MethodExists_OnMainWindowViewModel()
    {
        var method = typeof(MainWindowViewModel).GetMethod("DownloadAllToCacheAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static IList InvokeBuildDownloadTaskList(MainWindowViewModel vm, string tenantId)
    {
        var method = typeof(MainWindowViewModel).GetMethod("BuildDownloadTaskList",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        try
        {
            var result = method.Invoke(vm, [tenantId, CancellationToken.None]);
            return (IList)result!;
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            throw tie.InnerException;
        }
    }

    private static List<string?> GetTaskNames(IList tasks)
        => tasks.Cast<object>()
                .Select(t => t.GetType().GetProperty("Name")?.GetValue(t) as string)
                .ToList();

    private static void SetField(MainWindowViewModel vm, string fieldName, object? value)
    {
        var field = typeof(MainWindowViewModel).GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field.SetValue(vm, value);
    }

    private static TInterface CreateProxy<TInterface>() where TInterface : class
        => DispatchProxy.Create<TInterface, ThrowProxy>();

    private class ThrowProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new NotSupportedException("Stub proxy — not intended to be called in tests");
    }

    private static void InjectAllStubServices(MainWindowViewModel vm)
    {
        SetField(vm, "_configProfileService",            CreateProxy<IConfigurationProfileService>());
        SetField(vm, "_compliancePolicyService",         CreateProxy<ICompliancePolicyService>());
        SetField(vm, "_applicationService",              CreateProxy<IApplicationService>());
        SetField(vm, "_settingsCatalogService",          CreateProxy<ISettingsCatalogService>());
        SetField(vm, "_conditionalAccessPolicyService",  CreateProxy<IConditionalAccessPolicyService>());
        SetField(vm, "_assignmentFilterService",         CreateProxy<IAssignmentFilterService>());
        SetField(vm, "_policySetService",                CreateProxy<IPolicySetService>());
        SetField(vm, "_endpointSecurityService",         CreateProxy<IEndpointSecurityService>());
        SetField(vm, "_administrativeTemplateService",   CreateProxy<IAdministrativeTemplateService>());
        SetField(vm, "_enrollmentConfigurationService",  CreateProxy<IEnrollmentConfigurationService>());
        SetField(vm, "_appProtectionPolicyService",      CreateProxy<IAppProtectionPolicyService>());
        SetField(vm, "_managedAppConfigurationService",  CreateProxy<IManagedAppConfigurationService>());
        SetField(vm, "_termsAndConditionsService",       CreateProxy<ITermsAndConditionsService>());
        SetField(vm, "_scopeTagService",                 CreateProxy<IScopeTagService>());
        SetField(vm, "_roleDefinitionService",           CreateProxy<IRoleDefinitionService>());
        SetField(vm, "_intuneBrandingService",           CreateProxy<IIntuneBrandingService>());
        SetField(vm, "_azureBrandingService",            CreateProxy<IAzureBrandingService>());
        SetField(vm, "_autopilotService",                CreateProxy<IAutopilotService>());
        SetField(vm, "_deviceHealthScriptService",       CreateProxy<IDeviceHealthScriptService>());
        SetField(vm, "_macCustomAttributeService",       CreateProxy<IMacCustomAttributeService>());
        SetField(vm, "_featureUpdateProfileService",     CreateProxy<IFeatureUpdateProfileService>());
        SetField(vm, "_namedLocationService",            CreateProxy<INamedLocationService>());
        SetField(vm, "_authenticationStrengthService",   CreateProxy<IAuthenticationStrengthService>());
        SetField(vm, "_authenticationContextService",    CreateProxy<IAuthenticationContextService>());
        SetField(vm, "_termsOfUseService",               CreateProxy<ITermsOfUseService>());
        SetField(vm, "_deviceManagementScriptService",   CreateProxy<IDeviceManagementScriptService>());
        SetField(vm, "_deviceShellScriptService",        CreateProxy<IDeviceShellScriptService>());
        SetField(vm, "_complianceScriptService",         CreateProxy<IComplianceScriptService>());
        SetField(vm, "_groupService",                    CreateProxy<IGroupService>());
        SetField(vm, "_userService",                     CreateProxy<IUserService>());
    }
}
