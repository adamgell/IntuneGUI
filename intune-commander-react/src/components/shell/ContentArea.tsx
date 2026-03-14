import { useAppStore } from '../../store/appStore';
import { SettingsCatalogWorkspace } from '../workspace/SettingsCatalogWorkspace';
import { GlobalSearchResultsWorkspace } from '../workspace/GlobalSearchResultsWorkspace';

export function ContentArea() {
  const activeSidebarItem = useAppStore((s) => s.activeSidebarItem);
  const activeProfile = useAppStore((s) => s.activeProfile);

  if (activeSidebarItem === 'global-search') {
    return (
      <main className="content-area">
        <GlobalSearchResultsWorkspace />
      </main>
    );
  }

  if (activeSidebarItem === 'overview' && activeProfile) {
    return (
      <main className="content-area">
        <div className="tenant-summary">
          <h3>Tenant Summary</h3>
          <div className="tenant-summary-grid">
            <span className="tenant-summary-label">Profile</span>
            <span className="tenant-summary-value">{activeProfile.name}</span>
            <span className="tenant-summary-label">Tenant ID</span>
            <span className="tenant-summary-value">{activeProfile.tenantId}</span>
            <span className="tenant-summary-label">Cloud</span>
            <span className="tenant-summary-value">{activeProfile.cloud}</span>
            <span className="tenant-summary-label">Auth Method</span>
            <span className="tenant-summary-value">{activeProfile.authMethod}</span>
            {activeProfile.lastUsed && (
              <>
                <span className="tenant-summary-label">Last Used</span>
                <span className="tenant-summary-value">
                  {new Date(activeProfile.lastUsed).toLocaleString()}
                </span>
              </>
            )}
          </div>
        </div>
      </main>
    );
  }

  if (activeSidebarItem === 'settings-catalog') {
    return (
      <main className="content-area">
        <SettingsCatalogWorkspace />
      </main>
    );
  }

  return (
    <main className="content-area">
      <div className="content-placeholder">
        <h3>{activeSidebarItem ?? 'Welcome'}</h3>
        <p>
          This feature is coming soon. Select Settings Catalog from the sidebar
          to see real data.
        </p>
      </div>
    </main>
  );
}
