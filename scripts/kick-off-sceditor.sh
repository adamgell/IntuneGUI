#!/usr/bin/env zsh
# ---------------------------------------------------------------------------
# Kicks off Claude Code subagents for the Settings Catalog Editor feature.
# Run from the IntuneCommander repo root.
#
# Batch 1: Issues #162, #163, #164, #165 (independent -- run in parallel)
# Batch 2: Issue #166 (depends on Batch 1)
# Batch 3: Issue #167 (depends on Batch 2)
# Batch 4: Issue #168 (depends on Batch 3)
#
# Usage:
#   ./scripts/kick-off-sceditor.sh --batch 1       # Run all Batch 1 in parallel
#   ./scripts/kick-off-sceditor.sh --issue 162      # Run single issue
# ---------------------------------------------------------------------------
set -euo pipefail

CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
GRAY='\033[0;90m'
RED='\033[0;31m'
NC='\033[0m' # No Color

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMPDIR="${TMPDIR:-/tmp}"

# ---------------------------------------------------------------------------
# Resolve Claude Code CLI
# ---------------------------------------------------------------------------
if command -v claude &>/dev/null; then
    CLAUDE_BIN="$(command -v claude)"
else
    echo -e "${RED}Claude Code CLI not found. Install with: npm install -g @anthropic-ai/claude-code${NC}"
    exit 1
fi
echo -e "${GRAY}Using Claude Code CLI: ${CLAUDE_BIN}${NC}"

# ---------------------------------------------------------------------------
# Prompt definitions (heredocs)
# ---------------------------------------------------------------------------
read -r -d '' PROMPT_162 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #162 (SC Service CRUD Extensions) for the full specification.

Implement Phase 1: Extend ISettingsCatalogService with 3 new methods.

Plan:
1. Add method signatures to ISettingsCatalogService.cs and implementations to SettingsCatalogService.cs
2. Implement UpdatePolicySettingsAsync with the GET-DELETE-POST pattern and best-effort rollback
3. Update SettingsCatalogServiceTests.cs: method count 6 to 9, add 3 contract tests

Key constraints:
- Read the existing SettingsCatalogService.cs first to understand the Graph client patterns, error handling, and DebugLog usage
- Read GraphPatchHelper.cs and reuse PatchWithGetFallbackAsync for the PATCH metadata method
- PATCH only updates metadata (name, description, roleScopeTagIds), NOT settings
- UpdatePolicySettingsAsync must capture originals before deletion for rollback
- If rollback fails, throw AggregateException with both errors
- All methods take CancellationToken
- All endpoints are beta-only

After implementation, run:
  dotnet build
  dotnet test --filter "FullyQualifiedName~SettingsCatalogServiceTests"

Fix any failures before completing.
PROMPT

read -r -d '' PROMPT_163 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #163 (Baseline Models, Service and Fetch Script) for the full specification.

Implement Phases 2-3: BaselinePolicy model, BaselineService, OIB fetch script, and CI workflow.

Plan:
1. Create BaselinePolicy.cs, BaselineComparisonResult.cs, IBaselineService.cs, BaselineService.cs
2. Create 3 placeholder .json.gz assets, update .csproj with EmbeddedResource entries
3. Create Fetch-OibBaselines.ps1 and update-oib-baselines.yml CI workflow

Key constraints:
- Read SettingsCatalogDefinitionRegistry.cs first. BaselineService must follow the same Lazy<> embedded resource pattern
- Read Fetch-SettingsCatalogDefinitions.ps1. The OIB fetch script must mirror this pattern
- Read update-settings-catalog.yml. The CI workflow must mirror this pattern
- RawJson is JsonElement, NOT deserialized SDK models
- 3 separate embedded resources: oib-sc-baselines.json.gz, oib-es-baselines.json.gz, oib-compliance-baselines.json.gz
- Category parsing from OIB naming: "Win - OIB - {Type} - {Category} - {D/U} - {SubCategory}"
- CompareSettingsCatalog matches settingDefinitionId keys, compares leaf values as strings
- Fetch script: PS 5.1-compatible, ASCII-only, downloads from SkipToTheEndpoint/OpenIntuneBaseline
- Fetch script pulls from 3 subdirectories: WINDOWS/Settings Catalog/, WINDOWS/Endpoint Security/, WINDOWS/Compliance Policies/
- Placeholder assets must be valid gzipped JSON arrays (gzip compress "[]")

