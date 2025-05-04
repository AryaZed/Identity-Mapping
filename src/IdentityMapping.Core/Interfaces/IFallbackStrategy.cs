using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Defines a strategy for handling cases when a user mapping cannot be found
    /// </summary>
    public interface IFallbackStrategy
    {
        /// <summary>
        /// Priority of this fallback strategy (lower numbers run first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this strategy can handle the given scenario
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="context">The fallback context containing relevant information</param>
        /// <returns>True if this strategy can handle the scenario</returns>
        bool CanHandle(string applicationId, FallbackContext context);

        /// <summary>
        /// Attempts to find or create a user mapping when one doesn't exist
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="context">The fallback context containing relevant information</param>
        /// <returns>A fallback result indicating success or failure</returns>
        Task<FallbackResult> HandleAsync(string applicationId, FallbackContext context);
    }

    /// <summary>
    /// Context information for fallback operations
    /// </summary>
    public class FallbackContext
    {
        /// <summary>
        /// The legacy user ID if available
        /// </summary>
        public string? LegacyUserId { get; set; }

        /// <summary>
        /// The centralized identity ID if available
        /// </summary>
        public string? CentralizedIdentityId { get; set; }

        /// <summary>
        /// User's email address if available
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// User's mobile number if available
        /// </summary>
        public string? UserMobileNumber { get; set; }

        /// <summary>
        /// User's display name if available
        /// </summary>
        public string? UserDisplayName { get; set; }

        /// <summary>
        /// Additional data that might be useful for fallback strategies
        /// </summary>
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Result of a fallback operation
    /// </summary>
    public class FallbackResult
    {
        /// <summary>
        /// Whether the fallback operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The new or found mapping if successful
        /// </summary>
        public UserIdMapping? Mapping { get; set; }

        /// <summary>
        /// The centralized identity ID if found or created
        /// </summary>
        public string? CentralizedIdentityId { get; set; }

        /// <summary>
        /// The legacy user ID if found or created
        /// </summary>
        public string? LegacyUserId { get; set; }

        /// <summary>
        /// Message describing the fallback result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <param name="mapping">The mapping that was found or created</param>
        /// <param name="message">Message describing the success</param>
        /// <returns>A successful fallback result</returns>
        public static FallbackResult CreateSuccess(UserIdMapping mapping, string message = "Fallback strategy successful")
        {
            return new FallbackResult
            {
                Success = true,
                Mapping = mapping,
                CentralizedIdentityId = mapping.CentralizedIdentityId,
                LegacyUserId = mapping.LegacyUserId,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        /// <param name="message">Message describing the failure</param>
        /// <returns>A failed fallback result</returns>
        public static FallbackResult CreateFailure(string message = "Fallback strategy failed")
        {
            return new FallbackResult
            {
                Success = false,
                Message = message
            };
        }
    }
} 