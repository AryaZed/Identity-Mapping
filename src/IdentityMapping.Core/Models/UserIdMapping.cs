using System;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Represents a mapping between a legacy application user ID and a centralized identity
    /// </summary>
    public class UserIdMapping
    {
        /// <summary>
        /// Unique identifier for this mapping
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The application identifier
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// The user ID in the legacy application
        /// </summary>
        public string LegacyUserId { get; set; }

        /// <summary>
        /// The corresponding centralized identity ID
        /// </summary>
        public string CentralizedIdentityId { get; set; }

        /// <summary>
        /// When this mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Optional metadata about this mapping
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Indicates if this mapping has been validated/confirmed
        /// </summary>
        public bool IsValidated { get; set; }
    }
} 