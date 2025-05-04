using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Grpc.Net.Client;
using IdentityMapping.Core.Models;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Grpc.Examples
{
    /// <summary>
    /// Example client demonstrating how to use gRPC for identity mapping operations
    /// This is for demonstration purposes only and not part of the actual service
    /// </summary>
    public class GrpcExampleClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly IdentityMappingService.IdentityMappingServiceClient _client;
        private readonly ILogger<GrpcExampleClient> _logger;
        private bool _disposed;

        public GrpcExampleClient(string serverUrl, ILogger<GrpcExampleClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            try
            {
                // Create the gRPC channel
                _channel = GrpcChannel.ForAddress(serverUrl);
                
                // Create the client
                _client = new IdentityMappingService.IdentityMappingServiceClient(_channel);
                
                _logger.LogInformation($"Initialized gRPC client for {serverUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize gRPC client");
                throw;
            }
        }

        /// <summary>
        /// Transforms a claim using gRPC
        /// </summary>
        public async Task<System.Security.Claims.Claim> TransformClaimAsync(
            string applicationId, 
            System.Security.Claims.Claim sourceClaim, 
            MappingDirection direction,
            Dictionary<string, string> context = null)
        {
            try
            {
                // Create the request
                var request = new TransformClaimRequest
                {
                    ApplicationId = applicationId,
                    SourceClaim = new Claim { Type = sourceClaim.Type, Value = sourceClaim.Value },
                    Direction = (int)direction
                };
                
                // Add context data if provided
                if (context != null)
                {
                    foreach (var entry in context)
                    {
                        request.Context.Add(new ContextEntry { Key = entry.Key, Value = entry.Value });
                    }
                }
                
                // Call the service
                var response = await _client.TransformClaimAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception($"Transform claim failed: {response.ErrorMessage}");
                }
                
                if (response.TargetClaim == null)
                {
                    return null;
                }
                
                return new System.Security.Claims.Claim(response.TargetClaim.Type, response.TargetClaim.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling TransformClaim via gRPC");
                throw;
            }
        }

        /// <summary>
        /// Transforms multiple claims using gRPC
        /// </summary>
        public async Task<IEnumerable<System.Security.Claims.Claim>> TransformClaimsAsync(
            string applicationId, 
            IEnumerable<System.Security.Claims.Claim> sourceClaims, 
            MappingDirection direction,
            Dictionary<string, string> context = null)
        {
            try
            {
                // Create the request
                var request = new TransformClaimsRequest
                {
                    ApplicationId = applicationId,
                    Direction = (int)direction
                };
                
                // Add source claims
                foreach (var claim in sourceClaims)
                {
                    request.SourceClaims.Add(new Claim { Type = claim.Type, Value = claim.Value });
                }
                
                // Add context data if provided
                if (context != null)
                {
                    foreach (var entry in context)
                    {
                        request.Context.Add(new ContextEntry { Key = entry.Key, Value = entry.Value });
                    }
                }
                
                // Call the service
                var response = await _client.TransformClaimsAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception($"Transform claims failed: {response.ErrorMessage}");
                }
                
                return response.TargetClaims.Select(c => new System.Security.Claims.Claim(c.Type, c.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling TransformClaims via gRPC");
                throw;
            }
        }

        /// <summary>
        /// Creates a mapping rule using gRPC
        /// </summary>
        public async Task<MappingRuleProto> CreateMappingRuleAsync(
            string applicationId,
            string name,
            string sourceIdentifier,
            string targetIdentifier,
            MappingRuleType ruleType,
            MappingDirection direction)
        {
            try
            {
                var rule = new MappingRuleProto
                {
                    ApplicationId = applicationId,
                    Name = name,
                    Description = $"Rule created via gRPC on {DateTime.Now}",
                    SourceIdentifier = sourceIdentifier,
                    TargetIdentifier = targetIdentifier,
                    RuleType = (int)ruleType,
                    Direction = (int)direction,
                    Priority = 100,
                    IsEnabled = true
                };
                
                var request = new CreateMappingRuleRequest { Rule = rule };
                
                var response = await _client.CreateMappingRuleAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception($"Create mapping rule failed: {response.ErrorMessage}");
                }
                
                return response.Rule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CreateMappingRule via gRPC");
                throw;
            }
        }

        /// <summary>
        /// Gets all mapping rules for an application
        /// </summary>
        public async Task<IEnumerable<MappingRuleProto>> GetMappingRulesAsync(
            string applicationId,
            MappingRuleType? ruleType = null,
            MappingDirection? direction = null)
        {
            try
            {
                var request = new GetMappingRulesRequest
                {
                    ApplicationId = applicationId,
                    RuleType = ruleType.HasValue ? (int)ruleType.Value : -1,
                    Direction = direction.HasValue ? (int)direction.Value : -1
                };
                
                var response = await _client.GetMappingRulesAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception($"Get mapping rules failed: {response.ErrorMessage}");
                }
                
                return response.Rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetMappingRules via gRPC");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _channel?.Dispose();
            }

            _disposed = true;
        }
    }
} 