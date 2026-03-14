export enum CloudEnvironment {
  Commercial = 'Commercial',
  GCC = 'GCC',
  GCCHigh = 'GCCHigh',
  DoD = 'DoD',
}

export enum AuthMethod {
  Interactive = 'Interactive',
  ClientSecret = 'ClientSecret',
  DeviceCode = 'DeviceCode',
}

export interface TenantProfile {
  id: string;
  name: string;
  tenantId: string;
  clientId: string;
  cloud: CloudEnvironment;
  authMethod: AuthMethod;
  clientSecret?: string;
  lastUsed?: string;
}

export interface NavCategory {
  name: string;
  icon: string;
}

export interface NavGroup {
  name: string;
  icon: string;
  children: NavCategory[];
}

export interface ShellState {
  isConnected: boolean;
  isBusy: boolean;
  statusText: string;
  errorMessage?: string | null;
  activeProfile?: TenantProfile | null;
}

export interface DeviceCodeInfo {
  userCode: string;
  verificationUrl: string;
  message: string;
}

export interface ProfilesPayload {
  profiles: TenantProfile[];
  activeProfileId: string | null;
}

export interface ImportResult extends ProfilesPayload {
  imported: number;
}

// Navigation types for two-tier top nav
export interface PrimaryNavTab {
  id: string;
  label: string;
  secondaryTabs: SecondaryNavTab[];
}

export interface SecondaryNavTab {
  id: string;
  label: string;
}

export interface SidebarItem {
  id: string;
  label: string;
  count?: number;
}

export interface SidebarSection {
  label: string;
  items: SidebarItem[];
}

// Navigation definitions
export const primaryNavTabs: PrimaryNavTab[] = [
  {
    id: 'configuration',
    label: 'Configuration',
    secondaryTabs: [
      { id: 'configuration-profiles', label: 'Configuration profiles' },
      { id: 'settings-catalog', label: 'Settings Catalog' },
      { id: 'admin-templates', label: 'Admin templates' },
    ],
  },
  {
    id: 'endpoint-security',
    label: 'Endpoint security',
    secondaryTabs: [
      { id: 'compliance-policies', label: 'Compliance policies' },
      { id: 'endpoint-security-profiles', label: 'Security profiles' },
    ],
  },
  {
    id: 'devices',
    label: 'Devices',
    secondaryTabs: [
      { id: 'device-categories', label: 'Device categories' },
      { id: 'enrollment', label: 'Enrollment' },
      { id: 'autopilot', label: 'Autopilot' },
    ],
  },
  {
    id: 'reports',
    label: 'Reports',
    secondaryTabs: [
      { id: 'assignment-reports', label: 'Assignment reports' },
      { id: 'device-health', label: 'Device health scripts' },
    ],
  },
  {
    id: 'automation',
    label: 'Automation',
    secondaryTabs: [
      { id: 'device-scripts', label: 'Device scripts' },
      { id: 'shell-scripts', label: 'Shell scripts' },
    ],
  },
];
