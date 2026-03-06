# IntuneCommander — Settings Catalog Editor & OpenIntuneBaseline Integration

## Project Overview

IntuneCommander is a .NET 10 / Avalonia UI desktop application for Microsoft Intune management.
Repository root: `C:\Users\adam_admin\IntuneCommander`

This feature set adds:
- **Full Settings Catalog CRUD** — Update metadata, delete policies, replace settings (with rollback)
- **OpenIntuneBaseline (OIB) integration** — Embedded baseline JSONs for 3 policy types (SC, ES, Compliance) with compare & selective deploy
- **Settings editor** — TreeView + ContentControl with typed ViewModels per Graph setting type
- **Group Picker** — Dialog for assigning policies during deploy
- **Baselines view** — SC / ES / Compliance sub-tabs within a Baselines mode toggle

All Graph endpoints are **beta-only** (`/beta/deviceManagement/configurationPolicies`).

## Issue Tracker

Work is broken into 7 GitHub issues, ordered by dependency:

| Issue | Title | Depends On | Phase |
|-------|-------|------------|-------|
| #1 | SC Service CRUD Extensions | — | 1 |
| #2 | Baseline Models, Service & Fetch Script | — | 2–3 |
| #3 | Setting Editor ViewModel Hierarchy & Factory | — | 4 |
| #4 | Group Picker Dialog | — (deferrable) | 5 |
| #5 | Editor & Baseline Orchestrator ViewModels | #1, #2, #3 | 6 |
| #6 | XAML Views | #3, #4, #5 | 7 |
| #7 | Tests, Documentation & Smoke Verification | #1–#6 | 8–10 |

Issues #1, #2, #3, #4 are independent and can be built in parallel.
Issue #5 is the integration point. Issue #6 wires up the UI. Issue #7 is final verification.

**MVP cut line:** Issues #1–3, #5, #6, #7 (without Group Picker) produce a fully shippable feature.
Issue #4 (Group Picker) is deferrable — deploy without assignments works and users can assign later.

## Known Gaps (Explicit Scope Cuts)

- **ES/Compliance comparison** — `CompareSettingsCatalog` is the only comparison method. ES and Compliance sub-tabs show baselines and deploy them, but have no compare capability. The Compare button/section must be hidden on ES/Compliance sub-tabs.
- **$batch optimization** — `UpdatePolicySettingsAsync` uses sequential DELETE+POST. $batch (up to 20/batch) is a follow-up if performance is unacceptable.
- **ES/Compliance inline editing** — Only Settings Catalog policies get the TreeView editor. ES and Compliance editing is a future feature.
- **"Deploy to Existing" for ES/Compliance** — Only SC supports `UpdatePolicySettingsAsync`. ES and Compliance only support "Deploy as New."

## Existing Services (Already Present — DO NOT Recreate)

| Service | Interface | Methods | Status |
|---------|-----------|---------|--------|
| Settings Catalog | `ISettingsCatalogService` | List, Get, Create, GetAssignments, Assign, GetPolicySettings | **Needs +3 (Update, Delete, UpdateSettings)** |
| Endpoint Security | `IEndpointSecurityService` | List, Get, Create, Update, Delete, GetAssignments, Assign | Complete |
| Compliance | `ICompliancePolicyService` | List, Get, Create, Update, Delete, GetAssignments, Assign | Complete |
| Groups | `IGroupService` | SearchGroupsAsync + others | Complete |

## Existing Import/Export (Already Present — Reuse for Deploy)

| Type | Import Method | ID-Clearing Pattern |
|------|--------------|-------------------|
| Settings Catalog | `ImportSettingsCatalogPolicyAsync` | Clear IDs → Create → optionally Assign |
| Endpoint Security | `ImportEndpointSecurityIntentAsync` | Clear IDs → Create → optionally Assign |
| Compliance | `ImportCompliancePolicyAsync` | Clear IDs → Create → optionally Assign |

## Architecture Patterns (Follow Existing Conventions)

