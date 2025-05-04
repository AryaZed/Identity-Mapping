using IdentityMapping.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityMapping.UserMappingService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUserMappingServices(this IServiceCollection services)
        {
            services.AddScoped<IUserMappingService, UserMappingService>();
            
            return services;
        }
    }
} 