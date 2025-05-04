using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityMapping.Core.Models;
using IdentityMapping.Infrastructure.Messaging.Models;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Messaging.Examples
{
    /// <summary>
    /// Example client demonstrating how to use RabbitMQ for identity mapping operations
    /// This is for demonstration purposes only and not part of the actual service
    /// </summary>
    public class RabbitMQExampleClient
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ILogger<RabbitMQExampleClient> _logger;

        public RabbitMQExampleClient(
            IMessageBroker messageBroker,
            ILogger<RabbitMQExampleClient> logger)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Example of how to create a mapping rule asynchronously via RabbitMQ
        /// </summary>
        public async Task CreateMappingRuleAsync(string applicationId, string name, MappingRuleType ruleType)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            // Create the request message
            var message = new CreateMappingRuleMessage
            {
                CorrelationId = correlationId,
                ApplicationId = applicationId,
                Name = name,
                Description = $"Sample rule created via RabbitMQ on {DateTime.Now}",
                RuleType = ruleType,
                SourceIdentifier = "source_identifier",
                TargetIdentifier = "target_identifier",
                Direction = MappingDirection.Bidirectional,
                Priority = 100,
                IsEnabled = true
            };

            // For fire-and-forget, don't set ReplyTo
            // For request-response pattern, set ReplyTo to a response queue
            message.ReplyTo = "identity.mapping.rule.created.response";
            
            // Subscribe to the response queue first if we're expecting a response
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                await _messageBroker.SubscribeAsync<MappingRuleCreatedMessage>(
                    message.ReplyTo,
                    async responseMsg => 
                    {
                        if (responseMsg.CorrelationId == correlationId)
                        {
                            if (responseMsg.Success)
                            {
                                _logger.LogInformation($"Rule created successfully: {responseMsg.Id}");
                            }
                            else
                            {
                                _logger.LogError($"Rule creation failed: {responseMsg.ErrorMessage}");
                            }
                        }
                    });
            }
            
            // Publish the message
            await _messageBroker.PublishAsync(message, "identity.mapping.rule.create");
            _logger.LogInformation($"Published create rule message with correlationId {correlationId}");
        }

        /// <summary>
        /// Example of how to transform a claim via RabbitMQ
        /// </summary>
        public async Task<Claim> TransformClaimAsync(string applicationId, Claim sourceClaim, MappingDirection direction)
        {
            var correlationId = Guid.NewGuid().ToString();
            var responseQueue = $"identity.mapping.transform.claim.response.{correlationId}";
            var resultTcs = new TaskCompletionSource<Claim>();
            
            // Create the request message
            var message = new TransformClaimMessage
            {
                CorrelationId = correlationId,
                ApplicationId = applicationId,
                ClaimType = sourceClaim.Type,
                ClaimValue = sourceClaim.Value,
                Direction = direction,
                ReplyTo = responseQueue
            };
            
            // Subscribe to the response queue
            await _messageBroker.SubscribeAsync<TransformClaimResponseMessage>(
                responseQueue,
                async responseMsg => 
                {
                    if (responseMsg.CorrelationId == correlationId)
                    {
                        if (responseMsg.Success)
                        {
                            if (!string.IsNullOrEmpty(responseMsg.ClaimType))
                            {
                                resultTcs.SetResult(new Claim(responseMsg.ClaimType, responseMsg.ClaimValue ?? string.Empty));
                            }
                            else
                            {
                                resultTcs.SetResult(null);
                            }
                        }
                        else
                        {
                            resultTcs.SetException(new Exception($"Transform failed: {responseMsg.ErrorMessage}"));
                        }
                    }
                });
            
            // Publish the message
            await _messageBroker.PublishAsync(message, "identity.mapping.transform.claim");
            _logger.LogInformation($"Published transform claim message with correlationId {correlationId}");
            
            // Wait for the response with a timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            if (await Task.WhenAny(resultTcs.Task, timeoutTask) == timeoutTask)
            {
                throw new TimeoutException("Timeout waiting for claim transformation response");
            }
            
            return await resultTcs.Task;
        }
    }
} 