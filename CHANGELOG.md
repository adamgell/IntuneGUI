# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

## [0.4.7.0] — 2026-03-17

### Added
- **Groups workspace** — full list + detail workspace under the new Admin tab with DataGrid, type filter chips (Dynamic Device/User/Assigned), membership rule viewer, members table with type icons, and Intune assignments table. Backend uses parallel `Task.WhenAll` for counts + members + assignments with 1-hour detail cache.
- **Admin primary tab** — new top-level navigation tab with Tenant Admin and Groups sidebar items.
- **10+ new React workspace components** — Applications, Compliance Policy, Conditional Access, Device Config, Endpoint Security, Enrollment, Scripts Hub, Security Posture, Assignment Explorer, Policy Comparison workspaces with corresponding Zustand stores, TypeScript types, and .NET bridge services.
- **6 new Application sub-workspaces** — App Assignments (flat denormalized view), Bulk App Assignments, App Protection Policies, Managed Device App Configurations, Targeted Managed App Configurations, VPP Tokens.
- **ApplicationDataMapper** — extracted app type/platform/version/bundle helpers from ApplicationBridgeService into a shared static class for reuse across application workspaces.
- **DialogBridgeService** — native folder/file/save picker bridge commands (`dialog.pickFolder`, `dialog.pickFile`, `dialog.saveFile`) with UI thread marshaling and 5-minute React timeout.
- **ICacheService.GetSingle/SetSingle** — new single-object cache overloads eliminating the `List<T>` wrapper hack for detail caching.
- **MonacoDiffViewer** — extracted reusable diff component from PolicyComparisonWorkspace for use by Drift Detection and future workspaces.
- **Operations tab additions** — Drift Detection and Export/Import sidebar items with placeholder workspaces ready for buildout.
- **React UI workspaces expansion plan** — comprehensive planning documentation (`PLAN-react-workspaces.md`, `REMAINING-WORK.md`) outlining phased workspace buildout.
- **Application assignment search integration** — global search now includes application assignment matches in results.
- **Dynamic bridge command timeouts** — `HEAVY_COMMANDS` (60s) for multi-Graph-call commands and `LONG_RUNNING_COMMANDS` (180s) for bulk operations.

### Changed
- **Installer packaging** — added packaging details for Intune Commander MSI/MSIX builds.

## [0.4.6.2] — 2026-03-16

### Fixed
- **App showed a white box when launched from MSI install.** Two root causes: WebView2 couldn't create its data directory inside `C:\Program Files\` (no write access for standard users), and Vite's ES module script tags fail under a `file://` origin. Both are now resolved — the app navigates via a virtual host (`https://app.intunecommander.local`) and stores its WebView2 data in `%LocalAppData%\Intune.Commander\WebView2`.
- **Second instance crashed instead of starting.** When a second copy of the app was launched while another was already running, both tried to open the same `cache.db` file exclusively. The app now detects this, falls back to a no-op cache, and shows a clear warning dialog instead of crashing.

### Added
- **ARM64 installers.** Both MSI and MSIX are now built for ARM64 alongside x64, and are published in every release.
- **Simplified installer toolchain.** The WiX installer has been replaced with Master Packager Dev (mpdev), which produces both MSI and MSIX from a single JSON package definition.

### Updated
- OIB (Operating System Intune Baselines) definitions updated to March 2026.

---

## [0.4.5] — 2026-03-14

### Added

- **BaselineService & Settings Catalog CRUD** — new services for managing Settings Catalog definitions and OIB baseline automation
- **Settings Catalog Editor** — cherry-picked core improvements for settings catalog editing workflows
- **React UI Port** — new `Intune.Commander.DesktopReact` project with WPF+WebView2 host, featuring:
  - Settings Catalog workspace with master-detail layout
  - Overview dashboard with detection & remediation workspace
  - Global search, cache sync, MUI DataGrid, and dev WebSocket bridge
- **Directory Object Resolver** — new `IDirectoryObjectResolver` / `DirectoryObjectResolver` in Core
  - Batch-resolves directory object GUIDs to display names via `POST /directoryObjects/getByIds` (up to 1,000 IDs per call)
  - Handles Users, Groups, Directory Roles, Role Templates, Service Principals, and Applications
  - Automatically filters sentinel values (`All`, `None`, `GuestsOrExternalUsers`, etc.) to avoid unnecessary API calls
  - Checks `WellKnownAppRegistry` for Microsoft first-party app IDs before hitting the Graph API
- **Well-Known App Registry** — new `WellKnownAppRegistry` static class in Core/Models
  - Lazy-loads 4,354 Microsoft first-party Entra ID application entries from embedded `MicrosoftApps.json`
  - Case-insensitive GUID lookup with `WellKnownAppRegistry.Resolve(appId)` convenience method
  - Replaces the hardcoded ~30-entry `WellKnownApps` dictionary previously in `MainWindowViewModel.Detail.cs`
  - JSON file is an embedded resource — update by simply swapping the file, no code changes needed
