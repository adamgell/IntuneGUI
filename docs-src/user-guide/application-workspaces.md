# Application Workspaces

The **Applications** primary tab contains seven workspaces for managing and inspecting Intune application deployments.

## App Gallery

The main applications list showing all mobile apps registered in Intune. Click any app to see its detail panel with properties, links, categories, and assignment breakdown (Required / Available / Uninstall).

## Application Assignments

A flat, denormalized view of every app-to-group assignment across your tenant. Each row shows the app name, target group, install intent, assignment type, and key metadata — useful for auditing which groups receive which apps.

!!! info "Long-running command"
    This workspace fetches assignments for all applications. The bridge command uses a 180-second timeout to accommodate large tenants.

## Bulk App Assignments

Apply assignment changes to multiple applications at once. The workspace bootstraps by loading all apps and groups, then lets you select apps, choose a target group, set the install intent, and apply in a single operation.

## App Protection Policies

Lists all app protection policies (iOS/Android MAM policies) with detail view showing policy settings, platform, version, and assignments.

## Managed Device App Configurations

Lists managed device app configuration policies with detail view showing targeted mobile apps, settings, and assignments.

## Targeted Managed App Configurations

Lists targeted managed app configuration policies (MAM app configs) with detail view showing app group type, deployed app count, and assignments.

## VPP Tokens

Lists Apple Volume Purchase Program tokens with detail view showing organization, Apple ID, sync status, expiration, and account type.

## Known limitations

- All application workspaces are read-only; creating or modifying apps requires the Intune portal.
- Bulk App Assignments applies changes sequentially; there is no rollback if a mid-batch assignment fails.
- VPP token sync and renewal operations are not yet supported.
