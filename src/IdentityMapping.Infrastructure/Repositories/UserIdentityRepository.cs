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
    public class UserIdentityRepository : IUserIdentityRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public UserIdentityRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<UserIdentity> GetByIdAsync(string id)
        {
            return await _dbContext.UserIdentities.FindAsync(id);
        }

        public async Task<UserIdentity> GetByEmailAsync(string email)
        {
            return await _dbContext.UserIdentities
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<UserIdentity> GetByMobileNumberAsync(string mobileNumber)
        {
            return await _dbContext.UserIdentities
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        }

        public async Task<UserIdentity> CreateAsync(UserIdentity userIdentity)
        {
            if (string.IsNullOrEmpty(userIdentity.Id))
            {
                userIdentity.Id = Guid.NewGuid().ToString();
            }

            userIdentity.CreatedAt = DateTime.UtcNow;
            userIdentity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.UserIdentities.AddAsync(userIdentity);
            await _dbContext.SaveChangesAsync();

            return userIdentity;
        }

        public async Task<UserIdentity> UpdateAsync(UserIdentity userIdentity)
        {
            var existingIdentity = await _dbContext.UserIdentities.FindAsync(userIdentity.Id);
            if (existingIdentity == null)
            {
                throw new KeyNotFoundException($"User identity with ID {userIdentity.Id} not found.");
            }

            userIdentity.UpdatedAt = DateTime.UtcNow;
            userIdentity.CreatedAt = existingIdentity.CreatedAt; // Preserve the original creation time

            _dbContext.Entry(existingIdentity).CurrentValues.SetValues(userIdentity);
            await _dbContext.SaveChangesAsync();

            return userIdentity;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var identity = await _dbContext.UserIdentities.FindAsync(id);
            if (identity == null)
            {
                return false;
            }

            _dbContext.UserIdentities.Remove(identity);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<UserIdentity>> GetAllAsync(int skip = 0, int take = 100)
        {
            return await _dbContext.UserIdentities
                .OrderBy(u => u.DisplayName)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> AddLegacyUserIdAsync(string identityId, string applicationId, string legacyUserId)
        {
            var identity = await _dbContext.UserIdentities.FindAsync(identityId);
            if (identity == null)
            {
                return false;
            }

            if (identity.LegacyUserIds == null)
            {
                identity.LegacyUserIds = new Dictionary<string, string>();
            }

            identity.LegacyUserIds[applicationId] = legacyUserId;
            identity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<UserIdentity> FindByLegacyUserIdAsync(string applicationId, string legacyUserId)
        {
            // Since we're storing the legacy IDs as a JSON-serialized dictionary,
            // we need to load all user identities and filter them in memory.
            // In a production environment, you might want to consider storing the mappings
            // in a separate table for better performance.
            var allIdentities = await _dbContext.UserIdentities.ToListAsync();
            
            return allIdentities.FirstOrDefault(u => 
                u.LegacyUserIds != null && 
                u.LegacyUserIds.TryGetValue(applicationId, out string value) && 
                value == legacyUserId);
        }
    }
} 