After implementation, run:
  dotnet build
  dotnet test --filter "FullyQualifiedName~BaselineService"

Fix any failures before completing.
PROMPT

read -r -d '' PROMPT_164 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #164 (Setting Editor ViewModel Hierarchy and Factory) for the full specification.

Implement Phase 4: All Setting ViewModels plus SettingViewModelFactory with recursive dispatch.

Plan:
1. Create all VM files under src/Intune.Commander.Desktop/ViewModels/Settings/
2. Implement SettingViewModelFactory with Create() plus recursive CreateFromInstance()
3. Create SettingViewModelFactoryTests.cs with approximately 10 tests

Key constraints:
- Read ViewModelBase.cs first to understand the base class
- Read SettingsCatalogDefinitionRegistry.cs: use ResolveDisplayName/ResolveDescription for labels, SettingDefinitionEntry.Options for choice options
- Read the Microsoft.Graph.Beta.Models namespace to understand the actual SDK types: DeviceManagementConfigurationSetting, DeviceManagementConfigurationSettingInstance, and the polymorphic value types
- Use CommunityToolkit.Mvvm: [ObservableProperty], [RelayCommand], partial classes
- Factory has two methods: public Create(DeviceManagementConfigurationSetting) and private CreateFromInstance(DeviceManagementConfigurationSettingInstance)
- CreateFromInstance must recurse through choiceSettingValue.Children and groupSettingValue.Children
- Every VM must implement abstract ToGraphSetting() that reconstructs the Graph API object
- Roundtrip test: Create(setting).ToGraphSetting() must produce JSON-equivalent output
- Handle ALL collection variants: ChoiceSettingCollectionInstance, SimpleSettingCollectionInstance
- GroupSettingCollectionInstance maps to GroupSettingViewModel (same VM, iterate collection items)
- Unknown @odata.type results in UnknownSettingViewModel (read-only passthrough)

After implementation, run:
  dotnet build
  dotnet test --filter "FullyQualifiedName~SettingViewModelFactory"

Fix any failures before completing.
PROMPT

read -r -d '' PROMPT_165 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #165 (Group Picker Dialog) for the full specification.

Implement Phase 5: GroupPickerViewModel, GroupSelectionItem, and GroupPickerWindow.axaml.

Plan:
1. Create GroupSelectionItem.cs record and GroupPickerViewModel.cs
2. Create GroupPickerWindow.axaml with search, selection, and assignment target UI
3. Implement BuildAssignments<T>() for all 3 policy types

Key constraints:
- Read IGroupService.cs and GroupService.cs first to understand the existing search API
- Read MainWindowViewModel.Connection.cs to see how _groupService is instantiated
- GroupPickerViewModel uses existing IGroupService.SearchGroupsAsync()
- Dialog: search TextBox (300ms debounce), search results ListBox, selected groups ListBox, IsExclusion toggle per group, All Devices/All Users checkboxes, OK/Cancel
- BuildAssignments<T>() generates typed lists per BaselinePolicyType using the same @odata.type target discriminators
- Use Avalonia Window (not Dialog) shown via ShowDialog<T>() pattern
- CommunityToolkit.Mvvm for all VM attributes

After implementation, run:
  dotnet build

Fix any failures before completing.
PROMPT

read -r -d '' PROMPT_166 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #166 (Editor and Baseline Orchestrator ViewModels) for the full specification.

