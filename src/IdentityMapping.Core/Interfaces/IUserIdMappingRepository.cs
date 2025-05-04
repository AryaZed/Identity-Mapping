using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing user ID mappings
    /// </summary>
    public interface IUserIdMappingRepository
    {
        /// <summary>
        /// Gets a mapping by ID
        /// </summary>
        /// <param name="id">The mapping ID</param>
        /// <returns>The mapping or null if not found</returns>
        Task<UserIdMapping> GetByIdAsync(string id);

        /// <summary>
        /// Creates a new mapping
        /// </summary>
        /// <param name="mapping">The mapping to create</param>
        /// <returns>The created mapping with assigned ID</returns>
        Task<UserIdMapping> CreateAsync(UserIdMapping mapping);

        /// <summary>
        /// Updates an existing mapping
        /// </summary>
        /// <param name="mapping">The mapping to update</param>
        /// <returns>The updated mapping</returns>
        Task<UserIdMapping> UpdateAsync(UserIdMapping mapping);

        /// <summary>
        /// Deletes a mapping
        /// </summary>
        /// <param name="id">The mapping ID to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Gets all mappings
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>A list of mappings</returns>
        Task<IEnumerable<UserIdMapping>> GetAllAsync(int skip = 0, int take = 100);

        /// <summary>
        /// Gets mappings by application ID
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>A list of mappings for the specified application</returns>
        Task<IEnumerable<UserIdMapping>> GetByApplicationIdAsync(string applicationId, int skip = 0, int take = 100);

        /// <summary>
        /// Gets mappings by centralized identity ID
        /// </summary>
        /// <param name="centralizedIdentityId">The centralized identity ID</param>
        /// <returns>A list of mappings for the specified centralized identity</returns>
        Task<IEnumerable<UserIdMapping>> GetByCentralizedIdentityIdAsync(string centralizedIdentityId);

        /// <summary>
        /// Gets a mapping by application ID and legacy user ID
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyUserId">The legacy user ID</param>
        /// <returns>The mapping or null if not found</returns>
        Task<UserIdMapping> GetByLegacyUserIdAsync(string applicationId, string legacyUserId);

        /// <summary>
        /// Gets a mapping by application ID and centralized identity ID
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedIdentityId">The centralized identity ID</param>
        /// <returns>The mapping or null if not found</returns>
        Task<UserIdMapping> GetByApplicationAndIdentityIdAsync(string applicationId, string centralizedIdentityId);
    }
} 