using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Services
{
    public class IdentityMappingService : IIdentityMappingService
    {
        private readonly IRoleMappingRepository _roleMappingRepository;
        private readonly IClaimMappingRepository _claimMappingRepository;
        private readonly ILogger<IdentityMappingService> _logger;

        public IdentityMappingService(
            IRoleMappingRepository roleMappingRepository,
            IClaimMappingRepository claimMappingRepository,
            ILogger<IdentityMappingService> logger)
        {
            _roleMappingRepository = roleMappingRepository ?? throw new ArgumentNullException(nameof(roleMappingRepository));
            _claimMappingRepository = claimMappingRepository ?? throw new ArgumentNullException(nameof(claimMappingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> MapRoleToCentralizedAsync(string applicationId, string legacyRoleName)
        {
            if (string.IsNullOrEmpty(legacyRoleName))
                return null;

            try
            {
                var mapping = await _roleMappingRepository.GetByLegacyRoleNameAsync(applicationId, legacyRoleName);
                if (mapping != null)
                {
                    _logger.LogInformation($"Mapped legacy role '{legacyRoleName}' to centralized role '{mapping.CentralizedRoleName}'");
                    return mapping.CentralizedRoleName;
                }
                
                _logger.LogWarning($"No mapping found for legacy role '{legacyRoleName}' in application '{applicationId}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping legacy role '{legacyRoleName}' to centralized role");
                return null;
            }
        }

        public async Task<string> MapRoleToLegacyAsync(string applicationId, string centralizedRoleName)
        {
            if (string.IsNullOrEmpty(centralizedRoleName))
                return null;

            try
            {
                var mapping = await _roleMappingRepository.GetByCentralizedRoleNameAsync(applicationId, centralizedRoleName);
                if (mapping != null)
                {
                    _logger.LogInformation($"Mapped centralized role '{centralizedRoleName}' to legacy role '{mapping.LegacyRoleName}'");
                    return mapping.LegacyRoleName;
                }
                
                _logger.LogWarning($"No mapping found for centralized role '{centralizedRoleName}' in application '{applicationId}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping centralized role '{centralizedRoleName}' to legacy role");
                return null;
            }
        }

        public async Task<IEnumerable<string>> MapRolesToCentralizedAsync(string applicationId, IEnumerable<string> legacyRoleNames)
        {
            if (legacyRoleNames == null || !legacyRoleNames.Any())
                return Enumerable.Empty<string>();

            var centralizedRoles = new List<string>();
            
            foreach (var legacyRole in legacyRoleNames)
            {
                var centralizedRole = await MapRoleToCentralizedAsync(applicationId, legacyRole);
                if (!string.IsNullOrEmpty(centralizedRole))
                {
                    centralizedRoles.Add(centralizedRole);
                }
            }
            
            return centralizedRoles;
        }

        public async Task<IEnumerable<string>> MapRolesToLegacyAsync(string applicationId, IEnumerable<string> centralizedRoleNames)
        {
            if (centralizedRoleNames == null || !centralizedRoleNames.Any())
                return Enumerable.Empty<string>();

            var legacyRoles = new List<string>();
            
            foreach (var centralizedRole in centralizedRoleNames)
            {
                var legacyRole = await MapRoleToLegacyAsync(applicationId, centralizedRole);
                if (!string.IsNullOrEmpty(legacyRole))
                {
                    legacyRoles.Add(legacyRole);
                }
            }
            
            return legacyRoles;
        }

        public async Task<Claim> MapClaimToCentralizedAsync(string applicationId, Claim legacyClaim)
        {
            if (legacyClaim == null)
                return null;

            try
            {
                var mapping = await _claimMappingRepository.GetByLegacyClaimTypeAsync(applicationId, legacyClaim.Type);
                if (mapping != null && mapping.IncludeInCentralized)
                {
                    var claimValue = legacyClaim.Value;
                    
                    // Apply value transformation if specified
                    if (!string.IsNullOrEmpty(mapping.ValueTransformation))
                    {
                        claimValue = TransformClaimValue(legacyClaim.Value, mapping.ValueTransformation);
                    }
                    
                    _logger.LogInformation($"Mapped legacy claim '{legacyClaim.Type}' to centralized claim '{mapping.CentralizedClaimType}'");
                    return new Claim(mapping.CentralizedClaimType, claimValue);
                }
                
                _logger.LogWarning($"No mapping found for legacy claim '{legacyClaim.Type}' in application '{applicationId}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping legacy claim '{legacyClaim.Type}' to centralized claim");
                return null;
            }
        }

        public async Task<Claim> MapClaimToLegacyAsync(string applicationId, Claim centralizedClaim)
        {
            if (centralizedClaim == null)
                return null;

            try
            {
                var mapping = await _claimMappingRepository.GetByCentralizedClaimTypeAsync(applicationId, centralizedClaim.Type);
                if (mapping != null && mapping.IncludeInLegacy)
                {
                    var claimValue = centralizedClaim.Value;
                    
                    // Apply value transformation if specified
                    if (!string.IsNullOrEmpty(mapping.ValueTransformation))
                    {
                        claimValue = TransformClaimValue(centralizedClaim.Value, mapping.ValueTransformation);
                    }
                    
                    _logger.LogInformation($"Mapped centralized claim '{centralizedClaim.Type}' to legacy claim '{mapping.LegacyClaimType}'");
                    return new Claim(mapping.LegacyClaimType, claimValue);
                }
                
                _logger.LogWarning($"No mapping found for centralized claim '{centralizedClaim.Type}' in application '{applicationId}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping centralized claim '{centralizedClaim.Type}' to legacy claim");
                return null;
            }
        }

        public async Task<IEnumerable<Claim>> MapClaimsToCentralizedAsync(string applicationId, IEnumerable<Claim> legacyClaims)
        {
            if (legacyClaims == null || !legacyClaims.Any())
                return Enumerable.Empty<Claim>();

            var centralizedClaims = new List<Claim>();
            
            foreach (var legacyClaim in legacyClaims)
            {
                var centralizedClaim = await MapClaimToCentralizedAsync(applicationId, legacyClaim);
                if (centralizedClaim != null)
                {
                    centralizedClaims.Add(centralizedClaim);
                }
            }
            
            return centralizedClaims;
        }

        public async Task<IEnumerable<Claim>> MapClaimsToLegacyAsync(string applicationId, IEnumerable<Claim> centralizedClaims)
        {
            if (centralizedClaims == null || !centralizedClaims.Any())
                return Enumerable.Empty<Claim>();

            var legacyClaims = new List<Claim>();
            
            foreach (var centralizedClaim in centralizedClaims)
            {
                var legacyClaim = await MapClaimToLegacyAsync(applicationId, centralizedClaim);
                if (legacyClaim != null)
                {
                    legacyClaims.Add(legacyClaim);
                }
            }
            
            return legacyClaims;
        }

        private string TransformClaimValue(string originalValue, string transformation)
        {
            if (string.IsNullOrEmpty(transformation))
                return originalValue;

            try
            {
                // This is a simplified implementation.
                // In a real-world scenario, you might want to use a more sophisticated 
                // approach like expression parsing or scripting engine.
                
                // For now, we'll just handle some basic transformations:
                
                // 1. Replace values using format: "oldValue1:newValue1;oldValue2:newValue2"
                if (transformation.Contains(':') && transformation.Contains(';'))
                {
                    var replacements = transformation.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var replacement in replacements)
                    {
                        var parts = replacement.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            if (originalValue == parts[0])
                            {
                                return parts[1];
                            }
                        }
                    }
                }
                
                // 2. Simple prefix addition: "prefix:"
                else if (transformation.EndsWith(':'))
                {
                    return transformation.TrimEnd(':') + originalValue;
                }
                
                // 3. Simple suffix addition: ":suffix"
                else if (transformation.StartsWith(':'))
                {
                    return originalValue + transformation.TrimStart(':');
                }
                
                // 4. Simple replacement: "oldValue:newValue"
                else if (transformation.Contains(':'))
                {
                    var parts = transformation.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && originalValue == parts[0])
                    {
                        return parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying transformation '{transformation}' to claim value '{originalValue}'");
            }
            
            // Default to original value if transformation fails or is not recognized
            return originalValue;
        }
    }
}