using IdentityMapping.ApiClient;

namespace IdentityMapping.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IUserMappingClient _userMappingClient;

    public Worker(ILogger<Worker> logger, IUserMappingClient userMappingClient)
    {
        _logger = logger;
        _userMappingClient = userMappingClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Worker service started at: {time}", DateTimeOffset.Now);
            
            // Perform health check on API
            await PerformInitialHealthCheck(stoppingToken);
            
            // Service is healthy, let system handle MassTransit consumers
            while (!stoppingToken.IsCancellationRequested)
            {
                // Health check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Normal cancellation, no need to log as error
            _logger.LogInformation("Worker service stopped at: {time}", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in worker service");
        }
    }
    
    private async Task PerformInitialHealthCheck(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performing initial health check...");
        
        // Simple check to validate API client connectivity
        int retryCount = 0;
        const int maxRetries = 5;
        const int delaySeconds = 10;
        
        while (retryCount < maxRetries)
        {
            try
            {
                // Try to get a mock user mapping just to test connectivity
                await _userMappingClient.GetUserMappingsAsync("health-check", stoppingToken);
                _logger.LogInformation("Health check passed. API is reachable.");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Health check failed. Retry {RetryCount}/{MaxRetries} in {Delay} seconds", 
                    retryCount, maxRetries, delaySeconds);
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("Maximum retries reached. API may be unavailable.");
                    // Continue anyway, MassTransit will handle message processing when API becomes available
                    return;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }
    }
}
