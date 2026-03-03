using Intune.Commander.CLI.Helpers;
using Intune.Commander.CLI.Models;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Intune.Commander.CLI.Commands;

public static class ImportCommand
{
    public static Command Build()
    {
        var command = new Command("import", "Import Intune configurations from an export folder");

        var profile = new Option<string?>("--profile");
        var tenantId = new Option<string?>("--tenant-id");
        var clientId = new Option<string?>("--client-id");
        var secret = new Option<string?>("--secret");
        var cloud = new Option<string?>("--cloud");
        var folder = new Option<string>("--folder") { IsRequired = true };
        var dryRun = new Option<bool>("--dry-run");

        command.AddOption(profile);
        command.AddOption(tenantId);
        command.AddOption(clientId);
        command.AddOption(secret);
        command.AddOption(cloud);
        command.AddOption(folder);
        command.AddOption(dryRun);

        command.SetHandler(ExecuteAsync, profile, tenantId, clientId, secret, cloud, folder, dryRun);
        return command;
    }

    private static async Task ExecuteAsync(
        string? profile,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        string folder,
        bool dryRun)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Import folder '{folder}' was not found.");

        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();
        var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();
        var exportService = provider.GetRequiredService<IExportService>();

        var resolvedProfile = await ProfileResolver.ResolveAsync(profileService, profile, tenantId, clientId, secret, cloud);
        var graphClient = await graphClientFactory.CreateClientAsync(resolvedProfile, AuthHelper.DeviceCodeToStderr);

        var configurationProfileService = new ConfigurationProfileService(graphClient);
        var compliancePolicyService = new CompliancePolicyService(graphClient);
        var endpointSecurityService = new EndpointSecurityService(graphClient);
        var administrativeTemplateService = new AdministrativeTemplateService(graphClient);
        var enrollmentConfigurationService = new EnrollmentConfigurationService(graphClient);
        var appProtectionPolicyService = new AppProtectionPolicyService(graphClient);
        var managedAppConfigurationService = new ManagedAppConfigurationService(graphClient);
        var termsAndConditionsService = new TermsAndConditionsService(graphClient);
        var scopeTagService = new ScopeTagService(graphClient);
        var roleDefinitionService = new RoleDefinitionService(graphClient);
        var intuneBrandingService = new IntuneBrandingService(graphClient);
        var azureBrandingService = new AzureBrandingService(graphClient);
        var autopilotService = new AutopilotService(graphClient);
        var deviceHealthScriptService = new DeviceHealthScriptService(graphClient);
        var macCustomAttributeService = new MacCustomAttributeService(graphClient);
        var featureUpdateProfileService = new FeatureUpdateProfileService(graphClient);
        var namedLocationService = new NamedLocationService(graphClient);
        var authenticationStrengthService = new AuthenticationStrengthService(graphClient);
        var authenticationContextService = new AuthenticationContextService(graphClient);
        var termsOfUseService = new TermsOfUseService(graphClient);
        var deviceManagementScriptService = new DeviceManagementScriptService(graphClient);
        var deviceShellScriptService = new DeviceShellScriptService(graphClient);
        var complianceScriptService = new ComplianceScriptService(graphClient);
        var qualityUpdateProfileService = new QualityUpdateProfileService(graphClient);
        var driverUpdateProfileService = new DriverUpdateProfileService(graphClient);
        var settingsCatalogService = new SettingsCatalogService(graphClient);

        var importService = new ImportService(
            configurationProfileService,
            compliancePolicyService,
            endpointSecurityService,
            administrativeTemplateService,
            enrollmentConfigurationService,
            appProtectionPolicyService,
            managedAppConfigurationService,
            termsAndConditionsService,
            scopeTagService,
            roleDefinitionService,
            intuneBrandingService,
            azureBrandingService,
            autopilotService,
            deviceHealthScriptService,
            macCustomAttributeService,
            featureUpdateProfileService,
            namedLocationService,
            authenticationStrengthService,
            authenticationContextService,
            termsOfUseService,
            deviceManagementScriptService,
            deviceShellScriptService,
            complianceScriptService,
            qualityUpdateProfileService,
            driverUpdateProfileService,
            settingsCatalogService);

        var migrationTable = await importService.ReadMigrationTableAsync(folder);
        var imported = 0;

