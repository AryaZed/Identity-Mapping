using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.CSharp.RuntimeBinder;
using System.Linq.Dynamic.Core;

namespace IdentityMapping.Infrastructure.Services
{
    public class MappingRulesEngine : IMappingRulesEngine
    {
        private readonly IMappingRuleRepository _ruleRepository;
        private readonly ILogger<MappingRulesEngine> _logger;

        public MappingRulesEngine(
            IMappingRuleRepository ruleRepository,
            ILogger<MappingRulesEngine> logger)
        {
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Claim> TransformClaimAsync(string applicationId, Claim sourceClaim, MappingDirection direction, IDictionary<string, object> context = null)
        {
            if (sourceClaim == null)
                return null;

            try
            {
                // Create context if not provided
                context = context ?? new Dictionary<string, object>();
                
                // Add source claim to context
                context["sourceClaim"] = sourceClaim;
                context["sourceClaimType"] = sourceClaim.Type;
                context["sourceClaimValue"] = sourceClaim.Value;
                
                // Get applicable rules
                var rules = await _ruleRepository.GetByTypeAndDirectionAsync(applicationId, MappingRuleType.Claim, direction);
                
                // Filter rules that match the source claim type
                var applicableRules = rules.Where(r => 
                    r.SourceIdentifier.Equals(sourceClaim.Type, StringComparison.OrdinalIgnoreCase) || 
                    r.SourceIdentifier.Equals("*", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (!applicableRules.Any())
                {
                    _logger.LogDebug($"No applicable rules found for claim type {sourceClaim.Type}");
                    return null;
                }
                
                foreach (var rule in applicableRules)
                {
                    // Evaluate rule condition (if any)
                    if (!string.IsNullOrEmpty(rule.Condition))
                    {
                        if (!EvaluateCondition(rule.Condition, context))
                        {
                            _logger.LogDebug($"Rule {rule.Id} condition not met: {rule.Condition}");
                            continue;
                        }
                    }
                    
                    // Apply transformation
                    var targetValue = sourceClaim.Value;
                    if (!string.IsNullOrEmpty(rule.TransformExpression))
                    {
                        targetValue = ApplyTransformation(sourceClaim.Value, rule.TransformExpression, context);
                    }
                    
                    _logger.LogInformation($"Transformed claim {sourceClaim.Type} to {rule.TargetIdentifier} with value {targetValue}");
                    return new Claim(rule.TargetIdentifier, targetValue);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error transforming claim {sourceClaim.Type}");
                return null;
            }
        }

        public async Task<IEnumerable<Claim>> TransformClaimsAsync(string applicationId, IEnumerable<Claim> sourceClaims, MappingDirection direction, IDictionary<string, object> context = null)
        {
            if (sourceClaims == null || !sourceClaims.Any())
                return Enumerable.Empty<Claim>();

            var resultClaims = new List<Claim>();
            var claimArray = sourceClaims.ToArray();

            // Create context if not provided
            context = context ?? new Dictionary<string, object>();
            
            // Add all claims to context
            context["sourceClaims"] = claimArray;
            
            foreach (var claim in claimArray)
            {
                var transformedClaim = await TransformClaimAsync(applicationId, claim, direction, context);
                if (transformedClaim != null)
                {
                    resultClaims.Add(transformedClaim);
                }
            }

            return resultClaims;
        }

        public async Task<string> TransformRoleAsync(string applicationId, string sourceRole, MappingDirection direction, IDictionary<string, object> context = null)
        {
            if (string.IsNullOrEmpty(sourceRole))
                return null;

            try
            {
                // Create context if not provided
                context = context ?? new Dictionary<string, object>();
                
                // Add source role to context
                context["sourceRole"] = sourceRole;
                
                // Get applicable rules
                var rules = await _ruleRepository.GetByTypeAndDirectionAsync(applicationId, MappingRuleType.Role, direction);
                
                // Filter rules that match the source role
                var applicableRules = rules.Where(r => 
                    r.SourceIdentifier.Equals(sourceRole, StringComparison.OrdinalIgnoreCase) || 
                    r.SourceIdentifier.Equals("*", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (!applicableRules.Any())
                {
                    _logger.LogDebug($"No applicable rules found for role {sourceRole}");
                    return null;
                }
                
                foreach (var rule in applicableRules)
                {
                    // Evaluate rule condition (if any)
                    if (!string.IsNullOrEmpty(rule.Condition))
                    {
                        if (!EvaluateCondition(rule.Condition, context))
                        {
                            _logger.LogDebug($"Rule {rule.Id} condition not met: {rule.Condition}");
                            continue;
                        }
                    }
                    
                    // Apply transformation
                    var targetRole = rule.TargetIdentifier;
                    if (!string.IsNullOrEmpty(rule.TransformExpression))
                    {
                        targetRole = ApplyTransformation(sourceRole, rule.TransformExpression, context);
                    }
                    
                    _logger.LogInformation($"Transformed role {sourceRole} to {targetRole}");
                    return targetRole;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error transforming role {sourceRole}");
                return null;
            }
        }

        public async Task<IEnumerable<string>> TransformRolesAsync(string applicationId, IEnumerable<string> sourceRoles, MappingDirection direction, IDictionary<string, object> context = null)
        {
            if (sourceRoles == null || !sourceRoles.Any())
                return Enumerable.Empty<string>();

            var resultRoles = new List<string>();
            var roleArray = sourceRoles.ToArray();

            // Create context if not provided
            context = context ?? new Dictionary<string, object>();
            
            // Add all roles to context
            context["sourceRoles"] = roleArray;
            
            foreach (var role in roleArray)
            {
                var transformedRole = await TransformRoleAsync(applicationId, role, direction, context);
                if (!string.IsNullOrEmpty(transformedRole))
                {
                    resultRoles.Add(transformedRole);
                }
            }

            return resultRoles;
        }

        public bool EvaluateCondition(string condition, IDictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            try
            {
                // Simple expressions without DynamicLinq
                if (IsSimpleExpression(condition))
                {
                    return EvaluateSimpleExpression(condition, context);
                }

                // Use Dynamic LINQ for more complex expressions
                var paramExpression = CreateParameterExpression(context);
                var result = DynamicExpressionParser.ParseLambda(paramExpression, typeof(bool), condition).Compile().DynamicInvoke(paramExpression.Compile().DynamicInvoke());
                return (bool)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error evaluating condition: {condition}");
                return false;
            }
        }

        public string ApplyTransformation(string value, string expression, IDictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(expression))
                return value;

            try
            {
                // Simple transformations
                if (IsSimpleTransformation(expression))
                {
                    return ApplySimpleTransformation(value, expression, context);
                }

                // Handle more complex expressions using Dynamic LINQ
                context["value"] = value;
                var paramExpression = CreateParameterExpression(context);
                var result = DynamicExpressionParser.ParseLambda(paramExpression, typeof(string), expression).Compile().DynamicInvoke(paramExpression.Compile().DynamicInvoke());
                return result?.ToString() ?? value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying transformation: {expression}");
                return value;
            }
        }

        #region Helper Methods

        private bool IsSimpleExpression(string expression)
        {
            // Check if it's a simple expression like "value == 'test'" or "claim.Type == 'email'"
            return expression.Contains("==") || expression.Contains("!=") || expression.Contains("StartsWith") || expression.Contains("EndsWith") || expression.Contains("Contains");
        }

        private bool EvaluateSimpleExpression(string expression, IDictionary<string, object> context)
        {
            // Equal comparison
            if (expression.Contains("=="))
            {
                var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var leftValue = ResolveValue(parts[0].Trim(), context);
                    var rightValue = ResolveValue(parts[1].Trim(), context);
                    return string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Not equal comparison
            if (expression.Contains("!="))
            {
                var parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var leftValue = ResolveValue(parts[0].Trim(), context);
                    var rightValue = ResolveValue(parts[1].Trim(), context);
                    return !string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);
                }
            }

            // StartsWith
            var startsWithMatch = Regex.Match(expression, @"(.+)\.StartsWith\((.+)\)");
            if (startsWithMatch.Success)
            {
                var leftValue = ResolveValue(startsWithMatch.Groups[1].Value.Trim(), context);
                var rightValue = ResolveValue(startsWithMatch.Groups[2].Value.Trim(), context);
                return leftValue.StartsWith(rightValue, StringComparison.OrdinalIgnoreCase);
            }

            // EndsWith
            var endsWithMatch = Regex.Match(expression, @"(.+)\.EndsWith\((.+)\)");
            if (endsWithMatch.Success)
            {
                var leftValue = ResolveValue(endsWithMatch.Groups[1].Value.Trim(), context);
                var rightValue = ResolveValue(endsWithMatch.Groups[2].Value.Trim(), context);
                return leftValue.EndsWith(rightValue, StringComparison.OrdinalIgnoreCase);
            }

            // Contains
            var containsMatch = Regex.Match(expression, @"(.+)\.Contains\((.+)\)");
            if (containsMatch.Success)
            {
                var leftValue = ResolveValue(containsMatch.Groups[1].Value.Trim(), context);
                var rightValue = ResolveValue(containsMatch.Groups[2].Value.Trim(), context);
                return leftValue.Contains(rightValue, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private string ResolveValue(string value, IDictionary<string, object> context)
        {
            // Remove quotes if string literal
            if ((value.StartsWith("'") && value.EndsWith("'")) || (value.StartsWith("\"") && value.EndsWith("\"")))
                return value.Substring(1, value.Length - 2);

            // Check if it's a variable reference
            if (value.Contains("."))
            {
                var parts = value.Split('.');
                if (parts.Length == 2 && context.ContainsKey(parts[0]))
                {
                    var obj = context[parts[0]];
                    if (obj is Claim claim && parts[1] == "Value")
                        return claim.Value;
                    if (obj is Claim claim2 && parts[1] == "Type")
                        return claim2.Type;
                }
            }
            else if (context.ContainsKey(value))
            {
                var obj = context[value];
                if (obj is string strValue)
                    return strValue;
                return obj?.ToString() ?? string.Empty;
            }

            return value;
        }

        private bool IsSimpleTransformation(string expression)
        {
            // Simple prefix/suffix/replacement
            return expression.StartsWith("prefix:") || 
                   expression.StartsWith("suffix:") || 
                   expression.StartsWith("replace:") || 
                   expression.StartsWith("uppercase") || 
                   expression.StartsWith("lowercase");
        }

        private string ApplySimpleTransformation(string value, string expression, IDictionary<string, object> context)
        {
            // Add prefix
            if (expression.StartsWith("prefix:"))
            {
                return expression.Substring(7) + value;
            }

            // Add suffix
            if (expression.StartsWith("suffix:"))
            {
                return value + expression.Substring(7);
            }

            // Replace text
            if (expression.StartsWith("replace:"))
            {
                var replaceParts = expression.Substring(8).Split(':');
                if (replaceParts.Length == 2)
                {
                    return value.Replace(replaceParts[0], replaceParts[1]);
                }
            }

            // Uppercase
            if (expression.StartsWith("uppercase"))
            {
                return value.ToUpper();
            }

            // Lowercase
            if (expression.StartsWith("lowercase"))
            {
                return value.ToLower();
            }

            return value;
        }

        private System.Linq.Expressions.ParameterExpression CreateParameterExpression(IDictionary<string, object> context)
        {
            // Create a dynamic type to hold the context values
            var paramType = LinqRuntimeTypeBuilder.GetDynamicType(context.Select(kv => new DynamicProperty(kv.Key, kv.Value?.GetType() ?? typeof(object))).ToList());
            var paramExpression = System.Linq.Expressions.Expression.Parameter(paramType, "context");
            
            // Create the instance with the context values
            var instance = Activator.CreateInstance(paramType);
            foreach (var kv in context)
            {
                paramType.GetProperty(kv.Key)?.SetValue(instance, kv.Value);
            }
            
            // Create the param expression
            return System.Linq.Expressions.Expression.Constant(instance);
        }

        #endregion
    }

    #region Dynamic Type Building Helper

    internal static class LinqRuntimeTypeBuilder
    {
        private static readonly Dictionary<string, Type> BuiltTypes = new Dictionary<string, Type>();

        public static Type GetDynamicType(IEnumerable<DynamicProperty> properties)
        {
            var key = string.Join(":", properties.Select(p => $"{p.Name}:{p.Type.FullName}"));
            if (BuiltTypes.TryGetValue(key, out Type type))
                return type;

            var typeName = $"DynamicType_{Guid.NewGuid():N}";
            var typeBuilder = System.Reflection.Emit.AssemblyBuilder
                .DefineDynamicAssembly(new System.Reflection.AssemblyName("DynamicLinqTypes"), System.Reflection.Emit.AssemblyBuilderAccess.Run)
                .DefineDynamicModule("DynamicLinqTypesModule")
                .DefineType(typeName, System.Reflection.TypeAttributes.Public);

            foreach (var property in properties)
            {
                var fieldBuilder = typeBuilder.DefineField($"_{property.Name}", property.Type, System.Reflection.FieldAttributes.Private);
                var propertyBuilder = typeBuilder.DefineProperty(property.Name, System.Reflection.PropertyAttributes.None, property.Type, null);

                var getMethodBuilder = typeBuilder.DefineMethod(
                    $"get_{property.Name}",
                    System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.HideBySig,
                    property.Type,
                    Type.EmptyTypes);

                var getIl = getMethodBuilder.GetILGenerator();
                getIl.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                getIl.Emit(System.Reflection.Emit.OpCodes.Ldfld, fieldBuilder);
                getIl.Emit(System.Reflection.Emit.OpCodes.Ret);

                var setMethodBuilder = typeBuilder.DefineMethod(
                    $"set_{property.Name}",
                    System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.HideBySig,
                    null,
                    new[] { property.Type });

                var setIl = setMethodBuilder.GetILGenerator();
                setIl.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                setIl.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                setIl.Emit(System.Reflection.Emit.OpCodes.Stfld, fieldBuilder);
                setIl.Emit(System.Reflection.Emit.OpCodes.Ret);

                propertyBuilder.SetGetMethod(getMethodBuilder);
                propertyBuilder.SetSetMethod(setMethodBuilder);
            }

            type = typeBuilder.CreateType();
            BuiltTypes[key] = type;
            return type;
        }
    }

    internal class DynamicProperty
    {
        public string Name { get; }
        public Type Type { get; }

        public DynamicProperty(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }

    #endregion
}