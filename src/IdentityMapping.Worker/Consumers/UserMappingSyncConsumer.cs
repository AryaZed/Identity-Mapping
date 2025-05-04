using IdentityMapping.ApiClient;
using IdentityMapping.ApiClient.Models;
using IdentityMapping.Worker.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Worker.Consumers;

public class UserMappingSyncConsumer : IConsumer<SyncUserMapping>
{
    private readonly ILogger<UserMappingSyncConsumer> _logger;
    private readonly IUserMappingClient _userMappingClient;

    public UserMappingSyncConsumer(ILogger<UserMappingSyncConsumer> logger, IUserMappingClient userMappingClient)
    {
        _logger = logger;
        _userMappingClient = userMappingClient;
    }

    public async Task Consume(ConsumeContext<SyncUserMapping> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received user mapping sync request for user {UserId} in system {System}", 
            message.UserId, message.ExternalSystem);
        
        try
        {
            UserMappingDto? existingMapping = null;
            
            // First try to find mapping by phone number if provided
            if (!string.IsNullOrEmpty(message.PhoneNumber))
            {
                _logger.LogInformation("Attempting to find mapping by phone number {PhoneNumber}", message.PhoneNumber);
                existingMapping = await _userMappingClient.GetUserMappingByPhoneNumberAsync(
                    message.PhoneNumber,
                    message.ExternalSystem,
                    context.CancellationToken);
                
                if (existingMapping != null)
                {
                    _logger.LogInformation("Found mapping by phone number {PhoneNumber}", message.PhoneNumber);
                }
            }
            
            // If not found by phone, try by user ID
            if (existingMapping == null)
            {
                existingMapping = await _userMappingClient.GetUserMappingAsync(
                    message.UserId, 
                    message.ExternalSystem, 
                    context.CancellationToken);
            }
            
            if (existingMapping != null)
            {
                _logger.LogInformation("Updating existing mapping {MappingId} for user {UserId}", 
                    existingMapping.Id, message.UserId);
                
                // Update existing mapping
                await _userMappingClient.UpdateUserMappingAsync(
                    existingMapping.Id,
                    new UpdateUserMappingRequest 
                    { 
                        ExternalId = message.ExternalId,
                        AdditionalData = message.AdditionalData
                    },
                    context.CancellationToken);
            }
            else
            {
                _logger.LogInformation("Creating new mapping for user {UserId} with phone {PhoneNumber} in system {System}", 
                    message.UserId, message.PhoneNumber, message.ExternalSystem);
                
                // Create new mapping
                await _userMappingClient.CreateUserMappingAsync(
                    new CreateUserMappingRequest
                    {
                        UserId = message.UserId,
                        PhoneNumber = message.PhoneNumber,
                        ExternalSystem = message.ExternalSystem,
                        ExternalId = message.ExternalId,
                        AdditionalData = message.AdditionalData
                    },
                    context.CancellationToken);
            }
            
            _logger.LogInformation("Successfully processed user mapping for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user mapping sync for user {UserId}", message.UserId);
            // Rethrowing will trigger MassTransit retry policy
            throw;
        }
    }
} 