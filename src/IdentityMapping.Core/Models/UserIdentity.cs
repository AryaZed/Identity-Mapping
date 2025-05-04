using System;
using System.Collections.Generic;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Represents a user identity in the centralized identity system
    /// </summary>
    public class UserIdentity
    {
        /// <summary>
        /// Unique identifier for the user in the centralized identity system
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User's display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Indicates if the email is verified
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// User's mobile number
        /// </summary>
        public string MobileNumber { get; set; }

        /// <summary>
        /// Indicates if the mobile number is verified
        /// </summary>
        public bool MobileVerified { get; set; }

        /// <summary>
        /// Collection of legacy user identifiers mapped to this central identity
        /// Key: Application identifier, Value: User ID in that application
        /// </summary>
        public Dictionary<string, string> LegacyUserIds { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// When the user identity was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the user identity was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Optional claims or properties for the user
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    }
} 