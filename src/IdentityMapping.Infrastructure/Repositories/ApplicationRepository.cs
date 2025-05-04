using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using IdentityMapping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityMapping.Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly IdentityMappingDbContext _dbContext;

        public ApplicationRepository(IdentityMappingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Application> GetByIdAsync(string id)
        {
            return await _dbContext.Applications.FindAsync(id);
        }

        public async Task<Application> CreateAsync(Application application)
        {
            if (string.IsNullOrEmpty(application.Id))
            {
                application.Id = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(application.ApiKey))
            {
                application.ApiKey = GenerateApiKey();
            }

            application.RegisteredAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            await _dbContext.Applications.AddAsync(application);
            await _dbContext.SaveChangesAsync();

            return application;
        }

        public async Task<Application> UpdateAsync(Application application)
        {
            var existingApplication = await _dbContext.Applications.FindAsync(application.Id);
            if (existingApplication == null)
            {
                throw new KeyNotFoundException($"Application with ID {application.Id} not found.");
            }

            application.UpdatedAt = DateTime.UtcNow;
            application.RegisteredAt = existingApplication.RegisteredAt; // Preserve the original registration time
            application.ApiKey = existingApplication.ApiKey; // Preserve the API key

            _dbContext.Entry(existingApplication).CurrentValues.SetValues(application);
            await _dbContext.SaveChangesAsync();

            return application;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var application = await _dbContext.Applications.FindAsync(id);
            if (application == null)
            {
                return false;
            }

            _dbContext.Applications.Remove(application);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Application>> GetAllAsync(int skip = 0, int take = 100)
        {
            return await _dbContext.Applications
                .OrderBy(a => a.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> ValidateApiKeyAsync(string applicationId, string apiKey)
        {
            var application = await _dbContext.Applications.FindAsync(applicationId);
            if (application == null || !application.IsActive)
            {
                return false;
            }

            return application.ApiKey == apiKey;
        }

        public async Task<string> RegenerateApiKeyAsync(string applicationId)
        {
            var application = await _dbContext.Applications.FindAsync(applicationId);
            if (application == null)
            {
                throw new KeyNotFoundException($"Application with ID {applicationId} not found.");
            }

            application.ApiKey = GenerateApiKey();
            application.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return application.ApiKey;
        }

        private string GenerateApiKey()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("/", "_").Replace("+", "-").TrimEnd('=');
        }
    }
} 