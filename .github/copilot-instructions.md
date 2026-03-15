# Copilot Instructions

## Project Overview
Intune Commander is a .NET 10 / React 19 / WPF+WebView2 Windows desktop app for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD clouds. It's a ground-up remake of a PowerShell/WPF tool — the migration to compiled .NET specifically targets UI deadlocks and threading issues.

## External Documentation
- Use Context7 first for external frameworks, libraries, SDKs, GitHub Actions, and APIs whenever current behavior matters.
- Prefer Context7 for .NET, React, Zustand, Microsoft.Graph.Beta, Azure.Identity, LiteDB, PowerShell modules, and GitHub Actions before relying on memory.
- Skip Context7 only for purely repository-local code or when no relevant library entry exists.

## Critical: Async-First Rule
- **No `.GetAwaiter().GetResult()`, `.Wait()`, or `.Result` calls — ever.** Always `await`.
- All async methods must accept `CancellationToken`.

## Architecture
- **Three-project solution**: `Intune.Commander.Core` (class library: auth, services, models), `Intune.Commander.DesktopReact` (WPF + WebView2 host), and `intune-commander-react` (React 19 + TypeScript frontend).
- **Bridge pattern**: React frontend communicates with .NET via the `ic/1` protocol through `window.chrome.webview.postMessage`. The WPF host's `BridgeRouter` dispatches messages to `IBridgeService` implementations.
- **State management**: Zustand stores in `intune-commander-react/src/store/` — one store per domain.
- **DI setup**: Services registered in `ServiceCollectionExtensions.AddIntuneCommanderCore()`. Bridge services registered in `App.xaml.cs`. Graph-dependent services (e.g., `ConfigurationProfileService`) are created manually after auth, not via DI.

## Service-per-Type Pattern
Each Intune object type gets its own interface + implementation:
- `IConfigurationProfileService` / `ConfigurationProfileService` — Device Configurations
- `ICompliancePolicyService` / `CompliancePolicyService` — Compliance Policies
- `IApplicationService` / `ApplicationService` — Applications (read-only)

All take `GraphServiceClient` in constructor, use manual `@odata.nextLink` pagination (see below), accept `CancellationToken`, and return `List<T>`.

## Graph API Pagination — Manual `@odata.nextLink` (REQUIRED)
**Do NOT use `PageIterator`** — it silently truncates results on some tenants. All Graph list operations must use manual `while` loop pagination following `OdataNextLink`:
```csharp
var response = await _graphClient.DeviceAppManagement.MobileApps
    .GetAsync(req =>
    {
        req.QueryParameters.Top = 200;
        // other query params...
    }, cancellationToken);

var result = new List<MobileApp>();
while (response != null)
{
    if (response.Value != null)
        result.AddRange(response.Value);

    if (!string.IsNullOrEmpty(response.OdataNextLink))
    {
        response = await _graphClient.DeviceAppManagement.MobileApps
            .WithUrl(response.OdataNextLink)
            .GetAsync(cancellationToken: cancellationToken);
    }
    else
    {
        break;
    }
}
```
Key rules:
- Always set `$top=999` on the initial request.
- Use `.WithUrl(response.OdataNextLink)` (requires `using Microsoft.Kiota.Abstractions;`) to follow next pages.
- Apply this pattern to **every** service method that lists Graph objects, including `GroupService`.

## Key Conventions
- **Graph SDK models used directly** — no wrapper DTOs. Types like `DeviceConfiguration`, `MobileApp` come from `Microsoft.Graph.Models`.
- **Export format**: Subfolder-per-type (`DeviceConfigurations/`, `CompliancePolicies/`, `Applications/`) with `migration-table.json` for ID mappings. Must maintain read compatibility with the original PowerShell tool's JSON format.
- **Export wrappers** for types with assignments: `CompliancePolicyExport`, `ApplicationExport` bundle the object + its assignments list.
- **Profile storage**: Encrypted JSON at `%LOCALAPPDATA%\Intune.Commander\profiles.json` using `Microsoft.AspNetCore.DataProtection`. Marker prefix `INTUNEMANAGER_ENC:` distinguishes encrypted from plain files (marker preserved as compatibility constant).
- **Multi-cloud**: `CloudEndpoints.GetEndpoints(cloud)` returns `(graphBaseUrl, authorityHost)` tuple. Separate app registrations per cloud.
- **Computed columns**: DataGrid uses `DataGridColumnConfig` with `"Computed:"` prefix in `BindingPath` for values derived in code-behind (e.g., platform inferred from OData type).

## Git Workflow
- **Never commit directly to `main`.** All changes must go through a feature branch and pull request.
- Branch naming: `feature/`, `fix/`, `docs/` prefixes (e.g. `feature/wave7-scripts`, `fix/lazy-load-guard`).
- PRs should be created with `gh pr create` and submitted for Copilot / human review before merging.

## Build & Test
```bash
dotnet build                                    # Build all projects
dotnet test                                     # Run xUnit tests
dotnet run --project src/Intune.Commander.DesktopReact  # Launch the app
```
Tests live in `tests/Intune.Commander.Core.Tests/` — xUnit with `[Fact]`/`[Theory]`, temp directories for file I/O tests, `IDisposable` cleanup. Tests cover models and services in Core only (no UI tests).

**Unit tests are required for all new or changed code.** Every new service, model, or behavioral change in `Intune.Commander.Core` must include corresponding tests. PRs without adequate test coverage will not be merged.

## Adding a New Intune Object Type
1. Create `I{Type}Service` interface in `Core/Services/` following the CRUD + `GetAssignmentsAsync` pattern.
2. Create `{Type}Service` implementation taking `GraphServiceClient`, using manual `@odata.nextLink` pagination for listing (see pagination section above).
3. If assignments are needed, create `{Type}Export` model in `Core/Models/` bundling object + assignments.
4. Add export/import methods to `ExportService`/`ImportService`.
5. Add tests in `tests/Intune.Commander.Core.Tests/`.

## Adding a New Desktop UI Workspace
1. **React side**: Create a component in `intune-commander-react/src/components/workspace/`, a Zustand store in `src/store/`, and TypeScript types.
2. **Bridge side**: Add a bridge service in `src/Intune.Commander.DesktopReact/Services/` implementing `IBridgeService`.
3. **Register**: Wire the bridge service into `BridgeRouter` and add navigation in the React shell.
See existing workspaces (Settings Catalog, Detection & Remediation) for the full pattern.
