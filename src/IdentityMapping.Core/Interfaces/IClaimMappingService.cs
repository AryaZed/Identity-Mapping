using IdentityMapping.Core.Models;
using IdentityMapping.IdentityServer.Controllers;

namespace IdentityMapping.Core.Interfaces
{
    public interface IClaimMappingService
    {
        Task<IEnumerable<ClaimMapping>> GetAllClaimMappingsAsync(int skip = 0, int take = 20);
        Task<ClaimMapping?> GetClaimMappingByIdAsync(string id);
        Task<IEnumerable<ClaimMapping>> GetClaimMappingsByApplicationAsync(string applicationId);
        Task<ClaimMapping> CreateClaimMappingAsync(string sourceApplicationId, string targetApplicationId, string sourceClaimType, string targetClaimType, string? transformationExpression);
        Task<ClaimMapping> UpdateClaimMappingAsync(string id, string sourceClaimType, string targetClaimType, string? transformationExpression);
        Task<bool> DeleteClaimMappingAsync(string id);
        Task<IEnumerable<ClaimTransformationResult>> TransformClaimsAsync(string sourceApplicationId, string targetApplicationId, IEnumerable<UserClaim> claims);
    }
} 