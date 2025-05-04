using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Service for mapping identity information between legacy applications and centralized identity
    /// </summary>
    public interface IIdentityMappingService
    {
        /// <summary>
        /// Maps a role from legacy application to centralized identity system
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyRoleName">The legacy role name</param>
        /// <returns>The centralized role name or null if no mapping exists</returns>
        Task<string> MapRoleToCentralizedAsync(string applicationId, string legacyRoleName);

        /// <summary>
        /// Maps a role from centralized identity to legacy application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedRoleName">The centralized role name</param>
        /// <returns>The legacy role name or null if no mapping exists</returns>
        Task<string> MapRoleToLegacyAsync(string applicationId, string centralizedRoleName);

        /// <summary>
        /// Maps multiple roles from legacy application to centralized identity system
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyRoleNames">The legacy role names</param>
        /// <returns>The centralized role names</returns>
        Task<IEnumerable<string>> MapRolesToCentralizedAsync(string applicationId, IEnumerable<string> legacyRoleNames);

        /// <summary>
        /// Maps multiple roles from centralized identity to legacy application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedRoleNames">The centralized role names</param>
        /// <returns>The legacy role names</returns>
        Task<IEnumerable<string>> MapRolesToLegacyAsync(string applicationId, IEnumerable<string> centralizedRoleNames);

        /// <summary>
        /// Maps a claim from legacy application to centralized identity system
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyClaim">The legacy claim</param>
        /// <returns>The centralized claim or null if no mapping exists</returns>
        Task<Claim> MapClaimToCentralizedAsync(string applicationId, Claim legacyClaim);

        /// <summary>
        /// Maps a claim from centralized identity to legacy application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedClaim">The centralized claim</param>
        /// <returns>The legacy claim or null if no mapping exists</returns>
        Task<Claim> MapClaimToLegacyAsync(string applicationId, Claim centralizedClaim);

        /// <summary>
        /// Maps multiple claims from legacy application to centralized identity system
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyClaims">The legacy claims</param>
        /// <returns>The centralized claims</returns>
        Task<IEnumerable<Claim>> MapClaimsToCentralizedAsync(string applicationId, IEnumerable<Claim> legacyClaims);

        /// <summary>
        /// Maps multiple claims from centralized identity to legacy application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedClaims">The centralized claims</param>
        /// <returns>The legacy claims</returns>
        Task<IEnumerable<Claim>> MapClaimsToLegacyAsync(string applicationId, IEnumerable<Claim> centralizedClaims);
    }
} 