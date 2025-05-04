using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Services
{
    public class FallbackStrategyManager : IFallbackStrategyManager
    {
        private readonly List<IFallbackStrategy> _strategies = new List<IFallbackStrategy>();
        private readonly ILogger<FallbackStrategyManager> _logger;

        public FallbackStrategyManager(ILogger<FallbackStrategyManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RegisterStrategy(IFallbackStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _strategies.Add(strategy);
            // Sort by priority after adding
            _strategies.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            _logger.LogInformation($"Registered fallback strategy: {strategy.GetType().Name} with priority {strategy.Priority}");
        }

        public async Task<FallbackResult> ExecuteFallbackAsync(string applicationId, FallbackContext context)
        {
            _logger.LogInformation($"Executing fallback strategies for application {applicationId}");
            
            if (_strategies.Count == 0)
            {
                _logger.LogWarning("No fallback strategies registered");
                return FallbackResult.CreateFailure("No fallback strategies registered");
            }

            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(applicationId, context))
                {
                    _logger.LogInformation($"Trying fallback strategy: {strategy.GetType().Name}");
                    
                    try
                    {
                        var result = await strategy.HandleAsync(applicationId, context);
                        
                        if (result.Success)
                        {
                            _logger.LogInformation($"Fallback strategy {strategy.GetType().Name} succeeded: {result.Message}");
                            return result;
                        }
                        
                        _logger.LogInformation($"Fallback strategy {strategy.GetType().Name} failed: {result.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error executing fallback strategy {strategy.GetType().Name}");
                    }
                }
                else
                {
                    _logger.LogDebug($"Fallback strategy {strategy.GetType().Name} cannot handle this scenario");
                }
            }

            _logger.LogWarning("All fallback strategies failed or were not applicable");
            return FallbackResult.CreateFailure("All fallback strategies failed or were not applicable");
        }

        public IReadOnlyList<IFallbackStrategy> GetRegisteredStrategies()
        {
            return _strategies.AsReadOnly();
        }
    }
} 