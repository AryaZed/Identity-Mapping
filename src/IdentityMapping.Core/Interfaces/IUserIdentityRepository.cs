using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing user identities in the centralized system
    /// </summary>
    public interface IUserIdentityRepository
    {
        /// <summary>
        /// Gets a user identity by ID
        /// </summary>
        /// <param name="id">The identity ID</param>
        /// <returns>The user identity or null if not found</returns>
        Task<UserIdentity> GetByIdAsync(string id);

        /// <summary>
        /// Gets a user identity by email
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <returns>The user identity or null if not found</returns>
        Task<UserIdentity> GetByEmailAsync(string email);

        /// <summary>
        /// Gets a user identity by mobile number
        /// </summary>
        /// <param name="mobileNumber">The user's mobile number</param>
        /// <returns>The user identity or null if not found</returns>
        Task<UserIdentity> GetByMobileNumberAsync(string mobileNumber);

        /// <summary>
        /// Creates a new user identity
        /// </summary>
        /// <param name="userIdentity">The user identity to create</param>
        /// <returns>The created user identity with assigned ID</returns>
        Task<UserIdentity> CreateAsync(UserIdentity userIdentity);

        /// <summary>
        /// Updates an existing user identity
        /// </summary>
        /// <param name="userIdentity">The user identity to update</param>
        /// <returns>The updated user identity</returns>
        Task<UserIdentity> UpdateAsync(UserIdentity userIdentity);

        /// <summary>
        /// Deletes a user identity
        /// </summary>
        /// <param name="id">The identity ID to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Gets all user identities
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>A list of user identities</returns>
        Task<IEnumerable<UserIdentity>> GetAllAsync(int skip = 0, int take = 100);

        /// <summary>
        /// Add a legacy user ID mapping to an existing user identity
        /// </summary>
        /// <param name="identityId">The centralized identity ID</param>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyUserId">The legacy user ID</param>
        /// <returns>True if added successfully, false otherwise</returns>
        Task<bool> AddLegacyUserIdAsync(string identityId, string applicationId, string legacyUserId);

        /// <summary>
        /// Find a central identity by a legacy user ID and application ID
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyUserId">The legacy user ID</param>
        /// <returns>The central user identity or null if not found</returns>
        Task<UserIdentity> FindByLegacyUserIdAsync(string applicationId, string legacyUserId);
    }
} 