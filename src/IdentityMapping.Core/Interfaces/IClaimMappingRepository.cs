using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing claim mappings between legacy applications and centralized identity
    /// </summary>
    public interface IClaimMappingRepository
    {
        /// <summary>
        /// Gets a claim mapping by ID
        /// </summary>
        /// <param name="id">The mapping ID</param>
        /// <returns>The claim mapping or null if not found</returns>
        Task<ClaimMapping> GetByIdAsync(string id);

        /// <summary>
        /// Gets all claim mappings for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <returns>A list of claim mappings</returns>
        Task<IEnumerable<ClaimMapping>> GetByApplicationIdAsync(string applicationId);

        /// <summary>
        /// Gets a claim mapping by legacy claim type for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyClaimType">The legacy claim type</param>
        /// <returns>The claim mapping or null if not found</returns>
        Task<ClaimMapping> GetByLegacyClaimTypeAsync(string applicationId, string legacyClaimType);

        /// <summary>
        /// Gets a claim mapping by centralized claim type for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedClaimType">The centralized claim type</param>
        /// <returns>The claim mapping or null if not found</returns>
        Task<ClaimMapping> GetByCentralizedClaimTypeAsync(string applicationId, string centralizedClaimType);

        /// <summary>
        /// Creates a new claim mapping
        /// </summary>
        /// <param name="claimMapping">The claim mapping to create</param>
        /// <returns>The created claim mapping</returns>
        Task<ClaimMapping> CreateAsync(ClaimMapping claimMapping);

        /// <summary>
        /// Updates an existing claim mapping
        /// </summary>
        /// <param name="claimMapping">The claim mapping to update</param>
        /// <returns>The updated claim mapping</returns>
        Task<ClaimMapping> UpdateAsync(ClaimMapping claimMapping);

        /// <summary>
        /// Deletes a claim mapping
        /// </summary>
        /// <param name="id">The mapping ID</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);
    }
}