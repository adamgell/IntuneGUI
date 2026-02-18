using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IRoleDefinitionService
{
    Task<List<RoleDefinition>> ListRoleDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<RoleDefinition?> GetRoleDefinitionAsync(string id, CancellationToken cancellationToken = default);
    Task<RoleDefinition> CreateRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default);
    Task<RoleDefinition> UpdateRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default);
    Task DeleteRoleDefinitionAsync(string id, CancellationToken cancellationToken = default);
}