- **CA PowerPoint GUID-to-Name Resolution** — GUIDs in Conditional Access PowerPoint exports are now resolved to human-readable names
  - `AssignedUserWorkload`: resolves user, group, directory role, and service principal GUIDs in include/exclude lists
  - `AssignedCloudAppAction`: resolves application GUIDs in include/exclude lists
  - `ConditionLocations`: resolves named location GUIDs; sentinel values (`All` → "Any location", `AllTrusted` → "All trusted locations") preserved
  - `ConditionalAccessPptExportService`: collects all GUIDs across all policies, performs a single batch resolution, and passes the lookup to all helper constructors
  - 24 new unit tests covering name resolution across all helper classes, `WellKnownAppRegistry`, and `DirectoryObjectResolver` contract

### Changed

- `ConditionalAccessPptExportService` constructor now accepts an optional `IDirectoryObjectResolver` for batch GUID resolution
- `AssignedUserWorkload`, `AssignedCloudAppAction`, and `ConditionLocations` constructors accept an optional `IReadOnlyDictionary<string, string>` name lookup
- Desktop `MainWindowViewModel.Detail.cs` now delegates app-ID resolution to the shared `WellKnownAppRegistry` instead of a local dictionary

---

### Added

- **Permission Check Service** — new `IPermissionCheckService` / `PermissionCheckService` in Core
  - Acquires the current token via `TokenCredential`, base64url-decodes the JWT payload, and compares granted permissions against the 14 known-required Graph scopes
  - Supports both application tokens (`roles` claim) and delegated tokens (`scp` claim)
  - Returns a `PermissionCheckResult` model with `GrantedPermissions`, `MissingPermissions`, `ExtraPermissions`, `AllPermissionsGranted`, and `ClaimSource`
  - Fire-and-forget check fired immediately after successful tenant connection; result logged to Debug Log and stored on `MainWindowViewModel.LastPermissionCheckResult`
  - 15 unit tests covering JWT decoding, classification, interface contract, and edge cases (empty/malformed JWT, no permission claim)
- **Permissions Window** — new non-modal `PermissionsWindow` displaying:
  - Summary pill: green "All 15/15 Granted" or red "N/15 Granted"
  - Token type badge (`roles` = application / `scp` = delegated)
  - Missing permissions section (red ✗, only visible when there are gaps)
  - Granted permissions section (green ✓)
  - Collapsible Extra permissions expander
  - "Not connected" placeholder when no check result is available
  - Accessible via **Help → Permissions...** menu item (disabled when not connected)
- Added `IntuneGraphClientFactory.CreateClientWithCredentialAsync` — returns a `(GraphServiceClient, TokenCredential, string[])` tuple so the credential used for authentication can be reused by `PermissionCheckService` without a second auth round-trip
- Added three AXAML value converters in `Converters/PermissionConverters.cs`:
  - `PermissionSummaryBrushConverter` — `bool → SolidColorBrush` (green / red)
  - `PermissionSummaryTextConverter` — `PermissionCheckResult → "All N/N Granted"` summary string
  - `CountGreaterThanZeroConverter` — `int → bool` for conditional visibility bindings

### Changed

- Moved **Permissions** toolbar button into the **Help** menu as "🔑 Permissions..." to reduce toolbar clutter; item is disabled when not connected

### Fixed

- **Settings Catalog HTTP 500** — Cosmos DB skip-token cursor failures caused by over-large page requests
  - Reduced `$top` from 999 → 100 (more stable for Cosmos-backed stores)
  - Added retry loop with exponential backoff (2 s / 4 s) for transient 500 errors
  - Returns partial results instead of throwing on retry exhaustion
- **Quality Update Profiles HTTP 400** — `$top=999` exceeded the endpoint's hard cap of 200; reduced to `$top=200`
- **Driver Update Profiles HTTP 400** — same fix as Quality Update Profiles

### Documentation

- Updated `docs/GRAPH-PERMISSIONS.md`:
  - Added "Windows 365 — Cloud PC" section (`CloudPC.ReadWrite.All` permission + notes on Windows 365 licence requirement)
  - Added 9 previously missing service rows to the endpoint permission table (QualityUpdate, DriverUpdate, DeviceShellScript, ComplianceScript, AdmxFile, AppleDep, DeviceCategory, CloudPcProvisioning, CloudPcUserSettings)
  - Expanded existing permission rows to document all services that rely on each scope
- Updated `scripts/Setup-IntegrationTestApp.ps1`: added `CloudPC.ReadWrite.All` to `$requiredPermissions`

---

### Added

