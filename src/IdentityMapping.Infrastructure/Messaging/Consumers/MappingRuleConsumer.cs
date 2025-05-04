using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using IdentityMapping.Infrastructure.Messaging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityMapping.Infrastructure.Messaging.Consumers
{
    public class MappingRuleConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageBroker _messageBroker;
        private readonly ILogger<MappingRuleConsumer> _logger;
        private readonly RabbitMQSettings _settings;

        public MappingRuleConsumer(
            IServiceProvider serviceProvider,
            IMessageBroker messageBroker,
            IOptions<RabbitMQSettings> settings,
            ILogger<MappingRuleConsumer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _settings = settings.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("RabbitMQ integration is disabled. MappingRuleConsumer will not start.");
                return;
            }

            try
            {
                // Subscribe to create mapping rule messages
                await _messageBroker.SubscribeAsync<CreateMappingRuleMessage>(
                    "identity.mapping.rule.create", 
                    async message => await HandleCreateMessage(message));

                // Subscribe to update mapping rule messages
                await _messageBroker.SubscribeAsync<UpdateMappingRuleMessage>(
                    "identity.mapping.rule.update", 
                    async message => await HandleUpdateMessage(message));

                // Subscribe to delete mapping rule messages
                await _messageBroker.SubscribeAsync<DeleteMappingRuleMessage>(
                    "identity.mapping.rule.delete", 
                    async message => await HandleDeleteMessage(message));

                _logger.LogInformation("MappingRuleConsumer started and subscribed to rule queues");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting MappingRuleConsumer");
                throw;
            }
        }

        private async Task HandleCreateMessage(CreateMappingRuleMessage message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMappingRuleRepository>();

                // Create the mapping rule from the message
                var rule = new MappingRule
                {
                    Id = string.IsNullOrEmpty(message.Id) ? Guid.NewGuid().ToString() : message.Id,
                    ApplicationId = message.ApplicationId,
                    Name = message.Name,
                    Description = message.Description,
                    Condition = message.Condition,
                    RuleType = message.RuleType,
                    SourceIdentifier = message.SourceIdentifier,
                    TargetIdentifier = message.TargetIdentifier,
                    TransformExpression = message.TransformExpression,
                    Direction = message.Direction,
                    Priority = message.Priority,
                    IsEnabled = message.IsEnabled
                };

                var result = await repository.CreateAsync(rule);
                _logger.LogInformation($"Created mapping rule via message: {result.Id}");

                // Publish success notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleCreatedMessage
                    {
                        Id = result.Id,
                        ApplicationId = result.ApplicationId,
                        Name = result.Name,
                        CorrelationId = message.CorrelationId,
                        Success = true
                    }, message.ReplyTo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing create mapping rule message");
                
                // Publish failure notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleCreatedMessage
                    {
                        CorrelationId = message.CorrelationId,
                        Success = false,
                        ErrorMessage = ex.Message
                    }, message.ReplyTo);
                }
            }
        }

        private async Task HandleUpdateMessage(UpdateMappingRuleMessage message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMappingRuleRepository>();

                // Get existing rule
                var existingRule = await repository.GetByIdAsync(message.Id);
                if (existingRule == null)
                {
                    throw new KeyNotFoundException($"Mapping rule with ID {message.Id} not found");
                }

                // Update the mapping rule from the message
                existingRule.Name = message.Name;
                existingRule.Description = message.Description;
                existingRule.Condition = message.Condition;
                existingRule.RuleType = message.RuleType;
                existingRule.SourceIdentifier = message.SourceIdentifier;
                existingRule.TargetIdentifier = message.TargetIdentifier;
                existingRule.TransformExpression = message.TransformExpression;
                existingRule.Direction = message.Direction;
                existingRule.Priority = message.Priority;
                existingRule.IsEnabled = message.IsEnabled;

                var result = await repository.UpdateAsync(existingRule);
                _logger.LogInformation($"Updated mapping rule via message: {result.Id}");

                // Publish success notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleUpdatedMessage
                    {
                        Id = result.Id,
                        ApplicationId = result.ApplicationId,
                        CorrelationId = message.CorrelationId,
                        Success = true
                    }, message.ReplyTo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing update mapping rule message");
                
                // Publish failure notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleUpdatedMessage
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        Success = false,
                        ErrorMessage = ex.Message
                    }, message.ReplyTo);
                }
            }
        }

        private async Task HandleDeleteMessage(DeleteMappingRuleMessage message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMappingRuleRepository>();

                var success = await repository.DeleteAsync(message.Id);
                if (success)
                {
                    _logger.LogInformation($"Deleted mapping rule via message: {message.Id}");
                }
                else
                {
                    _logger.LogWarning($"Mapping rule not found for deletion: {message.Id}");
                }

                // Publish success notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleDeletedMessage
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        Success = success,
                        ErrorMessage = success ? null : "Rule not found"
                    }, message.ReplyTo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delete mapping rule message");
                
                // Publish failure notification if reply-to is provided
                if (!string.IsNullOrEmpty(message.ReplyTo))
                {
                    await _messageBroker.PublishAsync(new MappingRuleDeletedMessage
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        Success = false,
                        ErrorMessage = ex.Message
                    }, message.ReplyTo);
                }
            }
        }
    }
} 