Implement Phase 6: SettingsPolicyEditorViewModel, BaselineViewModel, SettingsCatalogViewMode enum, and MainWindowViewModel integration.

Plan:
1. Create SettingsCatalogViewMode.cs, SettingsPolicyEditorViewModel.cs, BaselineViewModel.cs
2. Modify MainWindowViewModel.cs: add fields, observable properties, toggle commands
3. Modify MainWindowViewModel.Connection.cs: instantiate baseline services
4. Modify MainWindowViewModel.Detail.cs: add factory methods

Key constraints:
- Read MainWindowViewModel.cs, .Connection.cs, and .Detail.cs first to understand the existing partial class structure, property patterns, and how other detail panels are wired
- Read the completed code from issues #162 (SC CRUD), #163 (BaselineService), #164 (Setting VMs) to understand what is now available
- SettingsPolicyEditorViewModel builds CategoryNodeViewModel tree using SettingViewModelFactory.Create() and SettingsCatalogDefinitionRegistry.Categories
- BaselineViewModel takes 5 service dependencies
- BaselineViewModel.DeployAsNewAsync dispatches by PolicyType to the correct existing service Create method
- Compare button/section must only be active when ActiveBaselineType == SettingsCatalog
- "Deploy to Existing" with UpdatePolicySettingsAsync only available for SC type
- Track HasUnsavedChanges in editor from child VM IsModified states

After implementation, run:
  dotnet build
  dotnet test --filter "Category!=Integration"

Fix any failures before completing.
PROMPT

read -r -d '' PROMPT_167 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #167 (XAML Views) for the full specification.

Implement Phase 7: All XAML views for the settings editor and baseline browser.

Plan:
1. Modify SettingsCatalogDetailPanel.axaml: mode toggle plus baseline sub-tabs plus edit/delete buttons
2. Create SettingsPolicyEditorPanel.axaml: TreeView plus ContentControl with 7 DataTemplates
3. Create GroupPickerWindow.axaml (if issue #165 code exists, otherwise skip)
4. Modify MainWindow.axaml: add editor panel entry with visibility binding

Key constraints:
- Read the existing SettingsCatalogDetailPanel.axaml first to understand current structure and style
- Read MainWindow.axaml to find the correct insertion point (before line 231, the existing detail panel)
- Read all VM files from issues #164 and #166 to ensure DataTemplate DataType attributes match exact class names and namespaces
- Use TreeDataTemplate (NOT WPF HierarchicalDataTemplate)
- ContentControl Content="{Binding}" auto-selects DataTemplate by VM runtime type
- Do NOT use Avalonia.Controls.TreeDataGrid (commercial license)
- DataTemplates: each setting gets DisplayName label, Description tooltip, IsModified visual indicator
- ChoiceCollectionSettingViewModel uses ListBox with SelectionMode="Multiple"
- GroupSettingViewModel uses ItemsControl recursive via ContentControl (depth bounded by data)
- Compare section hidden on ES/Compliance sub-tabs via IsVisible binding
- Editor panel visibility: IsVisible="{Binding ActiveSettingsEditor, Converter={x:Static ObjectConverters.IsNotNull}}"

After implementation, run:
  dotnet build
  dotnet run --project src/Intune.Commander.Desktop

Verify the app launches and the Settings Catalog category renders without crashes.
PROMPT

read -r -d '' PROMPT_168 <<'PROMPT' || true
Read CLAUDE.md in the repo root for project context and architecture patterns. Then read GitHub issue #168 (Tests, Documentation and Smoke Verification) for the full specification.

Implement Phases 8-10: Final test pass, documentation updates, and verification.

Plan:
1. Run full test suite and fix any failures
2. Update docs/GRAPH-PERMISSIONS.md and scripts/Setup-IntegrationTestApp.ps1
3. Add TODO comments for follow-up items in relevant files

Key constraints:
- Run dotnet test --filter "Category!=Integration" and fix ALL failures before proceeding
- Verify coverage >= 40% with /p:CollectCoverage=true /p:Threshold=40
- Update GRAPH-PERMISSIONS.md: add DeviceManagementConfiguration.ReadWrite.All for SC write operations
- Update Setup-IntegrationTestApp.ps1: ensure app registration includes write permissions
- Add TODO comments in these locations:
  - BaselineService.cs: "TODO: Implement ES/Compliance comparison"
  - UpdatePolicySettingsAsync: "TODO: Consider $batch optimization (20 requests/batch)"
  - BaselineViewModel.cs: "TODO: ES/Compliance inline editing"
  - BaselineViewModel.cs: "TODO: Deploy to Existing for ES/Compliance types"
- Do not add TODOs elsewhere. Keep them focused on the known gaps from CLAUDE.md

After implementation, run:
  dotnet build
  dotnet test --filter "Category!=Integration" /p:CollectCoverage=true /p:Threshold=40 /p:ThresholdType=line /p:ThresholdStat=total
  dotnet test --filter "FullyQualifiedName~SettingsCatalogServiceTests"
  dotnet test --filter "FullyQualifiedName~BaselineServiceTests"
  dotnet test --filter "FullyQualifiedName~SettingViewModelFactory"

All must pass with zero failures.
PROMPT

# ---------------------------------------------------------------------------
# Get prompt by issue number
# ---------------------------------------------------------------------------
get_prompt() {
    local issue="$1"
    case "$issue" in
        162) echo "$PROMPT_162" ;;
        163) echo "$PROMPT_163" ;;
        164) echo "$PROMPT_164" ;;
        165) echo "$PROMPT_165" ;;
        166) echo "$PROMPT_166" ;;
        167) echo "$PROMPT_167" ;;
        168) echo "$PROMPT_168" ;;
        *)   echo ""; return 1 ;;
    esac
}

