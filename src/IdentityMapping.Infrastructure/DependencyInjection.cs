using IdentityMapping.Core.Interfaces;
using IdentityMapping.Infrastructure.Data;
using IdentityMapping.Infrastructure.Grpc;
using IdentityMapping.Infrastructure.Messaging;
using IdentityMapping.Infrastructure.Messaging.Consumers;
using IdentityMapping.Infrastructure.Repositories;
using IdentityMapping.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityMapping.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<IdentityMappingDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(IdentityMappingDbContext).Assembly.FullName)));

            // Register repositories
            services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IUserIdMappingRepository, UserIdMappingRepository>();
            services.AddScoped<IRoleMappingRepository, RoleMappingRepository>();
            services.AddScoped<IClaimMappingRepository, ClaimMappingRepository>();
            services.AddScoped<IMappingRuleRepository, MappingRuleRepository>();

            // Register fallback strategy manager and strategies
            services.AddScoped<IFallbackStrategyManager, FallbackStrategyManager>();
            services.AddScoped<IFallbackStrategy, EmailBasedFallbackStrategy>();
            services.AddScoped<IFallbackStrategy, MobileBasedFallbackStrategy>();

            // Register identity mapping service
            services.AddScoped<IIdentityMappingService, IdentityMappingService>();
            
            // Register mapping rules engine
            services.AddScoped<IMappingRulesEngine, MappingRulesEngine>();

            // Add RabbitMQ integration
            services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
            services.AddSingleton<IMessageBroker, RabbitMQMessageBroker>();
            services.AddHostedService<MappingRuleConsumer>();
            services.AddHostedService<TransformConsumer>();

            // Add gRPC service
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
                options.MaxSendMessageSize = 5 * 1024 * 1024; // 5 MB
            });

            return services;
        }

        public static void MapGrpcServices(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map gRPC service
            endpoints.MapGrpcService<IdentityMappingGrpcService>();
        }

        /// <summary>
        /// Registers additional fallback strategies
        /// </summary>
        public static IServiceCollection AddFallbackStrategies(this IServiceCollection services)
        {
            // This method can be used to register additional fallback strategies
            // that aren't included in the core infrastructure
            
            return services;
        }
    }
} 