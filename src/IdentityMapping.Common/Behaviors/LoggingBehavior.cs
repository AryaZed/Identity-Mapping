using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("[START] {RequestType} {RequestId}", requestType, requestId);
        
        try
        {
            // Log request if not too large
            if (IsNotTooLargeToLog(request))
            {
                _logger.LogDebug("Request details: {Request}", JsonSerializer.Serialize(request));
            }
            
            var sw = Stopwatch.StartNew();
            var response = await next();
            sw.Stop();
            
            // Log response timing
            _logger.LogInformation("[END] {RequestType} {RequestId} completed in {ElapsedMilliseconds}ms", 
                requestType, requestId, sw.ElapsedMilliseconds);
            
            // Log response if not too large
            if (IsNotTooLargeToLog(response))
            {
                _logger.LogDebug("Response details: {Response}", JsonSerializer.Serialize(response));
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] {RequestType} {RequestId} failed: {ErrorMessage}", 
                requestType, requestId, ex.Message);
            throw;
        }
    }
    
    private bool IsNotTooLargeToLog<T>(T item)
    {
        try
        {
            if (item == null) return true;
            
            var json = JsonSerializer.Serialize(item);
            return json.Length < 10000; // Don't log if over 10KB
        }
        catch
        {
            return false;
        }
    }
} 