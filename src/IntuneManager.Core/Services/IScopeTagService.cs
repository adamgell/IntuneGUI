using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IScopeTagService
{
    Task<List<RoleScopeTag>> ListScopeTagsAsync(CancellationToken cancellationToken = default);
    Task<RoleScopeTag?> GetScopeTagAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleScopeTag> CreateScopeTagAsync(RoleScopeTag scopeTag, CancellationToken cancellationToken = default);
    Task<RoleScopeTag> UpdateScopeTagAsync(RoleScopeTag scopeTag, CancellationToken cancellationToken = default);
    Task DeleteScopeTagAsync(string id, CancellationToken cancellationToken = default);
}
