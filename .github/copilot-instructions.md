# Copilot Instructions

## Project Overview
Intune Commander is a .NET 10 / Avalonia UI desktop app for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD clouds. It's a ground-up remake of a PowerShell/WPF tool — the migration to compiled .NET specifically targets UI deadlocks and threading issues.

## Build & Test
```bash
dotnet build                                                    # Build all projects
dotnet test --filter "Category!=Integration"                    # Unit tests only (no credentials needed)
dotnet test --filter "FullyQualifiedName~ProfileServiceTests"   # Single test class
dotnet test --filter "Category=Integration"                     # Integration tests (needs AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET)
dotnet test /p:CollectCoverage=true /p:Threshold=40 /p:ThresholdType=line /p:ThresholdStat=total  # With 40% coverage gate
dotnet run --project src/IntuneManager.Desktop                  # Launch the app
```

## Technology
- Runtime: .NET 10, C# 12
- UI: Avalonia 11.3.x (`.axaml` files), CommunityToolkit.Mvvm 8.2.x
- Auth: Azure.Identity 1.17.x
- **Graph API: `Microsoft.Graph.Beta` 5.x-preview** — NOT the stable `Microsoft.Graph` package. All models and `GraphServiceClient` come from `Microsoft.Graph.Beta.*`.
- Cache: LiteDB 5.0.x (AES-encrypted), Charts: LiveChartsCore.SkiaSharpView.Avalonia

## Architecture
- **Two-project solution**: `Intune.Commander.Core` (class library: auth, services, models) and `Intune.Commander.Desktop` (Avalonia UI app).
- **MVVM with CommunityToolkit.Mvvm**: Use `[ObservableProperty]`, `[RelayCommand]`, and `partial` classes. ViewModels extend `ViewModelBase` which provides `IsBusy`/`ErrorMessage`.
- **DI setup**: Services registered in `ServiceCollectionExtensions.AddIntuneCommanderCore()`. Desktop-layer ViewModels registered in `App.axaml.cs`. Graph-dependent services (e.g., `ConfigurationProfileService`) are created manually after auth, not via DI.
- **ViewLocator pattern**: Avalonia resolves Views from ViewModels by naming convention (`FooViewModel` → `FooView`).

**Two projects:** `IntuneManager.Core` (class library) and `IntuneManager.Desktop` (Avalonia app).

**DI setup:** `App.axaml.cs` calls `services.AddIntuneManagerCore()` then registers `MainWindowViewModel` as transient.
- Singletons: `IAuthenticationProvider`, `IntuneGraphClientFactory`, `ProfileService`, `IProfileEncryptionService`, `ICacheService`
- Transient: `IExportService`
- **Graph API services are NOT in DI.** After login, `MainWindowViewModel` creates them with `new XxxService(graphClient)` — all `I*Service` fields in the VM are nullable until `ConnectAsync` runs.

**MVVM:** ViewModels are `partial class` extending `ViewModelBase` (provides `IsBusy`, `ErrorMessage`, `DebugLog`). Use `[ObservableProperty]` and `[RelayCommand]`. Avalonia's ViewLocator resolves `FooViewModel` → `FooView` by naming convention.

**`MainWindowViewModel` is split into partial classes** by concern: `.Connection.cs`, `.Loading.cs`, `.Navigation.cs`, `.Selection.cs`, `.Detail.cs`, `.ExportImport.cs`, `.Search.cs`, `.AppAssignments.cs`, `.ConditionalAccessExport.cs`. The VM holds 30+ `ObservableCollection<T>` properties (one per Intune type). Navigation is driven by `NavCategories`; each category loads lazily via `_*Loaded` boolean flags and `ICacheService`.

**Auth/multi-cloud:** `IntuneGraphClientFactory.CreateClientAsync(profile)` builds a `GraphServiceClient` using `Azure.Identity` with the endpoint from `CloudEndpoints.GetEndpoints(cloud)`. GCC-High → `https://graph.microsoft.us`; DoD → `https://dod-graph.microsoft.us`. GCC-High/DoD require separate app registrations.

**Caching:** `CacheService` uses LiteDB at `%LocalAppData%\IntuneManager\cache.db` (AES password in `cache-key.bin` via DataProtection). Entries keyed by tenant ID + type string, 24-hour TTL.

**Profile storage:** `ProfileService` writes `%LocalAppData%\IntuneManager\profiles.json`. Encrypted files are prefixed with `INTUNEMANAGER_ENC:`.

**DebugLogService:** Singleton (`DebugLogService.Instance`), `ObservableCollection<string>` capped at 2000 entries. Access via `ViewModelBase.DebugLog` (protected property). Use `DebugLog.Log(category, message)` / `DebugLog.LogError(...)`. All updates dispatch to UI thread. Observable property updates from background threads must use `Dispatcher.UIThread.Post()`.

## Critical: Async-First UI Rule
- **No `.GetAwaiter().GetResult()`, `.Wait()`, or `.Result` on the UI thread — ever.**
- Fire-and-forget for non-blocking loads: `_ = LoadProfilesAsync();`
- All `[RelayCommand]` methods returning `Task` get automatic `CancellationToken` support from CommunityToolkit.Mvvm.

## Graph API Pagination — Manual `@odata.nextLink` (REQUIRED)
**Do NOT use `PageIterator`** — it silently truncates results on some tenants. Use manual `while` loop on `OdataNextLink`:
```csharp
var response = await _graphClient.DeviceManagement.DeviceConfigurations
    .GetAsync(req => { req.QueryParameters.Top = 999; }, cancellationToken);

var result = new List<DeviceConfiguration>();
while (response != null)
{
    if (response.Value != null) result.AddRange(response.Value);
    if (!string.IsNullOrEmpty(response.OdataNextLink))
        response = await _graphClient.DeviceManagement.DeviceConfigurations
            .WithUrl(response.OdataNextLink)
            .GetAsync(cancellationToken: cancellationToken);
    else break;
}
```
- Always set `$top=999` on the initial request.
- `.WithUrl(...)` requires `using Microsoft.Kiota.Abstractions;`.
- Apply to **every** service method that lists Graph objects, including `GroupService`.

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
dotnet run --project src/Intune.Commander.Desktop  # Launch the app
```
Tests live in `tests/Intune.Commander.Core.Tests/` — xUnit with `[Fact]`/`[Theory]`, temp directories for file I/O tests, `IDisposable` cleanup. Tests cover models and services in Core only (no UI tests).

**Unit tests are required for all new or changed code.** Every new service, model, or behavioral change in `Intune.Commander.Core` must include corresponding tests. PRs without adequate test coverage will not be merged.

## Adding a New Intune Object Type
1. Create `I{Type}Service` interface in `Core/Services/` following the CRUD + `GetAssignmentsAsync` pattern.
2. Create `{Type}Service` implementation taking `GraphServiceClient`, using manual `@odata.nextLink` pagination for listing (see pagination section above).
3. If assignments are needed, create `{Type}Export` model in `Core/Models/` bundling object + assignments.
4. Add export/import methods to `ExportService`/`ImportService`.
5. Wire into `MainWindowViewModel`: add collection, selection property, column configs, nav category, and load logic.
6. Add tests in `tests/Intune.Commander.Core.Tests/`.