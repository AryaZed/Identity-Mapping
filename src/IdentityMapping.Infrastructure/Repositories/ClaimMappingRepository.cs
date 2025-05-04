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
    public class ClaimMappingRepository : IClaimMappingRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public ClaimMappingRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ClaimMapping> GetByIdAsync(string id)
        {
            return await _dbContext.ClaimMappings.FindAsync(id);
        }

        public async Task<IEnumerable<ClaimMapping>> GetByApplicationIdAsync(string applicationId)
        {
            return await _dbContext.ClaimMappings
                .Where(c => c.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<ClaimMapping> GetByLegacyClaimTypeAsync(string applicationId, string legacyClaimType)
        {
            return await _dbContext.ClaimMappings
                .FirstOrDefaultAsync(c => 
                    c.ApplicationId == applicationId && 
                    c.LegacyClaimType.ToLower() == legacyClaimType.ToLower());
        }

        public async Task<ClaimMapping> GetByCentralizedClaimTypeAsync(string applicationId, string centralizedClaimType)
        {
            return await _dbContext.ClaimMappings
                .FirstOrDefaultAsync(c => 
                    c.ApplicationId == applicationId && 
                    c.CentralizedClaimType.ToLower() == centralizedClaimType.ToLower());
        }

        public async Task<ClaimMapping> CreateAsync(ClaimMapping claimMapping)
        {
            if (string.IsNullOrEmpty(claimMapping.Id))
            {
                claimMapping.Id = Guid.NewGuid().ToString();
            }

            claimMapping.CreatedAt = DateTime.UtcNow;

            await _dbContext.ClaimMappings.AddAsync(claimMapping);
            await _dbContext.SaveChangesAsync();

            return claimMapping;
        }

        public async Task<ClaimMapping> UpdateAsync(ClaimMapping claimMapping)
        {
            var existingMapping = await _dbContext.ClaimMappings.FindAsync(claimMapping.Id);
            if (existingMapping == null)
            {
                throw new KeyNotFoundException($"Claim mapping with ID {claimMapping.Id} not found.");
            }

            // Preserve created date
            claimMapping.CreatedAt = existingMapping.CreatedAt;

            _dbContext.Entry(existingMapping).CurrentValues.SetValues(claimMapping);
            await _dbContext.SaveChangesAsync();

            return claimMapping;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var mapping = await _dbContext.ClaimMappings.FindAsync(id);
            if (mapping == null)
            {
                return false;
            }

            _dbContext.ClaimMappings.Remove(mapping);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
} 