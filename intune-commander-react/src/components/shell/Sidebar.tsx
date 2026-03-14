import { useState } from 'react';
import { useAppStore } from '../../store/appStore';
import '../../styles/sidebar.css';

interface SidebarSection {
  label: string;
  items: { id: string; label: string; count?: number }[];
}

const sections: SidebarSection[] = [
  {
    label: 'Quick access',
    items: [
      { id: 'overview', label: 'Overview' },
      { id: 'configuration-profiles', label: 'Configuration profiles' },
      { id: 'device-health-scripts', label: 'Device health scripts' },
      { id: 'assignment-reports', label: 'Assignment reports' },
    ],
  },
  {
    label: 'Favorites',
    items: [
      { id: 'settings-catalog', label: 'Settings Catalog' },
      { id: 'endpoint-security-profiles', label: 'Device Security' },
      { id: 'compliance-policies', label: 'Compliance policies' },
    ],
  },
  {
    label: 'By platform',
    items: [
      { id: 'platform-windows', label: 'Windows 10 and later' },
      { id: 'platform-macos', label: 'macOS' },
      { id: 'platform-ios', label: 'iOS/iPadOS' },
      { id: 'platform-android', label: 'Android Enterprise' },
    ],
  },
];

export function Sidebar() {
  const activeSidebarItem = useAppStore((s) => s.activeSidebarItem);
  const setSidebarItem = useAppStore((s) => s.setSidebarItem);
  const [searchQuery, setSearchQuery] = useState('');

  const filteredSections = searchQuery
    ? sections
        .map((section) => ({
          ...section,
          items: section.items.filter((item) =>
            item.label.toLowerCase().includes(searchQuery.toLowerCase()),
          ),
        }))
        .filter((section) => section.items.length > 0)
    : sections;

  return (
    <aside className="sidebar">
      <div className="sidebar-panel">
        <div className="sidebar-panel-header">
          <strong>Devices</strong>
          <span>Configuration and reporting menu</span>
        </div>

        <div className="sidebar-search">
          <input
            type="text"
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>

        {filteredSections.map((section) => (
          <div key={section.label} className="nav-group">
            <div className="nav-label">{section.label}</div>
            {section.items.map((item) => (
              <button
                key={item.id}
                className={`nav-item${item.id === activeSidebarItem ? ' active' : ''}`}
                onClick={() => setSidebarItem(item.id)}
              >
                <span className="nav-item-label">{item.label}</span>
                {item.count !== undefined && (
                  <small className="nav-item-count">{item.count}</small>
                )}
              </button>
            ))}
          </div>
        ))}
      </div>
    </aside>
  );
}
