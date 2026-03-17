# Groups Workspace

The Groups workspace provides a full list-and-detail view of Entra ID groups used by your Intune tenant. It lives under the **Admin** primary tab in the desktop UI.

## Features

- **Group list with DataGrid** — paginated table showing all groups with display name, type, membership type, and member count.
- **Type filter chips** — quickly filter by Dynamic Device, Dynamic User, or Assigned membership type.
- **Detail panel** — click any group row to see:
    - Group properties (display name, description, ID, mail, membership rule)
    - Membership rule viewer for dynamic groups
    - Members table with user/device/group type icons
    - Intune assignments table showing all policies and apps assigned to the group
- **Caching** — group list and detail data are cached with a 1-hour TTL. Repeat visits load instantly from cache.
- **Parallel data loading** — detail view uses `Task.WhenAll` to fetch member counts, members list, and Intune assignments concurrently.

## Navigation

1. Click the **Admin** tab in the top navigation bar.
2. Select **Groups** from the sidebar.

## Known limitations

- The workspace is read-only; group creation and editing are not yet supported.
- Member counts may differ from the Azure portal for nested groups (only direct members are shown).
- Assignment data covers policies and apps tracked by Intune Commander's core services.
