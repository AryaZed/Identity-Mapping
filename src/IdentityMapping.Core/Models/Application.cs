using System;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Represents an application integrated with the identity mapping system
    /// </summary>
    public class Application
    {
        /// <summary>
        /// Unique identifier for the application
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The application name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of the application
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The base URL for the application
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Secret key for the application to authenticate with the identity server
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// When the application was registered
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Last time the application information was updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Flag indicating if the application is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Optional database connection string or identifier for the application's user database
        /// </summary>
        public string DatabaseIdentifier { get; set; }

        /// <summary>
        /// The user ID field name in the application's database
        /// </summary>
        public string UserIdFieldName { get; set; } = "UserId";
    }
} 