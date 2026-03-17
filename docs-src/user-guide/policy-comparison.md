# Policy Comparison

The Policy Comparison workspace provides side-by-side JSON diff of any two policies within the same category, powered by the Monaco diff editor.

## How to use

1. Navigate to **Operations > Policy Diff** in the sidebar.
2. Select a policy category (Settings Catalog, Compliance, Device Configuration, Conditional Access, App Protection, or Endpoint Security).
3. Choose **Policy A** and **Policy B** from the dropdowns.
4. Click **Compare**.

The workspace displays a summary bar showing the number of differing properties, followed by a full Monaco side-by-side diff editor with syntax highlighting, line numbers, and word wrap.

## Features

- **Category-scoped comparison** — only policies of the same type can be compared.
- **Normalized JSON** — policies are serialized with sorted keys for a clean diff.
- **MonacoDiffViewer** — a shared reusable component also available to the Drift Detection workspace.
- **Difference count** — header shows total properties and how many differ.

## Known limitations

- Cross-category comparison (e.g., comparing a compliance policy to a device config) is not supported.
- Only the JSON representation is compared; semantic equivalence of settings is not evaluated.
