using IdentityMapping.ApiClient.Models;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.ApiClient;

public class UserMappingClient : IdentityMappingClient, IUserMappingClient
{
    public UserMappingClient(HttpClient httpClient, ILogger<UserMappingClient> logger) 
        : base(httpClient, logger)
    {
    }

    public async Task<UserMappingDto?> GetUserMappingAsync(string userId, string externalSystem, CancellationToken cancellationToken = default)
    {
        return await GetAsync<UserMappingDto>($"api/user-mappings/{userId}/{externalSystem}", cancellationToken);
    }

    public async Task<List<UserMappingDto>> GetUserMappingsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<UserMappingDto>>($"api/user-mappings/{userId}", cancellationToken) ?? new List<UserMappingDto>();
    }

    public async Task<UserMappingDto?> GetUserMappingByPhoneNumberAsync(string phoneNumber, string externalSystem, CancellationToken cancellationToken = default)
    {
        return await GetAsync<UserMappingDto>($"api/user-mappings/phone/{phoneNumber}/{externalSystem}", cancellationToken);
    }

    public async Task<List<UserMappingDto>> GetUserMappingsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<UserMappingDto>>($"api/user-mappings/phone/{phoneNumber}", cancellationToken) ?? new List<UserMappingDto>();
    }

    public async Task<UserMappingDto?> CreateUserMappingAsync(CreateUserMappingRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateUserMappingRequest, UserMappingDto>("api/user-mappings", request, cancellationToken);
    }

    public async Task<UserMappingDto?> UpdateUserMappingAsync(string mappingId, UpdateUserMappingRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<UpdateUserMappingRequest, UserMappingDto>($"api/user-mappings/{mappingId}", request, cancellationToken);
    }

    public async Task DeleteUserMappingAsync(string mappingId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/user-mappings/{mappingId}", cancellationToken);
    }
} 