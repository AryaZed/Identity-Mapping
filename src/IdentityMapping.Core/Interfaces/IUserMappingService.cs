using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;

namespace IdentityMapping.Core.Interfaces
{
    /// <summary>
    /// Service responsible for managing mappings between legacy user IDs and centralized identities
    /// </summary>
    public interface IUserMappingService
    {
        /// <summary>
        /// Creates a new mapping between a legacy user ID and a centralized identity
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyUserId">The legacy user ID</param>
        /// <param name="centralizedIdentityId">The centralized identity ID</param>
        /// <returns>The created mapping</returns>
        Task<UserIdMapping> CreateMappingAsync(string applicationId, string legacyUserId, string centralizedIdentityId);

        /// <summary>
        /// Gets a mapping by ID
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <returns>The mapping or null if not found</returns>
        Task<UserIdMapping> GetMappingByIdAsync(string mappingId);

        /// <summary>
        /// Gets all mappings for a specific application
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>A list of mappings</returns>
        Task<IEnumerable<UserIdMapping>> GetMappingsByApplicationAsync(string applicationId, int skip = 0, int take = 100);

        /// <summary>
        /// Gets the centralized identity ID for a legacy user
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="legacyUserId">The legacy user ID</param>
        /// <param name="useFallback">Whether to use fallback strategies if the mapping is not found</param>
        /// <param name="fallbackContext">Optional fallback context with additional information</param>
        /// <returns>The centralized identity ID or null if not found and fallback failed</returns>
        Task<string> GetCentralizedIdentityIdAsync(string applicationId, string legacyUserId, bool useFallback = false, FallbackContext? fallbackContext = null);

        /// <summary>
        /// Gets the legacy user ID for a specific application and centralized identity
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="centralizedIdentityId">The centralized identity ID</param>
        /// <param name="useFallback">Whether to use fallback strategies if the mapping is not found</param>
        /// <param name="fallbackContext">Optional fallback context with additional information</param>
        /// <returns>The legacy user ID or null if not found and fallback failed</returns>
        Task<string> GetLegacyUserIdAsync(string applicationId, string centralizedIdentityId, bool useFallback = false, FallbackContext? fallbackContext = null);

        /// <summary>
        /// Validates a mapping
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <returns>True if validated successfully, false otherwise</returns>
        Task<bool> ValidateMappingAsync(string mappingId);

        /// <summary>
        /// Deletes a mapping
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteMappingAsync(string mappingId);

        /// <summary>
        /// Updates an application's database to use centralized identities
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="options">Migration options</param>
        /// <returns>A migration report</returns>
        Task<MigrationReport> MigrateApplicationToIdentityServerAsync(string applicationId, MigrationOptions options);

        /// <summary>
        /// Tries to find or create a mapping using fallback strategies
        /// </summary>
        /// <param name="applicationId">The application ID</param>
        /// <param name="context">Fallback context with information to use</param>
        /// <returns>The fallback result</returns>
        Task<FallbackResult> TryFallbackAsync(string applicationId, FallbackContext context);
    }

    /// <summary>
    /// Options for migrating an application to use centralized identities
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// If true, creates a backup of the database before migration
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// If true, runs in test mode without making actual changes
        /// </summary>
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Maximum number of records to migrate
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// If true, uses fallback strategies for mappings not found during migration
        /// </summary>
        public bool UseFallbackStrategies { get; set; } = false;

        /// <summary>
        /// If true, skips records that fail to migrate instead of stopping the process
        /// </summary>
        public bool ContinueOnError { get; set; } = true;
    }

    /// <summary>
    /// Report of a migration operation
    /// </summary>
    public class MigrationReport
    {
        /// <summary>
        /// Number of records successfully migrated
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of records that failed to migrate
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Number of records that were successfully handled by fallback strategies
        /// </summary>
        public int FallbackSuccessCount { get; set; }

        /// <summary>
        /// Number of records where fallback strategies failed
        /// </summary>
        public int FallbackFailureCount { get; set; }

        /// <summary>
        /// Details of failures
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// If a backup was created, the backup identifier or location
        /// </summary>
        public string? BackupIdentifier { get; set; }

        /// <summary>
        /// When the migration started
        /// </summary>
        public System.DateTime StartTime { get; set; }

        /// <summary>
        /// When the migration completed
        /// </summary>
        public System.DateTime EndTime { get; set; }

        /// <summary>
        /// If the migration was a dry run
        /// </summary>
        public bool WasDryRun { get; set; }

        /// <summary>
        /// Migration duration in seconds
        /// </summary>
        public double DurationSeconds => (EndTime - StartTime).TotalSeconds;
    }
} 