### Service Layer
- Services implement interfaces (`IXxxService`) registered for DI/mocking
- `GraphPatchHelper.PatchWithGetFallbackAsync` handles PATCH-returns-null quirk
- `CancellationToken` on all async methods
- `DebugLog` for diagnostic logging

### Embedded Resources
- `SettingsCatalogDefinitionRegistry` uses `Lazy<>` for deferred loading from embedded `.json.gz`
- Same pattern for `BaselineService`

### ViewModels
- `CommunityToolkit.Mvvm` — `[ObservableProperty]`, `[RelayCommand]`, `partial` classes
- `ViewModelBase` is the common base class
- Partial `On{Property}Changed` methods for change tracking

### XAML / Avalonia
- `TreeDataTemplate` for hierarchical TreeView (NOT WPF `HierarchicalDataTemplate`)
- `DataTemplate` with `DataType` for type-discriminated rendering
- `ContentControl Content="{Binding}"` auto-selects DataTemplate by VM type
- **Do NOT use** `Avalonia.Controls.TreeDataGrid` (requires commercial Accelerate license)
- Fluent theme: `Avalonia.Themes.Fluent`

### Graph API (Settings Catalog — All Beta)
- Base: `https://graph.microsoft.com/beta/deviceManagement/configurationPolicies`
- **PATCH only updates metadata** (name, description, roleScopeTagIds), NOT settings
- Settings update: GET existing → DELETE each → POST new (with rollback on failure)
- Assignment POST is full REPLACE (not incremental)
- Filter pure SC policies: `templateReference.templateFamily == "none"` and `technologies has 'mdm'`
- Permission: `DeviceManagementConfiguration.ReadWrite.All`

### Settings Type Hierarchy (Polymorphic)

| @odata.type | Value Property | VM Class |
|-------------|---------------|----------|
| `#...ChoiceSettingInstance` | `choiceSettingValue` | `ChoiceSettingViewModel` |
| `#...ChoiceSettingCollectionInstance` | `choiceSettingCollectionValue` | `ChoiceCollectionSettingViewModel` |
| `#...SimpleSettingInstance` + `StringSettingValue` | `simpleSettingValue` | `SimpleStringSettingViewModel` |
| `#...SimpleSettingInstance` + `IntegerSettingValue` | `simpleSettingValue` | `SimpleIntegerSettingViewModel` |
| `#...SimpleSettingCollectionInstance` | `simpleSettingCollectionValue` | `SimpleCollectionSettingViewModel` |
| `#...GroupSettingInstance` | `groupSettingValue` | `GroupSettingViewModel` |
| `#...GroupSettingCollectionInstance` | `groupSettingCollectionValue` | `GroupSettingViewModel` |
| Unknown | — | `UnknownSettingViewModel` |

Settings nest recursively via `choiceSettingValue.children[]` and `groupSettingValue.children[]`.
OIB BitLocker policies have 3+ nesting levels.

### OIB Naming Convention
`Win - OIB - {Type} - {Category} - {D/U} - {SubCategory}`
- Type: `SC` (Settings Catalog), `ES` (Endpoint Security), `TP` (Template Profile)
- D = Device scope, U = User scope

### OIB JSON Format
OIB Settings Catalog JSONs are **direct Graph API POST payloads**. They include `@odata.type` discriminators and can be sent directly to `POST /beta/deviceManagement/configurationPolicies`.
Store as `JsonElement` in `BaselinePolicy.RawJson` to avoid polymorphic deserialization.

## Verification (Run After Each Issue)

```bash
# Build must succeed
dotnet build

# All unit tests pass
dotnet test --filter "Category!=Integration"

# Specific test classes (when applicable)
dotnet test --filter "FullyQualifiedName~SettingsCatalogServiceTests"
dotnet test --filter "FullyQualifiedName~BaselineServiceTests"
dotnet test --filter "FullyQualifiedName~SettingViewModelFactory"
```