- **Conditional Access PowerPoint Export** (Phase 1-5 complete)
  - New service: `IConditionalAccessPptExportService` / `ConditionalAccessPptExportService`
  - Generates comprehensive PowerPoint presentations with:
    - Cover slide with tenant name and timestamp
    - Tenant summary with policy counts
    - Policy inventory table (all policies)
    - Per-policy detail slides (conditions, grant controls, assignments)
  - UI integration: Export button visible in Conditional Access category
  - File save dialog with timestamped default filename
  - Async export with cancellation support and progress feedback
  - 11 comprehensive unit tests (parameter validation, file creation, PPTX structure)
  - Commercial cloud support (v1); GCC/GCC-High/DoD deferred to future release
- Added Syncfusion.Presentation.Net.Core v28.1.33 dependency for PowerPoint generation
- Added Syncfusion license initialization via `SYNCFUSION_LICENSE_KEY` environment variable
- Updated SERVICE-IMPLEMENTATION-PLAN.md with Wave 6 (CA PowerPoint Export Integration)
- Documented Syncfusion licensing requirements in README.md

## [2026-02-18 Release]

### Added

- Added new Graph-backed services and interfaces for:
  - Conditional Access Policies (`IConditionalAccessPolicyService`, `ConditionalAccessPolicyService`)
  - Assignment Filters (`IAssignmentFilterService`, `AssignmentFilterService`)
  - Policy Sets (`IPolicySetService`, `PolicySetService`)
- Added manual `@odata.nextLink` pagination patterns to newly introduced list operations.
- Added new navigation categories and table wiring for:
  - Conditional Access
  - Assignment Filters
  - Policy Sets
- Added dedicated detail-pane sections for the new categories in the desktop UI.
- Added cache-first support for the new categories, including:
  - cache keys
  - lazy load flags
  - cache restore on connect
  - cache persistence on successful loads
- Added asynchronous lazy loading for the new categories when navigating to their tabs.
- Added `HumanDateTimeConverter` and applied it to detail-pane timestamp displays.
- Added Wave 4–5 Graph-backed services and interfaces for:
  - Windows Autopilot deployment profiles
  - Device Health Scripts
  - macOS Custom Attributes
  - Feature Update Profiles
  - Named Locations
  - Authentication Strength policies
  - Authentication Context references
  - Terms of Use agreements
- Added Wave 4–5 export/import parity coverage in core tests, including new service contract tests.
- Added Graph endpoint audit artifacts:
  - `docs/graph-uri-inventory.csv`
  - `docs/GRAPH_URI_AUDIT.md`
- Added `Micke-K/IntuneManagement` as a Git submodule for endpoint parity and reference.

### Changed

- Rebranded product/user-facing naming to **Intune Commander** across app surfaces and documentation.
- Updated assembly metadata (`AssemblyTitle`, `Product`) to Intune Commander.
- Updated repository URL references to the renamed remote.
- Improved dark/light theme behavior by moving hardcoded UI text/error colors to dynamic theme brushes.
- Tuned left navigation text color for dark mode readability with a dedicated nav text brush.
- Expanded `MainWindowViewModel` category refresh/filter/selection flows to include new service categories.
- Extended `IExportService` / `ExportService` and `IImportService` / `ImportService` with Wave 4–5 object types and migration-map support.
- Updated connection flow to use cache-first behavior for expanded cached dataset coverage.
- Updated refresh behavior to keep new categories lazy (load on selected tab) instead of always eagerly fetching.

### Fixed

- Fixed right-pane “no selection” placeholder logic to account for newly added selection types.
- Fixed XAML issues introduced during detail card expansion:
  - invalid `WrapPanel` size setting (`ItemWidth="Auto"`)
  - invalid `PolicySet` property binding
- Resolved startup/runtime XAML load instability by correcting compile-time XAML errors and validating clean rebuilds.
- Fixed build regressions caused by temporary method-placement/bracing conflicts in `MainWindowViewModel`.
- Fixed remaining Avalonia compiled binding errors in detail panes by replacing invalid `Description` bindings for `NamedLocation` and `Agreement` with valid model properties.

### UX Improvements

- Improved readability of secondary text and error text in dark theme.
- Enhanced new detail cards with richer metadata:
  - Conditional Access: state, timestamps, control indicators
  - Policy Sets: item count, timestamps
- Standardized detail-pane timestamp formatting to human-readable local time.

### Documentation

- Updated branding and links in key project docs (`README.md`, `CLAUDE.md`, planning docs, and Copilot instructions).
- Documented Graph URI inventory and service expansion mapping.

### Build & Validation

- Verified successful desktop project builds after each major implementation wave.
- Validated no remaining compile diagnostics for impacted files at release cut.
- Confirmed full core test pass (`175` passed) via `dotnet test` after Wave 4–5 integration.
