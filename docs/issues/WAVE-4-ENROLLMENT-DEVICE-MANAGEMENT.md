# Wave 4: Enrollment and Device Management Services

**Reference:** [docs/SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) — Wave 4

## Scope

Wave 4 focuses on implementing services for device enrollment automation and management:

1. **Autopilot Profiles** — Windows Autopilot deployment profiles
2. **Device Health Scripts** — Proactive remediation scripts (Windows)
3. **Mac Custom Attributes** — Custom attribute shell scripts for macOS
4. **Feature Update Profiles** — Windows feature update deployment profiles

## Service Implementations

### 1. Autopilot Service

**Interface:** `IAutopilotService`  
**Class:** `AutopilotService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/windowsAutopilotDeploymentProfiles`
- Assignments: `/deviceManagement/windowsAutopilotDeploymentProfiles/{id}/assignments`

**Methods to Implement:**
- [ ] `ListWindowsAutopilotDeploymentProfilesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<WindowsAutopilotDeploymentProfile>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetWindowsAutopilotDeploymentProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `WindowsAutopilotDeploymentProfile`
  - Throw if null response
- [ ] `CreateWindowsAutopilotDeploymentProfileAsync(WindowsAutopilotDeploymentProfile profile, CancellationToken cancellationToken = default)`
  - Returns created `WindowsAutopilotDeploymentProfile`
  - Throw if null response
- [ ] `UpdateWindowsAutopilotDeploymentProfileAsync(WindowsAutopilotDeploymentProfile profile, CancellationToken cancellationToken = default)`
  - Returns updated `WindowsAutopilotDeploymentProfile`
  - Throw if null response
- [ ] `DeleteWindowsAutopilotDeploymentProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string profileId, CancellationToken cancellationToken = default)`
  - Returns `List<WindowsAutopilotDeploymentProfileAssignment>`
  - Use manual pagination
- [ ] `AssignProfileAsync(string profileId, List<WindowsAutopilotDeploymentProfileAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`

### 2. Device Health Script Service

**Interface:** `IDeviceHealthScriptService`  
**Class:** `DeviceHealthScriptService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/deviceHealthScripts`
- Assignments: `/deviceManagement/deviceHealthScripts/{id}/assignments`

**Methods to Implement:**
- [ ] `ListDeviceHealthScriptsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceHealthScript>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetDeviceHealthScriptAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `DeviceHealthScript`
  - Throw if null response
- [ ] `CreateDeviceHealthScriptAsync(DeviceHealthScript script, CancellationToken cancellationToken = default)`
  - Returns created `DeviceHealthScript`
  - Throw if null response
- [ ] `UpdateDeviceHealthScriptAsync(DeviceHealthScript script, CancellationToken cancellationToken = default)`
  - Returns updated `DeviceHealthScript`
  - Throw if null response
- [ ] `DeleteDeviceHealthScriptAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string scriptId, CancellationToken cancellationToken = default)`
  - Returns `List<DeviceHealthScriptAssignment>`
  - Use manual pagination
- [ ] `AssignScriptAsync(string scriptId, List<DeviceHealthScriptAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`

### 3. Mac Custom Attribute Service

**Interface:** `IMacCustomAttributeService`  
**Class:** `MacCustomAttributeService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/deviceCustomAttributeShellScripts`
- Assignments: `/deviceManagement/deviceCustomAttributeShellScripts/{id}/assignments`

**Methods to Implement:**
- [ ] `ListDeviceCustomAttributeShellScriptsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceCustomAttributeShellScript>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetDeviceCustomAttributeShellScriptAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `DeviceCustomAttributeShellScript`
  - Throw if null response
- [ ] `CreateDeviceCustomAttributeShellScriptAsync(DeviceCustomAttributeShellScript script, CancellationToken cancellationToken = default)`
  - Returns created `DeviceCustomAttributeShellScript`
  - Throw if null response
- [ ] `UpdateDeviceCustomAttributeShellScriptAsync(DeviceCustomAttributeShellScript script, CancellationToken cancellationToken = default)`
  - Returns updated `DeviceCustomAttributeShellScript`
  - Throw if null response
- [ ] `DeleteDeviceCustomAttributeShellScriptAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string scriptId, CancellationToken cancellationToken = default)`
  - Returns `List<DeviceCustomAttributeShellScriptAssignment>`
  - Use manual pagination
- [ ] `AssignScriptAsync(string scriptId, List<DeviceCustomAttributeShellScriptAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`

### 4. Feature Update Service

**Interface:** `IFeatureUpdateService`  
**Class:** `FeatureUpdateService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/windowsFeatureUpdateProfiles`
- Assignments: `/deviceManagement/windowsFeatureUpdateProfiles/{id}/assignments`

**Methods to Implement:**
- [ ] `ListWindowsFeatureUpdateProfilesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<WindowsFeatureUpdateProfile>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetWindowsFeatureUpdateProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `WindowsFeatureUpdateProfile`
  - Throw if null response
- [ ] `CreateWindowsFeatureUpdateProfileAsync(WindowsFeatureUpdateProfile profile, CancellationToken cancellationToken = default)`
  - Returns created `WindowsFeatureUpdateProfile`
  - Throw if null response
- [ ] `UpdateWindowsFeatureUpdateProfileAsync(WindowsFeatureUpdateProfile profile, CancellationToken cancellationToken = default)`
  - Returns updated `WindowsFeatureUpdateProfile`
  - Throw if null response
- [ ] `DeleteWindowsFeatureUpdateProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string profileId, CancellationToken cancellationToken = default)`
  - Returns `List<WindowsFeatureUpdateProfileAssignment>`
  - Use manual pagination
- [ ] `AssignProfileAsync(string profileId, List<WindowsFeatureUpdateProfileAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`

## Scaffolding Steps

### Core Service Setup
- [ ] Add interface files in `src/IntuneManager.Core/Services/`
- [ ] Add implementation classes in `src/IntuneManager.Core/Services/`
- [ ] Ensure constructors accept `GraphServiceClient`
- [ ] Add all service methods with proper signatures
- [ ] Implement manual `@odata.nextLink` pagination in all list methods
- [ ] Ensure all models use `Microsoft.Graph.Beta.Models` types
- [ ] Add null checks and throw on null responses for create/update/get

### Export/Import Integration
- [ ] Extend `ExportService` to handle Wave 4 objects
- [ ] Extend `ImportService` to handle Wave 4 objects
- [ ] Create export wrapper models for objects with assignments
- [ ] Add subfolder paths for each type in export structure
- [ ] Maintain migration-table compatibility
- [ ] Consider script content encoding/decoding for health scripts and Mac scripts

### Desktop UI Integration
- [ ] Add collections in `MainWindowViewModel` for each type
- [ ] Add selection properties for each type
- [ ] Add DataGrid column configurations
- [ ] Add navigation category entries (possibly under "Device Management")
- [ ] Implement lazy-load handlers
- [ ] Add cache invalidation keys
- [ ] Wire up export/import commands for new types

### Testing
- [ ] Add `AutopilotServiceTests.cs` in `tests/IntuneManager.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations
- [ ] Add `DeviceHealthScriptServiceTests.cs` in `tests/IntuneManager.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations
  - Test script content handling
- [ ] Add `MacCustomAttributeServiceTests.cs` in `tests/IntuneManager.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations
  - Test script content handling
- [ ] Add `FeatureUpdateServiceTests.cs` in `tests/IntuneManager.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations

## Definition of Done

- [ ] All services compile without errors
- [ ] Unit tests added and passing for all services
- [ ] Manual pagination used on every list method
- [ ] CancellationToken passed to all Graph API calls
- [ ] No UI-thread sync blocking introduced
- [ ] Services consumed by desktop ViewModel
- [ ] Export/Import functionality working for all types
- [ ] UI displays and allows interaction with all new types

## Notes

- Autopilot profiles may have different subtypes (User-driven, Self-deploying, Pre-provisioned)
- Device Health Scripts contain PowerShell script content that needs proper encoding
- Mac Custom Attributes contain shell script content that needs proper encoding
- Feature Update Profiles control Windows 10/11 feature update deployment
- Script content should be base64-encoded in Graph API calls — verify encoding requirements
- Consider adding script validation or preview capabilities in UI
