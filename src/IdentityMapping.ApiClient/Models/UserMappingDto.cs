namespace IdentityMapping.ApiClient.Models;

public class UserMappingDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ExternalSystem { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public Dictionary<string, string>? AdditionalData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
} 