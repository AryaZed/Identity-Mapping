using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Manages and executes fallback strategies when user mappings are not found
    /// </summary>
    public interface IFallbackStrategyManager
    {
        /// <summary>
        /// Register a fallback strategy
        /// </summary>
        /// <param name="strategy">The strategy to register</param>
        void RegisterStrategy(IFallbackStrategy strategy);

        /// <summary>
        /// Executes fallback strategies to find or create a user mapping
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="context">The fallback context containing relevant information</param>
        /// <returns>The result of the fallback operation</returns>
        Task<FallbackResult> ExecuteFallbackAsync(string applicationId, FallbackContext context);

        /// <summary>
        /// Gets all registered fallback strategies
        /// </summary>
        /// <returns>A list of registered fallback strategies</returns>
        IReadOnlyList<IFallbackStrategy> GetRegisteredStrategies();
    }
} 