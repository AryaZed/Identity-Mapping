using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Repository for managing dynamic mapping rules
    /// </summary>
    public interface IMappingRuleRepository
    {
        /// <summary>
        /// Gets a mapping rule by ID
        /// </summary>
        /// <param name="id">The rule ID</param>
        /// <returns>The mapping rule or null if not found</returns>
        Task<MappingRule> GetByIdAsync(string id);

        /// <summary>
        /// Gets all mapping rules for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <returns>A list of mapping rules</returns>
        Task<IEnumerable<MappingRule>> GetByApplicationIdAsync(string applicationId);

        /// <summary>
        /// Gets all mapping rules for a specific application and rule type
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="ruleType">The rule type</param>
        /// <returns>A list of mapping rules</returns>
        Task<IEnumerable<MappingRule>> GetByTypeAsync(string applicationId, MappingRuleType ruleType);

        /// <summary>
        /// Gets all mapping rules for a specific application, rule type, and direction
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="ruleType">The rule type</param>
        /// <param name="direction">The mapping direction</param>
        /// <returns>A list of mapping rules</returns>
        Task<IEnumerable<MappingRule>> GetByTypeAndDirectionAsync(string applicationId, MappingRuleType ruleType, MappingDirection direction);

        /// <summary>
        /// Gets all mapping rules for a specific source identifier
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="sourceIdentifier">The source identifier</param>
        /// <returns>A list of mapping rules</returns>
        Task<IEnumerable<MappingRule>> GetBySourceIdentifierAsync(string applicationId, string sourceIdentifier);

        /// <summary>
        /// Creates a new mapping rule
        /// </summary>
        /// <param name="rule">The mapping rule to create</param>
        /// <returns>The created mapping rule</returns>
        Task<MappingRule> CreateAsync(MappingRule rule);

        /// <summary>
        /// Updates an existing mapping rule
        /// </summary>
        /// <param name="rule">The mapping rule to update</param>
        /// <returns>The updated mapping rule</returns>
        Task<MappingRule> UpdateAsync(MappingRule rule);

        /// <summary>
        /// Deletes a mapping rule
        /// </summary>
        /// <param name="id">The rule ID</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAsync(string id);
    }
} 