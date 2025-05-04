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
        public string Id { get; set; }

        /// <summary>
        /// The application ID this mapping belongs to
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// The role name in the legacy application
        /// </summary>
        public string LegacyRoleName { get; set; }

        /// <summary>
        /// The role name in the centralized identity system
        /// </summary>
        public string CentralizedRoleName { get; set; }

        /// <summary>
        /// When the role mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Description or additional information about this role mapping
        /// </summary>
        public string Description { get; set; }
    }
}