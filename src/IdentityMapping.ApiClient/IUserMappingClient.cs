using IdentityMapping.ApiClient.Models;

namespace IdentityMapping.ApiClient;

public interface IUserMappingClient
{
    Task<UserMappingDto?> GetUserMappingAsync(string userId, string externalSystem, CancellationToken cancellationToken = default);
    Task<List<UserMappingDto>> GetUserMappingsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserMappingDto?> GetUserMappingByPhoneNumberAsync(string phoneNumber, string externalSystem, CancellationToken cancellationToken = default);
    Task<List<UserMappingDto>> GetUserMappingsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<UserMappingDto?> CreateUserMappingAsync(CreateUserMappingRequest request, CancellationToken cancellationToken = default);
    Task<UserMappingDto?> UpdateUserMappingAsync(string mappingId, UpdateUserMappingRequest request, CancellationToken cancellationToken = default);
    Task DeleteUserMappingAsync(string mappingId, CancellationToken cancellationToken = default);
} 