using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing role mappings between legacy applications and centralized identity
    /// </summary>
    public interface IRoleMappingRepository
    {
        /// <summary>
        /// Gets a role mapping by ID
        /// </summary>
        /// <param name="id">The mapping ID</param>
        /// <returns>The role mapping or null if not found</returns>
        Task<RoleMapping> GetByIdAsync(string id);

        /// <summary>
        /// Gets all role mappings for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <returns>A list of role mappings</returns>
        Task<IEnumerable<RoleMapping>> GetByApplicationIdAsync(string applicationId);

        /// <summary>
        /// Gets a role mapping by legacy role name for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyRoleName">The legacy role name</param>
        /// <returns>The role mapping or null if not found</returns>
        Task<RoleMapping> GetByLegacyRoleNameAsync(string applicationId, string legacyRoleName);

        /// <summary>
        /// Gets a role mapping by centralized role name for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedRoleName">The centralized role name</param>
        /// <returns>The role mapping or null if not found</returns>
        Task<RoleMapping> GetByCentralizedRoleNameAsync(string applicationId, string centralizedRoleName);

        /// <summary>
        /// Creates a new role mapping
        /// </summary>
        /// <param name="roleMapping">The role mapping to create</param>
        /// <returns>The created role mapping</returns>
        Task<RoleMapping> CreateAsync(RoleMapping roleMapping);

        /// <summary>
        /// Updates an existing role mapping
        /// </summary>
        /// <param name="roleMapping">The role mapping to update</param>
        /// <returns>The updated role mapping</returns>
        Task<RoleMapping> UpdateAsync(RoleMapping roleMapping);

        /// <summary>
        /// Deletes a role mapping
        /// </summary>
        /// <param name="id">The mapping ID</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);
    }
} 