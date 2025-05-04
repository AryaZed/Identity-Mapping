using System;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Maps claims between legacy applications and centralized identity server
    /// </summary>
    public class ClaimMapping
    {
        /// <summary>
        /// Unique identifier for this claim mapping
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The application ID this mapping belongs to
        /// </summary>
        public string SourceApplicationId { get; set; } = string.Empty;

        /// <summary>
        /// The claim type in the legacy application
        /// </summary>
        public string SourceClaimType { get; set; } = string.Empty;

        /// <summary>
        /// The claim type in the centralized identity system
        /// </summary>
        public string TargetClaimType { get; set; } = string.Empty;

        /// <summary>
        /// Optional value transformation expression
        /// </summary>
        public string? TransformationExpression { get; set; }

        /// <summary>
        /// When the claim mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the claim mapping was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Whether to include this claim when mapping from legacy to centralized
        /// </summary>
        public bool IncludeInCentralized { get; set; } = true;

        /// <summary>
        /// Whether to include this claim when mapping from centralized to legacy
        /// </summary>
        public bool IncludeInLegacy { get; set; } = true;

        /// <summary>
        /// Description or additional information about this claim mapping
        /// </summary>
        public string Description { get; set; }
    }
} 