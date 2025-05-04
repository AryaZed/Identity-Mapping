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
    public class MappingRuleRepository : IMappingRuleRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public MappingRuleRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<MappingRule> GetByIdAsync(string id)
        {
            return await _dbContext.MappingRules.FindAsync(id);
        }

        public async Task<IEnumerable<MappingRule>> GetByApplicationIdAsync(string applicationId)
        {
            return await _dbContext.MappingRules
                .Where(r => r.ApplicationId == applicationId && r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<IEnumerable<MappingRule>> GetByTypeAsync(string applicationId, MappingRuleType ruleType)
        {
            return await _dbContext.MappingRules
                .Where(r => r.ApplicationId == applicationId && r.RuleType == ruleType && r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<IEnumerable<MappingRule>> GetByTypeAndDirectionAsync(string applicationId, MappingRuleType ruleType, MappingDirection direction)
        {
            return await _dbContext.MappingRules
                .Where(r => r.ApplicationId == applicationId && 
                       r.RuleType == ruleType && 
                       (r.Direction == direction || r.Direction == MappingDirection.Bidirectional) &&
                       r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<IEnumerable<MappingRule>> GetBySourceIdentifierAsync(string applicationId, string sourceIdentifier)
        {
            return await _dbContext.MappingRules
                .Where(r => r.ApplicationId == applicationId && 
                       r.SourceIdentifier == sourceIdentifier &&
                       r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<MappingRule> CreateAsync(MappingRule rule)
        {
            if (string.IsNullOrEmpty(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString();
            }

            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            await _dbContext.MappingRules.AddAsync(rule);
            await _dbContext.SaveChangesAsync();

            return rule;
        }

        public async Task<MappingRule> UpdateAsync(MappingRule rule)
        {
            var existingRule = await _dbContext.MappingRules.FindAsync(rule.Id);
            if (existingRule == null)
            {
                throw new KeyNotFoundException($"Mapping rule with ID {rule.Id} not found.");
            }

            // Preserve created date
            rule.CreatedAt = existingRule.CreatedAt;
            rule.UpdatedAt = DateTime.UtcNow;

            _dbContext.Entry(existingRule).CurrentValues.SetValues(rule);
            await _dbContext.SaveChangesAsync();

            return rule;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var rule = await _dbContext.MappingRules.FindAsync(id);
            if (rule == null)
            {
                return false;
            }

            _dbContext.MappingRules.Remove(rule);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
} 