using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdentityMapping.ApiClient;

public class IdentityMappingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityMappingClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdentityMappingClient(HttpClient httpClient, ILogger<IdentityMappingClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending GET request to {RequestUri}", requestUri);
            
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making GET request to {RequestUri}", requestUri);
            throw;
        }
    }

    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending POST request to {RequestUri}", requestUri);
            
            var response = await _httpClient.PostAsJsonAsync(requestUri, request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making POST request to {RequestUri}", requestUri);
            throw;
        }
    }

    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending PUT request to {RequestUri}", requestUri);
            
            var response = await _httpClient.PutAsJsonAsync(requestUri, request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making PUT request to {RequestUri}", requestUri);
            throw;
        }
    }

    protected async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending DELETE request to {RequestUri}", requestUri);
            
            var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making DELETE request to {RequestUri}", requestUri);
            throw;
        }
    }
} 