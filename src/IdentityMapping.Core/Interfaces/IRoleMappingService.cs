using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    public interface IRoleMappingService
    {
        Task<IEnumerable<RoleMapping>> GetAllRoleMappingsAsync(int skip = 0, int take = 20);
        Task<RoleMapping?> GetRoleMappingByIdAsync(string id);
        Task<IEnumerable<RoleMapping>> GetRoleMappingsByApplicationAsync(string applicationId);
        Task<RoleMapping> CreateRoleMappingAsync(string sourceApplicationId, string targetApplicationId, string sourceRole, string targetRole);
        Task<RoleMapping> UpdateRoleMappingAsync(string id, string sourceRole, string targetRole);
        Task<bool> DeleteRoleMappingAsync(string id);
        Task<string?> TranslateRoleAsync(string sourceApplicationId, string targetApplicationId, string sourceRole);
    }
} 