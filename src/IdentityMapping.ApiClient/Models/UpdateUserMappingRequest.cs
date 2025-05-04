namespace IdentityMapping.ApiClient.Models;

public class UpdateUserMappingRequest
{
    public string? ExternalId { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
} 