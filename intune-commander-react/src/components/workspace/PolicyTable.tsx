import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';

export function PolicyTable() {
  const policies = useSettingsCatalogStore((s) => s.policies);
  const selectedPolicyId = useSettingsCatalogStore((s) => s.selectedPolicyId);
  const isLoadingList = useSettingsCatalogStore((s) => s.isLoadingList);
  const selectPolicy = useSettingsCatalogStore((s) => s.selectPolicy);

  if (isLoadingList) {
    return (
      <div className="panel panel-list">
        <div className="panel-header">
          <strong>Policy list</strong>
          <span>Loading...</span>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading policies from Graph API...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="panel panel-list">
      <div className="panel-header">
        <strong>Policy list</strong>
        <span>{policies.length} policies</span>
      </div>
      <div className="panel-body">
        <div className="policy-table-shell">
          <table className="policy-table">
            <thead>
              <tr>
                <th>Name</th>
                <th className="col-platform">Platform</th>
                <th>Profile type</th>
                <th className="col-modified">Modified</th>
                <th className="col-scope">Scope tag</th>
                <th className="col-actions" />
              </tr>
            </thead>
            <tbody>
              {policies.map((policy) => (
                <tr
                  key={policy.id}
                  className={policy.id === selectedPolicyId ? 'active-row' : ''}
                  onClick={() => void selectPolicy(policy.id)}
                >
                  <td>
                    <div className="policy-name">
                      <strong>{policy.name}</strong>
                      {policy.description && <span>{policy.description}</span>}
                    </div>
                  </td>
                  <td className="col-platform">{policy.platform}</td>
                  <td>{policy.profileType}</td>
                  <td className="col-modified">
                    {policy.lastModified
                      ? new Date(policy.lastModified).toLocaleString()
                      : ''}
                  </td>
                  <td className="col-scope">
                    <span className="status-pill">{policy.scopeTag}</span>
                  </td>
                  <td className="col-actions">
                    <button
                      className="view-btn"
                      onClick={() => void selectPolicy(policy.id)}
                    >
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