        var deviceConfigurations = await importService.ReadDeviceConfigurationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in deviceConfigurations)
            {
                await importService.ImportDeviceConfigurationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += deviceConfigurations.Count;

        var compliancePolicies = await importService.ReadCompliancePoliciesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in compliancePolicies)
            {
                await importService.ImportCompliancePolicyAsync(item, migrationTable);
                imported++;
            }
        else
            imported += compliancePolicies.Count;

        var endpointSecurityIntents = await importService.ReadEndpointSecurityIntentsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in endpointSecurityIntents)
            {
                await importService.ImportEndpointSecurityIntentAsync(item, migrationTable);
                imported++;
            }
        else
            imported += endpointSecurityIntents.Count;

        var administrativeTemplates = await importService.ReadAdministrativeTemplatesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in administrativeTemplates)
            {
                await importService.ImportAdministrativeTemplateAsync(item, migrationTable);
                imported++;
            }
        else
            imported += administrativeTemplates.Count;

        var enrollmentConfigurations = await importService.ReadEnrollmentConfigurationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in enrollmentConfigurations)
            {
                await importService.ImportEnrollmentConfigurationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += enrollmentConfigurations.Count;

        var appProtectionPolicies = await importService.ReadAppProtectionPoliciesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in appProtectionPolicies)
            {
                await importService.ImportAppProtectionPolicyAsync(item, migrationTable);
                imported++;
            }
        else
            imported += appProtectionPolicies.Count;

        var managedDeviceAppConfigurations = await importService.ReadManagedDeviceAppConfigurationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in managedDeviceAppConfigurations)
            {
                await importService.ImportManagedDeviceAppConfigurationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += managedDeviceAppConfigurations.Count;

        var targetedManagedAppConfigurations = await importService.ReadTargetedManagedAppConfigurationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in targetedManagedAppConfigurations)
            {
                await importService.ImportTargetedManagedAppConfigurationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += targetedManagedAppConfigurations.Count;

        var termsAndConditions = await importService.ReadTermsAndConditionsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in termsAndConditions)
            {
                await importService.ImportTermsAndConditionsAsync(item, migrationTable);
                imported++;
            }
        else
            imported += termsAndConditions.Count;

        var scopeTags = await importService.ReadScopeTagsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in scopeTags)
            {
                await importService.ImportScopeTagAsync(item, migrationTable);
                imported++;
            }
        else
            imported += scopeTags.Count;

        var roleDefinitions = await importService.ReadRoleDefinitionsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in roleDefinitions)
            {
                await importService.ImportRoleDefinitionAsync(item, migrationTable);
                imported++;
            }
        else
            imported += roleDefinitions.Count;

        var intuneBrandingProfiles = await importService.ReadIntuneBrandingProfilesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in intuneBrandingProfiles)
            {
                await importService.ImportIntuneBrandingProfileAsync(item, migrationTable);
                imported++;
            }
        else
            imported += intuneBrandingProfiles.Count;

        var azureBrandingLocalizations = await importService.ReadAzureBrandingLocalizationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in azureBrandingLocalizations)
            {
                await importService.ImportAzureBrandingLocalizationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += azureBrandingLocalizations.Count;

        var autopilotProfiles = await importService.ReadAutopilotProfilesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in autopilotProfiles)
            {
                await importService.ImportAutopilotProfileAsync(item, migrationTable);
                imported++;
            }
        else
            imported += autopilotProfiles.Count;

        var deviceHealthScripts = await importService.ReadDeviceHealthScriptsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in deviceHealthScripts)
            {
                await importService.ImportDeviceHealthScriptAsync(item, migrationTable);
                imported++;
            }
        else
            imported += deviceHealthScripts.Count;

        var macCustomAttributes = await importService.ReadMacCustomAttributesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in macCustomAttributes)
            {
                await importService.ImportMacCustomAttributeAsync(item, migrationTable);
                imported++;
            }
        else
            imported += macCustomAttributes.Count;

        var featureUpdateProfiles = await importService.ReadFeatureUpdateProfilesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in featureUpdateProfiles)
            {
                await importService.ImportFeatureUpdateProfileAsync(item, migrationTable);
                imported++;
            }
        else
            imported += featureUpdateProfiles.Count;

        var namedLocations = await importService.ReadNamedLocationsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in namedLocations)
            {
                await importService.ImportNamedLocationAsync(item, migrationTable);
                imported++;
            }
        else
            imported += namedLocations.Count;

        var authenticationStrengthPolicies = await importService.ReadAuthenticationStrengthPoliciesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in authenticationStrengthPolicies)
            {
                await importService.ImportAuthenticationStrengthPolicyAsync(item, migrationTable);
                imported++;
            }
        else
            imported += authenticationStrengthPolicies.Count;

        var authenticationContexts = await importService.ReadAuthenticationContextsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in authenticationContexts)
            {
                await importService.ImportAuthenticationContextAsync(item, migrationTable);
                imported++;
            }
        else
            imported += authenticationContexts.Count;

        var termsOfUseAgreements = await importService.ReadTermsOfUseAgreementsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in termsOfUseAgreements)
            {
                await importService.ImportTermsOfUseAgreementAsync(item, migrationTable);
                imported++;
            }
        else
            imported += termsOfUseAgreements.Count;

        var deviceManagementScripts = await importService.ReadDeviceManagementScriptsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in deviceManagementScripts)
            {
                await importService.ImportDeviceManagementScriptAsync(item, migrationTable);
                imported++;
            }
        else
            imported += deviceManagementScripts.Count;

        var deviceShellScripts = await importService.ReadDeviceShellScriptsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in deviceShellScripts)
            {
                await importService.ImportDeviceShellScriptAsync(item, migrationTable);
                imported++;
            }
        else
            imported += deviceShellScripts.Count;

        var complianceScripts = await importService.ReadComplianceScriptsFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in complianceScripts)
            {
                await importService.ImportComplianceScriptAsync(item, migrationTable);
                imported++;
            }
        else
            imported += complianceScripts.Count;

        var qualityUpdateProfiles = await importService.ReadQualityUpdateProfilesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in qualityUpdateProfiles)
            {
                await importService.ImportQualityUpdateProfileAsync(item, migrationTable);
                imported++;
            }
        else
            imported += qualityUpdateProfiles.Count;

        var driverUpdateProfiles = await importService.ReadDriverUpdateProfilesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in driverUpdateProfiles)
            {
                await importService.ImportDriverUpdateProfileAsync(item, migrationTable);
                imported++;
            }
        else
            imported += driverUpdateProfiles.Count;

        var settingsCatalogPolicies = await importService.ReadSettingsCatalogPoliciesFromFolderAsync(folder);
        if (!dryRun)
            foreach (var item in settingsCatalogPolicies)
            {
                await importService.ImportSettingsCatalogPolicyAsync(item, migrationTable);
                imported++;
            }
        else
            imported += settingsCatalogPolicies.Count;

        if (!dryRun)
            await exportService.SaveMigrationTableAsync(migrationTable, folder);

        OutputFormatter.WriteJsonToStdout(new
        {
            result = new CommandResult
            {
                Command = "import",
                Count = imported,
                Path = folder,
                DryRun = dryRun
            },
            migrationTable
        });
    }
}
