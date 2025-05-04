using IdentityMapping.ApiClient;
using IdentityMapping.Common;
using IdentityMapping.Worker;
using IdentityMapping.Worker.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddCommonServices();
builder.Services.AddIdentityMappingApiClient(builder.Configuration["ApiClient:BaseAddress"] ?? "http://localhost:5000");

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<UserMappingSyncConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        
        // Configure endpoints
        cfg.ConfigureEndpoints(context);
    });
});

// Add worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
