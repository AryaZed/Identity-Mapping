using System;

namespace IdentityMapping.Core.Models
{
    /// <summary>
    /// Defines a rule for dynamically mapping identity information between systems
    /// </summary>
    public class MappingRule
    {
        /// <summary>
        /// Unique identifier for this rule
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The application ID this rule belongs to
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Rule name for reference
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Rule description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Condition expression that determines if the rule applies
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// The type of mapping this rule handles
        /// </summary>
        public MappingRuleType RuleType { get; set; }

        /// <summary>
        /// Source identifier (e.g., claim type, role name)
        /// </summary>
        public string SourceIdentifier { get; set; }

        /// <summary>
        /// Target identifier (e.g., claim type, role name)
        /// </summary>
        public string TargetIdentifier { get; set; }

        /// <summary>
        /// Expression for transforming values
        /// </summary>
        public string TransformExpression { get; set; }

        /// <summary>
        /// Direction of the mapping
        /// </summary>
        public MappingDirection Direction { get; set; }

        /// <summary>
        /// Priority of the rule (lower values run first)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// When the rule was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the rule was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Types of mapping rules
    /// </summary>
    public enum MappingRuleType
    {
        /// <summary>
        /// Rule applies to claim mappings
        /// </summary>
        Claim = 0,

        /// <summary>
        /// Rule applies to role mappings
        /// </summary>
        Role = 1,

        /// <summary>
        /// Rule applies to user mappings
        /// </summary>
        User = 2
    }

    /// <summary>
    /// Direction of mapping rules
    /// </summary>
    public enum MappingDirection
    {
        /// <summary>
        /// Rule applies when mapping from legacy to centralized
        /// </summary>
        LegacyToCentralized = 0,

        /// <summary>
        /// Rule applies when mapping from centralized to legacy
        /// </summary>
        CentralizedToLegacy = 1,

        /// <summary>
        /// Rule applies in both directions
        /// </summary>
        Bidirectional = 2
    }
} 