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
    public class RoleMappingRepository : IRoleMappingRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public RoleMappingRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<RoleMapping> GetByIdAsync(string id)
        {
            return await _dbContext.RoleMappings.FindAsync(id);
        }

        public async Task<IEnumerable<RoleMapping>> GetByApplicationIdAsync(string applicationId)
        {
            return await _dbContext.RoleMappings
                .Where(r => r.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<RoleMapping> GetByLegacyRoleNameAsync(string applicationId, string legacyRoleName)
        {
            return await _dbContext.RoleMappings
                .FirstOrDefaultAsync(r => 
                    r.ApplicationId == applicationId && 
                    r.LegacyRoleName.ToLower() == legacyRoleName.ToLower());
        }

        public async Task<RoleMapping> GetByCentralizedRoleNameAsync(string applicationId, string centralizedRoleName)
        {
            return await _dbContext.RoleMappings
                .FirstOrDefaultAsync(r => 
                    r.ApplicationId == applicationId && 
                    r.CentralizedRoleName.ToLower() == centralizedRoleName.ToLower());
        }

        public async Task<RoleMapping> CreateAsync(RoleMapping roleMapping)
        {
            if (string.IsNullOrEmpty(roleMapping.Id))
            {
                roleMapping.Id = Guid.NewGuid().ToString();
            }

            roleMapping.CreatedAt = DateTime.UtcNow;

            await _dbContext.RoleMappings.AddAsync(roleMapping);
            await _dbContext.SaveChangesAsync();

            return roleMapping;
        }

        public async Task<RoleMapping> UpdateAsync(RoleMapping roleMapping)
        {
            var existingMapping = await _dbContext.RoleMappings.FindAsync(roleMapping.Id);
            if (existingMapping == null)
            {
                throw new KeyNotFoundException($"Role mapping with ID {roleMapping.Id} not found.");
            }

            // Preserve created date
            roleMapping.CreatedAt = existingMapping.CreatedAt;

            _dbContext.Entry(existingMapping).CurrentValues.SetValues(roleMapping);
            await _dbContext.SaveChangesAsync();

            return roleMapping;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var mapping = await _dbContext.RoleMappings.FindAsync(id);
            if (mapping == null)
            {
                return false;
            }

            _dbContext.RoleMappings.Remove(mapping);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
} 