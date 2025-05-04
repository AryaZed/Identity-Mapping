using System;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Maps roles between legacy applications and centralized identity server
    /// </summary>
    public class RoleMapping
    {
        /// <summary>
        /// Unique identifier for this role mapping
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The application ID this mapping belongs to
        /// </summary>
        public string SourceApplicationId { get; set; } = string.Empty;

        /// <summary>
        /// The application ID this mapping belongs to
        /// </summary>
        public string TargetApplicationId { get; set; } = string.Empty;

        /// <summary>
        /// The role name in the legacy application
        /// </summary>
        public string SourceRole { get; set; } = string.Empty;

        /// <summary>
        /// The role name in the centralized identity system
        /// </summary>
        public string TargetRole { get; set; } = string.Empty;

        /// <summary>
        /// When the role mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the role mapping was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Description or additional information about this role mapping
        /// </summary>
        public string Description { get; set; }
    }
}