# ---------------------------------------------------------------------------
# Launch a single Claude Code subagent in the background
# ---------------------------------------------------------------------------
declare -A CLAUDE_PIDS
declare -A CLAUDE_LOGS

start_claude_subagent() {
    local issue="$1"
    local prompt
    prompt="$(get_prompt "$issue")"
    if [[ -z "$prompt" ]]; then
        echo -e "${RED}No prompt defined for issue #${issue}${NC}"
        return 1
    fi

    echo -e "\n${GREEN}>> Starting Claude Code subagent for Issue #${issue}...${NC}"

    local prompt_file="${TMPDIR}/claude-prompt-${issue}.txt"
    local log_file="${TMPDIR}/claude-output-${issue}.log"
    printf '%s' "$prompt" > "$prompt_file"

    # Run claude in background, capture output to log file
    (cd "$REPO_ROOT" && "$CLAUDE_BIN" -p "$(cat "$prompt_file")" --dangerously-skip-permissions > "$log_file" 2>&1) &
    local pid=$!
    CLAUDE_PIDS[$issue]=$pid
    CLAUDE_LOGS[$issue]=$log_file

    echo -e "${GRAY}   PID ${pid} started for Issue #${issue} (log: ${log_file})${NC}"
}

# ---------------------------------------------------------------------------
# Wait for a batch of subagents to complete
# ---------------------------------------------------------------------------
wait_claude_batch() {
    local issues=("$@")
    local pids_display=""
    for issue in "${issues[@]}"; do
        pids_display+="PID ${CLAUDE_PIDS[$issue]} (#${issue})  "
    done
    echo -e "\n${YELLOW}Waiting for: ${pids_display}${NC}"

    local failed=()
    for issue in "${issues[@]}"; do
        local pid="${CLAUDE_PIDS[$issue]}"
        local log="${CLAUDE_LOGS[$issue]}"

        if wait "$pid"; then
            echo -e "\n${CYAN}========== Issue #${issue} (OK) ==========${NC}"
        else
            echo -e "\n${RED}========== Issue #${issue} (FAILED, exit $?) ==========${NC}"
            failed+=("$issue")
        fi

        # Print output
        if [[ -f "$log" ]]; then
            cat "$log"
        fi

        # Clean up
        rm -f "${TMPDIR}/claude-prompt-${issue}.txt" "$log"
    done

    if [[ ${#failed[@]} -gt 0 ]]; then
        echo -e "\n${RED}WARNING: The following issues FAILED: ${failed[*]}${NC}"
    fi
}

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
BATCH=""
ISSUE=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --batch|-b)
            BATCH="$2"; shift 2 ;;
        --issue|-i)
            ISSUE="$2"; shift 2 ;;
        *)
            echo "Unknown argument: $1"; exit 1 ;;
    esac
