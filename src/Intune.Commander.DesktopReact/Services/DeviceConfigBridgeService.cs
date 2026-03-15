using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class DeviceConfigBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKey = "DeviceConfigurations";
    private IConfigurationProfileService? _service;

    public DeviceConfigBridgeService(AuthBridgeService authBridge, ICacheService cache, ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IConfigurationProfileService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new ConfigurationProfileService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var service = GetService();
        var tenantId = GetTenantId();

        if (tenantId is not null)
        {
            var cached = _cache.Get<DeviceConfiguration>(tenantId, CacheKey);
            if (cached is { Count: > 0 })
                return await MapList(cached, service);
        }

        var configs = await service.ListDeviceConfigurationsAsync();
        if (tenantId is not null) _cache.Set(tenantId, CacheKey, configs);
        return await MapList(configs, service);
    }

    private async Task<DeviceConfigListItemDto[]> MapList(List<DeviceConfiguration> configs, IConfigurationProfileService service)
    {
        // Fetch assignment counts in parallel
        var sem = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();
        var tasks = configs.Where(c => c.Id is not null).Select(async c =>
        {
            await sem.WaitAsync();
            try
            {
                var assignments = await service.GetAssignmentsAsync(c.Id!);
                lock (counts) counts[c.Id!] = assignments.Count;
            }
            catch { /* skip */ }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);

        return configs.Select(c => new DeviceConfigListItemDto(
            Id: c.Id ?? "",
            DisplayName: c.DisplayName ?? "",
            Description: c.Description,
            ConfigurationType: c.GetType().Name.Replace("DeviceConfiguration", "").Replace("Configuration", ""),
            CreatedDateTime: c.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: c.LastModifiedDateTime?.ToString("o") ?? "",
            AssignmentCount: counts.GetValueOrDefault(c.Id ?? "", 0)
        )).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var service = GetService();
        var client = _authBridge.GraphClient!;

        var configTask = service.GetConfigurationProfileAsync(id);
        var assignmentsTask = service.GetAssignmentsAsync(id);
        await Task.WhenAll(configTask, assignmentsTask);

        var config = await configTask ?? throw new InvalidOperationException($"Config {id} not found");
        var assignments = await assignmentsTask;
        var groupNames = await ResolveGroupNames(assignments.Select(a => a.Target).ToList(), client);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return new DeviceConfigDetailDto(
            Id: config.Id ?? "",
            DisplayName: config.DisplayName ?? "",
            Description: config.Description,
            ConfigurationType: config.GetType().Name,
            CreatedDateTime: config.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: config.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (config.RoleScopeTagIds ?? []).ToArray(),
            Assignments: MapAssignments(assignments.Select(a => a.Target).ToList(), groupNames),
            RawJson: JsonSerializer.Serialize(config, jsonOptions));
    }

    private static async Task<Dictionary<string, string>> ResolveGroupNames(
        List<DeviceAndAppManagementAssignmentTarget?> targets,
        Microsoft.Graph.Beta.GraphServiceClient client)
    {
        var groupIds = targets.OfType<GroupAssignmentTarget>().Select(g => g.GroupId)
            .Concat(targets.OfType<ExclusionGroupAssignmentTarget>().Select(g => g.GroupId))
            .Where(id => id is not null).Distinct().ToList();
        var names = new Dictionary<string, string>();
        var sem = new SemaphoreSlim(5);
        var tasks = groupIds.Select(async gid =>
        {
            await sem.WaitAsync();
            try
            {
                var g = await client.Groups[gid].GetAsync(r => r.QueryParameters.Select = ["displayName"]);
                if (g?.DisplayName is not null) lock (names) names[gid!] = g.DisplayName;
            }
            catch { }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
        return names;
    }

    private static AssignmentDto[] MapAssignments(List<DeviceAndAppManagementAssignmentTarget?> targets, Dictionary<string, string> groupNames)
    {
        return targets.Where(t => t is not null).Select(t => t switch
        {
            AllDevicesAssignmentTarget => new AssignmentDto("All Devices", "Include"),
            AllLicensedUsersAssignmentTarget => new AssignmentDto("All Users", "Include"),
            ExclusionGroupAssignmentTarget excl => new AssignmentDto(
                groupNames.GetValueOrDefault(excl.GroupId ?? "") ?? excl.GroupId ?? "Unknown", "Exclude"),
            GroupAssignmentTarget grp => new AssignmentDto(
                groupNames.GetValueOrDefault(grp.GroupId ?? "") ?? grp.GroupId ?? "Unknown", "Include"),
            _ => new AssignmentDto("Unknown", "Unknown")
        }).ToArray();
    }
}
