namespace IdentityMapping.Core.Models
{
    public class ClaimTransformationResult
    {
        public string OriginalType { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;
        public string TransformedType { get; set; } = string.Empty;
        public string TransformedValue { get; set; } = string.Empty;
    }
} 