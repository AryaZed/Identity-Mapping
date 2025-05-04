using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using IdentityMapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityMapping.Infrastructure.Repositories
{
    public class UserIdMappingRepository : IUserIdMappingRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public UserIdMappingRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<UserIdMapping> GetByIdAsync(string id)
        {
            return await _dbContext.UserIdMappings.FindAsync(id);
        }

        public async Task<UserIdMapping> CreateAsync(UserIdMapping mapping)
        {
            if (string.IsNullOrEmpty(mapping.Id))
            {
                mapping.Id = Guid.NewGuid().ToString();
            }

            mapping.CreatedAt = DateTime.UtcNow;

            // Check if a mapping already exists for this application and legacy user ID
            var existingMapping = await _dbContext.UserIdMappings
                .FirstOrDefaultAsync(m => m.ApplicationId == mapping.ApplicationId && m.LegacyUserId == mapping.LegacyUserId);

            if (existingMapping != null)
            {
                throw new InvalidOperationException($"A mapping already exists for application {mapping.ApplicationId} and legacy user ID {mapping.LegacyUserId}.");
            }

            await _dbContext.UserIdMappings.AddAsync(mapping);
            await _dbContext.SaveChangesAsync();

            return mapping;
        }

        public async Task<UserIdMapping> UpdateAsync(UserIdMapping mapping)
        {
            var existingMapping = await _dbContext.UserIdMappings.FindAsync(mapping.Id);
            if (existingMapping == null)
            {
                throw new KeyNotFoundException($"UserIdMapping with ID {mapping.Id} not found.");
            }

            // Ensure we don't change the creation time
            mapping.CreatedAt = existingMapping.CreatedAt;

            // Check if the update would create a duplicate mapping
            if (existingMapping.ApplicationId != mapping.ApplicationId || existingMapping.LegacyUserId != mapping.LegacyUserId)
            {
                var duplicateMapping = await _dbContext.UserIdMappings
                    .FirstOrDefaultAsync(m => m.ApplicationId == mapping.ApplicationId && 
                                        m.LegacyUserId == mapping.LegacyUserId &&
                                        m.Id != mapping.Id);

                if (duplicateMapping != null)
                {
                    throw new InvalidOperationException($"A mapping already exists for application {mapping.ApplicationId} and legacy user ID {mapping.LegacyUserId}.");
                }
            }

            _dbContext.Entry(existingMapping).CurrentValues.SetValues(mapping);
            await _dbContext.SaveChangesAsync();

            return mapping;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var mapping = await _dbContext.UserIdMappings.FindAsync(id);
            if (mapping == null)
            {
                return false;
            }

            _dbContext.UserIdMappings.Remove(mapping);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<UserIdMapping>> GetAllAsync(int skip = 0, int take = 100)
        {
            return await _dbContext.UserIdMappings
                .OrderBy(m => m.ApplicationId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserIdMapping>> GetByApplicationIdAsync(string applicationId, int skip = 0, int take = 100)
        {
            return await _dbContext.UserIdMappings
                .Where(m => m.ApplicationId == applicationId)
                .OrderBy(m => m.LegacyUserId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserIdMapping>> GetByCentralizedIdentityIdAsync(string centralizedIdentityId)
        {
            return await _dbContext.UserIdMappings
                .Where(m => m.CentralizedIdentityId == centralizedIdentityId)
                .OrderBy(m => m.ApplicationId)
                .ToListAsync();
        }

        public async Task<UserIdMapping> GetByLegacyUserIdAsync(string applicationId, string legacyUserId)
        {
            return await _dbContext.UserIdMappings
                .FirstOrDefaultAsync(m => m.ApplicationId == applicationId && m.LegacyUserId == legacyUserId);
        }

        public async Task<UserIdMapping> GetByApplicationAndIdentityIdAsync(string applicationId, string centralizedIdentityId)
        {
            return await _dbContext.UserIdMappings
                .FirstOrDefaultAsync(m => m.ApplicationId == applicationId && m.CentralizedIdentityId == centralizedIdentityId);
        }
    }
} 