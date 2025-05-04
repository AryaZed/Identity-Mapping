using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing applications in the identity mapping system
    /// </summary>
    public interface IApplicationRepository
    {
        /// <summary>
        /// Gets an application by ID
        /// </summary>
        /// <param name="id">The application ID</param>
        /// <returns>The application or null if not found</returns>
        Task<Application> GetByIdAsync(string id);

        /// <summary>
        /// Creates a new application
        /// </summary>
        /// <param name="application">The application to create</param>
        /// <returns>The created application with assigned ID</returns>
        Task<Application> CreateAsync(Application application);

        /// <summary>
        /// Updates an existing application
        /// </summary>
        /// <param name="application">The application to update</param>
        /// <returns>The updated application</returns>
        Task<Application> UpdateAsync(Application application);

        /// <summary>
        /// Deletes an application
        /// </summary>
        /// <param name="id">The application ID to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Gets all applications
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>A list of applications</returns>
        Task<IEnumerable<Application>> GetAllAsync(int skip = 0, int take = 100);

        /// <summary>
        /// Validates application API key
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="apiKey">The API key to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateApiKeyAsync(string applicationId, string apiKey);

        /// <summary>
        /// Regenerates an API key for an application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <returns>The new API key</returns>
        Task<string> RegenerateApiKeyAsync(string applicationId);
    }
} 