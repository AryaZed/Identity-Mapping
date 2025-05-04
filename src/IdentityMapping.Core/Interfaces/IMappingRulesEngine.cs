using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Engine for executing dynamic mapping rules
    /// </summary>
    public interface IMappingRulesEngine
    {
        /// <summary>
        /// Applies rules to transform a claim based on dynamic rules
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="sourceClaim">The source claim</param>
        /// <param name="direction">The mapping direction</param>
        /// <param name="context">Optional context data for rule evaluation</param>
        /// <returns>The transformed claim or null if no applicable rules</returns>
        Task<Claim> TransformClaimAsync(string applicationId, Claim sourceClaim, MappingDirection direction, IDictionary<string, object> context = null);

        /// <summary>
        /// Applies rules to transform multiple claims based on dynamic rules
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="sourceClaims">The source claims</param>
        /// <param name="direction">The mapping direction</param>
        /// <param name="context">Optional context data for rule evaluation</param>
        /// <returns>The transformed claims</returns>
        Task<IEnumerable<Claim>> TransformClaimsAsync(string applicationId, IEnumerable<Claim> sourceClaims, MappingDirection direction, IDictionary<string, object> context = null);

        /// <summary>
        /// Applies rules to transform a role based on dynamic rules
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="sourceRole">The source role</param>
        /// <param name="direction">The mapping direction</param>
        /// <param name="context">Optional context data for rule evaluation</param>
        /// <returns>The transformed role or null if no applicable rules</returns>
        Task<string> TransformRoleAsync(string applicationId, string sourceRole, MappingDirection direction, IDictionary<string, object> context = null);

        /// <summary>
        /// Applies rules to transform multiple roles based on dynamic rules
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="sourceRoles">The source roles</param>
        /// <param name="direction">The mapping direction</param>
        /// <param name="context">Optional context data for rule evaluation</param>
        /// <returns>The transformed roles</returns>
        Task<IEnumerable<string>> TransformRolesAsync(string applicationId, IEnumerable<string> sourceRoles, MappingDirection direction, IDictionary<string, object> context = null);

        /// <summary>
        /// Evaluates a rule condition against the provided context
        /// </summary>
        /// <param name="condition">The condition expression</param>
        /// <param name="context">Context data for evaluation</param>
        /// <returns>True if the condition is met, false otherwise</returns>
        bool EvaluateCondition(string condition, IDictionary<string, object> context);

        /// <summary>
        /// Applies a transformation expression to a value
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="expression">The transformation expression</param>
        /// <param name="context">Context data for evaluation</param>
        /// <returns>The transformed value</returns>
        string ApplyTransformation(string value, string expression, IDictionary<string, object> context);
    }
} 