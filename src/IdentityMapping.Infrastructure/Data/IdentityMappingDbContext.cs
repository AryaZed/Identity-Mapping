using IdentityMapping.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityMapping.Infrastructure.Data
{
    public class IdentityMappingDbContext : DbContext
    {
        public IdentityMappingDbContext(DbContextOptions<IdentityMappingDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserIdentity> UserIdentities { get; set; } = null!;
        public DbSet<UserIdMapping> UserIdMappings { get; set; } = null!;
        public DbSet<Application> Applications { get; set; } = null!;
        public DbSet<RoleMapping> RoleMappings { get; set; } = null!;
        public DbSet<ClaimMapping> ClaimMappings { get; set; } = null!;
        public DbSet<MappingRule> MappingRules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserIdentity
            modelBuilder.Entity<UserIdentity>(entity =>
            {
                entity.ToTable("UserIdentities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.EmailVerified).IsRequired();
                entity.Property(e => e.MobileNumber).HasMaxLength(50);
                entity.Property(e => e.MobileVerified).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Configure dictionary properties
                entity.Property(e => e.LegacyUserIds).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new Dictionary<string, string>());
                
                entity.Property(e => e.Claims).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new Dictionary<string, string>());
            });

            // Configure UserIdMapping
            modelBuilder.Entity<UserIdMapping>(entity =>
            {
                entity.ToTable("UserIdMappings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicationId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LegacyUserId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CentralizedIdentityId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Metadata).HasMaxLength(1000);
                entity.Property(e => e.IsValidated).IsRequired();

                // Create a unique index on ApplicationId and LegacyUserId
                entity.HasIndex(e => new { e.ApplicationId, e.LegacyUserId }).IsUnique();
                
                // Create an index on CentralizedIdentityId
                entity.HasIndex(e => e.CentralizedIdentityId);
            });

            // Configure Application
            modelBuilder.Entity<Application>(entity =>
            {
                entity.ToTable("Applications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.BaseUrl).HasMaxLength(500);
                entity.Property(e => e.ApiKey).HasMaxLength(100).IsRequired();
                entity.Property(e => e.RegisteredAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.DatabaseIdentifier).HasMaxLength(500);
                entity.Property(e => e.UserIdFieldName).HasMaxLength(100).IsRequired();
            });

            // Configure RoleMapping
            modelBuilder.Entity<RoleMapping>(entity =>
            {
                entity.ToTable("RoleMappings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicationId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LegacyRoleName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CentralizedRoleName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);

                // Create a unique index on ApplicationId and LegacyRoleName 
                entity.HasIndex(e => new { e.ApplicationId, e.LegacyRoleName }).IsUnique();
                
                // Create a unique index on ApplicationId and CentralizedRoleName
                entity.HasIndex(e => new { e.ApplicationId, e.CentralizedRoleName }).IsUnique();
            });

            // Configure ClaimMapping
            modelBuilder.Entity<ClaimMapping>(entity =>
            {
                entity.ToTable("ClaimMappings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicationId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LegacyClaimType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CentralizedClaimType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ValueTransformation).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IncludeInCentralized).IsRequired();
                entity.Property(e => e.IncludeInLegacy).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);

                // Create a unique index on ApplicationId and LegacyClaimType
                entity.HasIndex(e => new { e.ApplicationId, e.LegacyClaimType }).IsUnique();
                
                // Create a unique index on ApplicationId and CentralizedClaimType
                entity.HasIndex(e => new { e.ApplicationId, e.CentralizedClaimType }).IsUnique();
            });

            // Configure MappingRule
            modelBuilder.Entity<MappingRule>(entity =>
            {
                entity.ToTable("MappingRules");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicationId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Condition).HasMaxLength(1000);
                entity.Property(e => e.RuleType).IsRequired();
                entity.Property(e => e.SourceIdentifier).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TargetIdentifier).HasMaxLength(100);
                entity.Property(e => e.TransformExpression).HasMaxLength(1000);
                entity.Property(e => e.Direction).IsRequired();
                entity.Property(e => e.Priority).IsRequired();
                entity.Property(e => e.IsEnabled).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // Create an index on ApplicationId, RuleType and SourceIdentifier
                entity.HasIndex(e => new { e.ApplicationId, e.RuleType, e.SourceIdentifier });
                
                // Create an index on Priority
                entity.HasIndex(e => e.Priority);
            });
        }
    }
} 