using IdentityMapping.Common;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityMapping.ApiClient;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityMappingApiClient(
        this IServiceCollection services, 
        string baseAddress)
    {
        services.AddResilienceHttpClient<IUserMappingClient, UserMappingClient>("UserMappingClient")
            .ConfigureHttpClient(client => 
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        
        return services;
    }
} 