done

# No args -- show usage
if [[ -z "$BATCH" && -z "$ISSUE" ]]; then
    echo -e "${CYAN}"
    cat <<'EOF'

Settings Catalog Editor -- Claude Code Subagent Launcher
========================================================

Batch 1 (parallel):   #162  #163  #164  #165
Batch 2 (sequential): #166
Batch 3 (sequential): #167
Batch 4 (sequential): #168

Usage:
  ./scripts/kick-off-sceditor.sh --batch 1       # Run all Batch 1 in parallel
  ./scripts/kick-off-sceditor.sh --issue 162      # Run single issue

EOF
    echo -e "${NC}"
    exit 0
fi

# ---------------------------------------------------------------------------
# Single issue mode
# ---------------------------------------------------------------------------
if [[ -n "$ISSUE" ]]; then
    echo -e "${CYAN}Running Issue #${ISSUE}...${NC}"
    prompt="$(get_prompt "$ISSUE")"
    if [[ -z "$prompt" ]]; then
        echo -e "${RED}Invalid issue number: ${ISSUE}${NC}"
        exit 1
    fi
    prompt_file="${TMPDIR}/claude-prompt-${ISSUE}.txt"
    printf '%s' "$prompt" > "$prompt_file"
    cd "$REPO_ROOT"
    "$CLAUDE_BIN" -p "$(cat "$prompt_file")" --dangerously-skip-permissions
    rm -f "$prompt_file"
    exit 0
fi

# ---------------------------------------------------------------------------
# Batch mode
# ---------------------------------------------------------------------------
case "$BATCH" in
    1)
        echo -e "\n${CYAN}BATCH 1 -- Launching 4 parallel subagents (#162, #163, #164, #165)${NC}"
        for issue in 162 163 164 165; do
            start_claude_subagent "$issue"
        done
        wait_claude_batch 162 163 164 165
        echo -e "\n${GREEN}Batch 1 complete. Merge all branches, then run: ./scripts/kick-off-sceditor.sh --batch 2${NC}"
        ;;
    2)
        echo -e "\n${CYAN}BATCH 2 -- Issue #166 (depends on Batch 1)${NC}"
        start_claude_subagent 166
        wait_claude_batch 166
        echo -e "\n${GREEN}Batch 2 complete. Run: ./scripts/kick-off-sceditor.sh --batch 3${NC}"
        ;;
    3)
        echo -e "\n${CYAN}BATCH 3 -- Issue #167 (depends on Batch 2)${NC}"
        start_claude_subagent 167
        wait_claude_batch 167
        echo -e "\n${GREEN}Batch 3 complete. Run: ./scripts/kick-off-sceditor.sh --batch 4${NC}"
        ;;
    4)
        echo -e "\n${CYAN}BATCH 4 -- Issue #168 (final verification)${NC}"
        start_claude_subagent 168
        wait_claude_batch 168
        echo -e "\n${GREEN}Batch 4 complete. All issues implemented.${NC}"
        ;;
    *)
        echo -e "${RED}Invalid batch: ${BATCH}. Use 1, 2, 3, or 4.${NC}"
        exit 1
        ;;
esac
