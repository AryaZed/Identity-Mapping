namespace IdentityMapping.Worker.Messages;

public class SyncUserMapping
{
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ExternalSystem { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public Dictionary<string, string>? AdditionalData { get; set; }
} 