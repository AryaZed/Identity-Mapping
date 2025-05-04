using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.UserMappingService
{
    public class UserMappingService : IUserMappingService
    {
        private readonly IUserIdMappingRepository _mappingRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserIdentityRepository _userIdentityRepository;
        private readonly IFallbackStrategyManager _fallbackStrategyManager;
        private readonly ILogger<UserMappingService> _logger;

        public UserMappingService(
            IUserIdMappingRepository mappingRepository,
            IApplicationRepository applicationRepository,
            IUserIdentityRepository userIdentityRepository,
            IFallbackStrategyManager fallbackStrategyManager,
            ILogger<UserMappingService> logger)
        {
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userIdentityRepository = userIdentityRepository ?? throw new ArgumentNullException(nameof(userIdentityRepository));
            _fallbackStrategyManager = fallbackStrategyManager ?? throw new ArgumentNullException(nameof(fallbackStrategyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserIdMapping> CreateMappingAsync(string applicationId, string legacyUserId, string centralizedIdentityId)
        {
            // Validate application exists
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
            {
                throw new KeyNotFoundException($"Application with ID {applicationId} not found.");
            }

            // Validate that centralized identity exists
            var identity = await _userIdentityRepository.GetByIdAsync(centralizedIdentityId);
            if (identity == null)
            {
                throw new KeyNotFoundException($"Centralized identity with ID {centralizedIdentityId} not found.");
            }

            // Check if mapping already exists
            var existingMapping = await _mappingRepository.GetByLegacyUserIdAsync(applicationId, legacyUserId);
            if (existingMapping != null)
            {
                throw new InvalidOperationException($"A mapping already exists for application {applicationId} and legacy user ID {legacyUserId}.");
            }

            // Create the mapping
            var mapping = new UserIdMapping
            {
                ApplicationId = applicationId,
                LegacyUserId = legacyUserId,
                CentralizedIdentityId = centralizedIdentityId,
                CreatedAt = DateTime.UtcNow,
                IsValidated = false
            };

            var result = await _mappingRepository.CreateAsync(mapping);

            // Also add to the UserIdentity's legacy IDs collection
            await _userIdentityRepository.AddLegacyUserIdAsync(centralizedIdentityId, applicationId, legacyUserId);

            return result;
        }

        public async Task<UserIdMapping> GetMappingByIdAsync(string mappingId)
        {
            return await _mappingRepository.GetByIdAsync(mappingId);
        }

        public async Task<IEnumerable<UserIdMapping>> GetMappingsByApplicationAsync(string applicationId, int skip = 0, int take = 100)
        {
            return await _mappingRepository.GetByApplicationIdAsync(applicationId, skip, take);
        }

        public async Task<string> GetCentralizedIdentityIdAsync(string applicationId, string legacyUserId, bool useFallback = false, FallbackContext? fallbackContext = null)
        {
            var mapping = await _mappingRepository.GetByLegacyUserIdAsync(applicationId, legacyUserId);
            
            if (mapping != null)
            {
                return mapping.CentralizedIdentityId;
            }
            
            if (useFallback)
            {
                // Prepare fallback context if not provided
                fallbackContext ??= new FallbackContext
                {
                    LegacyUserId = legacyUserId
                };

                // Ensure legacy user ID is set in the context
                if (string.IsNullOrEmpty(fallbackContext.LegacyUserId))
                {
                    fallbackContext.LegacyUserId = legacyUserId;
                }

                // Try to get additional user information from application database if possible
                // This part would be specific to each application, potentially retrieving email, mobile, etc.
                // This is just a placeholder for where you would add that logic
                
                var fallbackResult = await TryFallbackAsync(applicationId, fallbackContext);
                
                if (fallbackResult.Success && fallbackResult.CentralizedIdentityId != null)
                {
                    _logger.LogInformation($"Found centralized identity ID {fallbackResult.CentralizedIdentityId} using fallback strategy");
                    return fallbackResult.CentralizedIdentityId;
                }
                
                _logger.LogWarning($"Fallback strategies failed to find a centralized identity ID for legacy user ID {legacyUserId}");
            }
            
            return null;
        }

        public async Task<string> GetLegacyUserIdAsync(string applicationId, string centralizedIdentityId, bool useFallback = false, FallbackContext? fallbackContext = null)
        {
            var mapping = await _mappingRepository.GetByApplicationAndIdentityIdAsync(applicationId, centralizedIdentityId);
            
            if (mapping != null)
            {
                return mapping.LegacyUserId;
            }
            
            if (useFallback)
            {
                // Prepare fallback context if not provided
                fallbackContext ??= new FallbackContext
                {
                    CentralizedIdentityId = centralizedIdentityId
                };

                // Ensure centralized identity ID is set in the context
                if (string.IsNullOrEmpty(fallbackContext.CentralizedIdentityId))
                {
                    fallbackContext.CentralizedIdentityId = centralizedIdentityId;
                }

                // Try to get additional information for fallback
                if (fallbackContext.UserEmail == null || fallbackContext.UserMobileNumber == null)
                {
                    var identity = await _userIdentityRepository.GetByIdAsync(centralizedIdentityId);
                    if (identity != null)
                    {
                        fallbackContext.UserEmail = identity.Email;
                        fallbackContext.UserDisplayName = identity.DisplayName;
                        fallbackContext.UserMobileNumber = identity.MobileNumber;
                    }
                }

                var fallbackResult = await TryFallbackAsync(applicationId, fallbackContext);
                
                if (fallbackResult.Success && fallbackResult.LegacyUserId != null)
                {
                    _logger.LogInformation($"Found legacy user ID {fallbackResult.LegacyUserId} using fallback strategy");
                    return fallbackResult.LegacyUserId;
                }
                
                _logger.LogWarning($"Fallback strategies failed to find a legacy user ID for centralized identity ID {centralizedIdentityId}");
            }
            
            return null;
        }

        public async Task<bool> ValidateMappingAsync(string mappingId)
        {
            var mapping = await _mappingRepository.GetByIdAsync(mappingId);
            if (mapping == null)
            {
                return false;
            }

            mapping.IsValidated = true;
            await _mappingRepository.UpdateAsync(mapping);
            return true;
        }

        public async Task<bool> DeleteMappingAsync(string mappingId)
        {
            return await _mappingRepository.DeleteAsync(mappingId);
        }

        public async Task<FallbackResult> TryFallbackAsync(string applicationId, FallbackContext context)
        {
            return await _fallbackStrategyManager.ExecuteFallbackAsync(applicationId, context);
        }

        public async Task<MigrationReport> MigrateApplicationToIdentityServerAsync(string applicationId, MigrationOptions options)
        {
            _logger.LogInformation($"Starting migration for application {applicationId} with options: DryRun={options.DryRun}, CreateBackup={options.CreateBackup}, BatchSize={options.BatchSize}, UseFallback={options.UseFallbackStrategies}");

            var report = new MigrationReport
            {
                StartTime = DateTime.UtcNow,
                WasDryRun = options.DryRun
            };

            try
            {
                // Get application details
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    throw new KeyNotFoundException($"Application with ID {applicationId} not found.");
                }

                if (string.IsNullOrEmpty(application.DatabaseIdentifier))
                {
                    throw new InvalidOperationException($"Application {applicationId} does not have a database identifier configured.");
                }

                // Create a connection to the application database
                using (var connection = new SqlConnection(application.DatabaseIdentifier))
                {
                    await connection.OpenAsync();

                    // Create a backup if requested
                    if (options.CreateBackup && !options.DryRun)
                    {
                        var backupName = $"{connection.Database}_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                        var backupPath = $"C:\\Backups\\{backupName}.bak";
                        
                        try
                        {
                            using (var command = new SqlCommand($"BACKUP DATABASE [{connection.Database}] TO DISK = '{backupPath}'", connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            report.BackupIdentifier = backupPath;
                            _logger.LogInformation($"Created backup at {backupPath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create database backup");
                            if (!options.ContinueOnError)
                            {
                                throw;
                            }
                            report.Errors.Add($"Failed to create backup: {ex.Message}");
                        }
                    }

                    // Get all mappings for the application
                    var mappings = await _mappingRepository.GetByApplicationIdAsync(applicationId, 0, int.MaxValue);
                    var mappingsDict = new Dictionary<string, string>();
                    foreach (var mapping in mappings)
                    {
                        if (mapping.IsValidated)
                        {
                            mappingsDict[mapping.LegacyUserId] = mapping.CentralizedIdentityId;
                        }
                    }

                    if (mappingsDict.Count == 0)
                    {
                        throw new InvalidOperationException($"No validated mappings found for application {applicationId}.");
                    }

                    // Find tables with the user ID field
                    var userIdField = application.UserIdFieldName;
                    var tables = new List<string>();

                    using (var command = new SqlCommand(
                        @"SELECT t.name FROM sys.tables t
                          JOIN sys.columns c ON t.object_id = c.object_id
                          WHERE c.name = @userIdField", connection))
                    {
                        command.Parameters.AddWithValue("@userIdField", userIdField);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }
                    }

                    _logger.LogInformation($"Found {tables.Count} tables with {userIdField} field");

                    // Process each table
                    foreach (var table in tables)
                    {
                        _logger.LogInformation($"Processing table {table}");

                        if (options.DryRun)
                        {
                            // In dry run mode, just count the records that would be updated
                            using (var command = new SqlCommand(
                                $"SELECT COUNT(*) FROM [{table}] WHERE [{userIdField}] IN (SELECT CAST(k AS VARCHAR) FROM OPENJSON(@legacyIds, '$'))", connection))
                            {
                                command.Parameters.AddWithValue("@legacyIds", System.Text.Json.JsonSerializer.Serialize(mappingsDict.Keys));
                                var count = (int)await command.ExecuteScalarAsync();
                                report.SuccessCount += count;
                                _logger.LogInformation($"Would update {count} records in table {table}");
                            }

                            // If using fallback, count records that would need fallback
                            if (options.UseFallbackStrategies)
                            {
                                using (var command = new SqlCommand(
                                    $"SELECT COUNT(*) FROM [{table}] WHERE [{userIdField}] NOT IN (SELECT CAST(k AS VARCHAR) FROM OPENJSON(@legacyIds, '$'))", connection))
                                {
                                    command.Parameters.AddWithValue("@legacyIds", System.Text.Json.JsonSerializer.Serialize(mappingsDict.Keys));
                                    var count = (int)await command.ExecuteScalarAsync();
                                    _logger.LogInformation($"Would attempt fallback for {count} records in table {table}");
                                }
                            }
                        }
                        else
                        {
                            // In real mode, actually update the records
                            // Process in batches to avoid long-running transactions
                            var processedCount = 0;
                            var errorCount = 0;
                            var fallbackSuccessCount = 0;
                            var fallbackFailureCount = 0;

                            // First, update records with known mappings
                            foreach (var mapping in mappingsDict)
                            {
                                try
                                {
                                    using (var command = new SqlCommand(
                                        $"UPDATE [{table}] SET [{userIdField}] = @newId WHERE [{userIdField}] = @oldId", connection))
                                    {
                                        command.Parameters.AddWithValue("@newId", mapping.Value);
                                        command.Parameters.AddWithValue("@oldId", mapping.Key);
                                        
                                        var affectedRows = await command.ExecuteNonQueryAsync();
                                        processedCount += affectedRows;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    var error = $"Error updating {table} for legacy ID {mapping.Key}: {ex.Message}";
                                    report.Errors.Add(error);
                                    _logger.LogError(ex, error);
                                    
                                    if (!options.ContinueOnError)
                                    {
                                        throw;
                                    }
                                }

                                if (processedCount + errorCount >= options.BatchSize)
                                {
                                    break;
                                }
                            }

                            // If using fallback strategies, try to handle unmapped records
                            if (options.UseFallbackStrategies)
                            {
                                // Get unmapped legacy IDs
                                var unmappedIds = new List<string>();
                                using (var command = new SqlCommand(
                                    $"SELECT DISTINCT [{userIdField}] FROM [{table}] WHERE [{userIdField}] NOT IN (SELECT CAST(k AS VARCHAR) FROM OPENJSON(@legacyIds, '$'))", connection))
                                {
                                    command.Parameters.AddWithValue("@legacyIds", System.Text.Json.JsonSerializer.Serialize(mappingsDict.Keys));
                                    
                                    using (var reader = await command.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            unmappedIds.Add(reader.GetString(0));
                                        }
                                    }
                                }
                                
                                _logger.LogInformation($"Found {unmappedIds.Count} unmapped legacy IDs in table {table}");

                                // Try fallback for each unmapped ID
                                foreach (var legacyId in unmappedIds)
                                {
                                    try
                                    {
                                        // Create a minimal fallback context
                                        var context = new FallbackContext
                                        {
                                            LegacyUserId = legacyId
                                        };
                                        
                                        // Try to get email, mobile or other info from the database if possible
                                        try {
                                            using (var infoCommand = new SqlCommand(
                                                $"SELECT Email, PhoneNumber, DisplayName FROM Users WHERE Id = @userId", connection))
                                            {
                                                infoCommand.Parameters.AddWithValue("@userId", legacyId);
                                                
                                                using (var reader = await infoCommand.ExecuteReaderAsync())
                                                {
                                                    if (await reader.ReadAsync())
                                                    {
                                                        if (!reader.IsDBNull(0))
                                                            context.UserEmail = reader.GetString(0);
                                                        
                                                        if (!reader.IsDBNull(1))
                                                            context.UserMobileNumber = reader.GetString(1);
                                                        
                                                        if (!reader.IsDBNull(2))
                                                            context.UserDisplayName = reader.GetString(2);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, $"Failed to retrieve additional user info for legacy ID {legacyId}");
                                        }
                                        
                                        var fallbackResult = await TryFallbackAsync(applicationId, context);
                                        
                                        if (fallbackResult.Success && fallbackResult.Mapping != null)
                                        {
                                            // Update records with the new mapping
                                            using (var command = new SqlCommand(
                                                $"UPDATE [{table}] SET [{userIdField}] = @newId WHERE [{userIdField}] = @oldId", connection))
                                            {
                                                command.Parameters.AddWithValue("@newId", fallbackResult.Mapping.CentralizedIdentityId);
                                                command.Parameters.AddWithValue("@oldId", fallbackResult.Mapping.LegacyUserId);
                                                
                                                var affectedRows = await command.ExecuteNonQueryAsync();
                                                fallbackSuccessCount += affectedRows;
                                            }
                                        }
                                        else
                                        {
                                            fallbackFailureCount++;
                                            _logger.LogWarning($"Fallback failed for legacy ID {legacyId}: {fallbackResult.Message}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        fallbackFailureCount++;
                                        var error = $"Error applying fallback for legacy ID {legacyId}: {ex.Message}";
                                        report.Errors.Add(error);
                                        _logger.LogError(ex, error);
                                        
                                        if (!options.ContinueOnError)
                                        {
                                            throw;
                                        }
                                    }
                                }
                            }

                            report.SuccessCount += processedCount;
                            report.FailureCount += errorCount;
                            report.FallbackSuccessCount += fallbackSuccessCount;
                            report.FallbackFailureCount += fallbackFailureCount;
                            
                            _logger.LogInformation($"Updated {processedCount} records in table {table} with {errorCount} errors. Fallback: {fallbackSuccessCount} successful, {fallbackFailureCount} failed.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Migration failed for application {applicationId}");
                report.Errors.Add($"Migration failed: {ex.Message}");
                report.FailureCount++;
            }
            finally
            {
                report.EndTime = DateTime.UtcNow;
            }

            return report;
        }
    }
} 