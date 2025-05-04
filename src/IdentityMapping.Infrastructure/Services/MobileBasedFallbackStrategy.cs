using System;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Services
{
    /// <summary>
    /// A fallback strategy that tries to find or create mappings based on mobile number
    /// </summary>
    public class MobileBasedFallbackStrategy : IFallbackStrategy
    {
        private readonly IUserIdentityRepository _userIdentityRepository;
        private readonly IUserIdMappingRepository _mappingRepository;
        private readonly ILogger<MobileBasedFallbackStrategy> _logger;

        public MobileBasedFallbackStrategy(
            IUserIdentityRepository userIdentityRepository,
            IUserIdMappingRepository mappingRepository,
            ILogger<MobileBasedFallbackStrategy> logger)
        {
            _userIdentityRepository = userIdentityRepository ?? throw new ArgumentNullException(nameof(userIdentityRepository));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Mobile-based strategy has same priority as email
        public int Priority => 50;

        public bool CanHandle(string applicationId, FallbackContext context)
        {
            // This strategy can handle cases where we have a mobile number
            return !string.IsNullOrEmpty(context.UserMobileNumber);
        }

        public async Task<FallbackResult> HandleAsync(string applicationId, FallbackContext context)
        {
            if (string.IsNullOrEmpty(context.UserMobileNumber))
            {
                return FallbackResult.CreateFailure("Mobile number is required for mobile-based fallback strategy");
            }

            _logger.LogInformation($"Attempting mobile-based fallback for application {applicationId} with mobile {context.UserMobileNumber}");

            try
            {
                // Try to find an existing user identity by mobile number
                var existingIdentity = await _userIdentityRepository.GetByMobileNumberAsync(context.UserMobileNumber);

                if (existingIdentity != null)
                {
                    _logger.LogInformation($"Found existing user identity with ID {existingIdentity.Id} for mobile {context.UserMobileNumber}");

                    // If we're looking for a centralized identity ID but have a legacy ID
                    if (string.IsNullOrEmpty(context.CentralizedIdentityId) && !string.IsNullOrEmpty(context.LegacyUserId))
                    {
                        // Check if a mapping already exists
                        var existingMapping = await _mappingRepository.GetByLegacyUserIdAsync(applicationId, context.LegacyUserId);
                        
                        if (existingMapping != null)
                        {
                            _logger.LogInformation($"Found existing mapping for legacy user ID {context.LegacyUserId}");
                            return FallbackResult.CreateSuccess(existingMapping, "Found existing mapping by legacy user ID");
                        }

                        // Create a new mapping
                        var newMapping = new UserIdMapping
                        {
                            ApplicationId = applicationId,
                            LegacyUserId = context.LegacyUserId,
                            CentralizedIdentityId = existingIdentity.Id,
                            CreatedAt = DateTime.UtcNow,
                            IsValidated = false,
                            Metadata = $"Created by mobile-based fallback strategy for mobile {context.UserMobileNumber}"
                        };

                        var createdMapping = await _mappingRepository.CreateAsync(newMapping);
                        
                        // Also add to the user identity's legacy IDs
                        await _userIdentityRepository.AddLegacyUserIdAsync(existingIdentity.Id, applicationId, context.LegacyUserId);
                        
                        _logger.LogInformation($"Created new mapping with ID {createdMapping.Id} from legacy user ID {context.LegacyUserId} to centralized identity ID {existingIdentity.Id}");
                        
                        return FallbackResult.CreateSuccess(createdMapping, "Created new mapping based on existing user identity found by mobile number");
                    }
                    // If we're looking for a legacy ID but have a centralized identity ID
                    else if (!string.IsNullOrEmpty(context.CentralizedIdentityId) && string.IsNullOrEmpty(context.LegacyUserId))
                    {
                        if (existingIdentity.Id != context.CentralizedIdentityId)
                        {
                            _logger.LogWarning($"Found identity with ID {existingIdentity.Id} does not match provided centralized identity ID {context.CentralizedIdentityId}");
                            return FallbackResult.CreateFailure("Mobile number matches a different centralized identity than provided");
                        }

                        // Check if this user has any legacy IDs for this application
                        if (existingIdentity.LegacyUserIds != null && 
                            existingIdentity.LegacyUserIds.TryGetValue(applicationId, out string legacyId))
                        {
                            var mapping = await _mappingRepository.GetByLegacyUserIdAsync(applicationId, legacyId);
                            
                            if (mapping != null)
                            {
                                _logger.LogInformation($"Found existing mapping for application {applicationId} and centralized identity ID {context.CentralizedIdentityId}");
                                return FallbackResult.CreateSuccess(mapping, "Found existing mapping from central identity's legacy IDs");
                            }
                        }

                        // In this case, we don't have enough information to create a mapping
                        return FallbackResult.CreateFailure("Cannot create a mapping without a legacy user ID");
                    }
                    
                    // If both IDs are provided, we can create a mapping directly
                    if (!string.IsNullOrEmpty(context.CentralizedIdentityId) && !string.IsNullOrEmpty(context.LegacyUserId))
                    {
                        if (existingIdentity.Id != context.CentralizedIdentityId)
                        {
                            _logger.LogWarning($"Found identity with ID {existingIdentity.Id} does not match provided centralized identity ID {context.CentralizedIdentityId}");
                            return FallbackResult.CreateFailure("Mobile number matches a different centralized identity than provided");
                        }

                        var existingMapping = await _mappingRepository.GetByLegacyUserIdAsync(applicationId, context.LegacyUserId);
                        
                        if (existingMapping != null)
                        {
                            _logger.LogInformation($"Found existing mapping for legacy user ID {context.LegacyUserId}");
                            return FallbackResult.CreateSuccess(existingMapping, "Found existing mapping by legacy user ID");
                        }

                        var newMapping = new UserIdMapping
                        {
                            ApplicationId = applicationId,
                            LegacyUserId = context.LegacyUserId,
                            CentralizedIdentityId = context.CentralizedIdentityId,
                            CreatedAt = DateTime.UtcNow,
                            IsValidated = false,
                            Metadata = $"Created by mobile-based fallback strategy for mobile {context.UserMobileNumber}"
                        };

                        var createdMapping = await _mappingRepository.CreateAsync(newMapping);
                        
                        // Also add to the user identity's legacy IDs
                        await _userIdentityRepository.AddLegacyUserIdAsync(existingIdentity.Id, applicationId, context.LegacyUserId);
                        
                        _logger.LogInformation($"Created new mapping with ID {createdMapping.Id}");
                        
                        return FallbackResult.CreateSuccess(createdMapping, "Created new mapping based on provided IDs and existing user identity");
                    }
                }
                else if (!string.IsNullOrEmpty(context.UserDisplayName) && !string.IsNullOrEmpty(context.LegacyUserId))
                {
                    // No existing identity, but we have enough information to create one
                    _logger.LogInformation($"No existing user identity found for mobile {context.UserMobileNumber}, creating new one");
                    
                    var newIdentity = new UserIdentity
                    {
                        DisplayName = context.UserDisplayName,
                        MobileNumber = context.UserMobileNumber,
                        MobileVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    var createdIdentity = await _userIdentityRepository.CreateAsync(newIdentity);
                    
                    // Create a mapping
                    var newMapping = new UserIdMapping
                    {
                        ApplicationId = applicationId,
                        LegacyUserId = context.LegacyUserId,
                        CentralizedIdentityId = createdIdentity.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsValidated = false,
                        Metadata = $"Created by mobile-based fallback strategy for mobile {context.UserMobileNumber}"
                    };
                    
                    var createdMapping = await _mappingRepository.CreateAsync(newMapping);
                    
                    // Add to the user identity's legacy IDs
                    await _userIdentityRepository.AddLegacyUserIdAsync(createdIdentity.Id, applicationId, context.LegacyUserId);
                    
                    _logger.LogInformation($"Created new user identity with ID {createdIdentity.Id} and mapping with ID {createdMapping.Id}");
                    
                    return FallbackResult.CreateSuccess(createdMapping, "Created new user identity and mapping based on mobile number and display name");
                }
                
                _logger.LogWarning("Insufficient information to create a mapping");
                return FallbackResult.CreateFailure("Insufficient information to create a mapping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mobile-based fallback strategy");
                return FallbackResult.CreateFailure($"Error in mobile-based fallback strategy: {ex.Message}");
            }
        }
    }
} 