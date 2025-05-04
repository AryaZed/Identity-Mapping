using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Grpc.Core;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.Extensions.Logging;

namespace IdentityMapping.Infrastructure.Grpc
{
    public class IdentityMappingGrpcService : IdentityMappingService.IdentityMappingServiceBase
    {
        private readonly IMappingRulesEngine _rulesEngine;
        private readonly IMappingRuleRepository _ruleRepository;
        private readonly ILogger<IdentityMappingGrpcService> _logger;

        public IdentityMappingGrpcService(
            IMappingRulesEngine rulesEngine,
            IMappingRuleRepository ruleRepository,
            ILogger<IdentityMappingGrpcService> logger)
        {
            _rulesEngine = rulesEngine ?? throw new ArgumentNullException(nameof(rulesEngine));
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<TransformClaimResponse> TransformClaim(TransformClaimRequest request, ServerCallContext context)
        {
            try
            {
                if (request.SourceClaim == null)
                {
                    return new TransformClaimResponse { Success = false, ErrorMessage = "Source claim is required" };
                }

                var sourceClaim = new Claim(request.SourceClaim.Type, request.SourceClaim.Value);
                var direction = (MappingDirection)request.Direction;
                
                // Convert context
                var contextDict = request.Context?.ToDictionary(c => c.Key, c => (object)c.Value) 
                    ?? new Dictionary<string, object>();
                
                var result = await _rulesEngine.TransformClaimAsync(request.ApplicationId, sourceClaim, direction, contextDict);
                
                if (result == null)
                {
                    return new TransformClaimResponse { Success = true };
                }
                
                return new TransformClaimResponse 
                { 
                    Success = true, 
                    TargetClaim = new Claim { Type = result.Type, Value = result.Value }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransformClaim gRPC call");
                return new TransformClaimResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<TransformClaimsResponse> TransformClaims(TransformClaimsRequest request, ServerCallContext context)
        {
            try
            {
                if (request.SourceClaims == null || !request.SourceClaims.Any())
                {
                    return new TransformClaimsResponse { Success = false, ErrorMessage = "Source claims are required" };
                }

                var sourceClaims = request.SourceClaims.Select(c => new Claim(c.Type, c.Value)).ToList();
                var direction = (MappingDirection)request.Direction;
                
                // Convert context
                var contextDict = request.Context?.ToDictionary(c => c.Key, c => (object)c.Value) 
                    ?? new Dictionary<string, object>();
                
                var results = await _rulesEngine.TransformClaimsAsync(request.ApplicationId, sourceClaims, direction, contextDict);
                
                var response = new TransformClaimsResponse { Success = true };
                
                if (results != null && results.Any())
                {
                    response.TargetClaims.AddRange(results.Select(c => new Claim { Type = c.Type, Value = c.Value }));
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransformClaims gRPC call");
                return new TransformClaimsResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<TransformRoleResponse> TransformRole(TransformRoleRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SourceRole))
                {
                    return new TransformRoleResponse { Success = false, ErrorMessage = "Source role is required" };
                }

                var direction = (MappingDirection)request.Direction;
                
                // Convert context
                var contextDict = request.Context?.ToDictionary(c => c.Key, c => (object)c.Value) 
                    ?? new Dictionary<string, object>();
                
                var result = await _rulesEngine.TransformRoleAsync(request.ApplicationId, request.SourceRole, direction, contextDict);
                
                return new TransformRoleResponse 
                { 
                    Success = true, 
                    TargetRole = result ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransformRole gRPC call");
                return new TransformRoleResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<TransformRolesResponse> TransformRoles(TransformRolesRequest request, ServerCallContext context)
        {
            try
            {
                if (request.SourceRoles == null || !request.SourceRoles.Any())
                {
                    return new TransformRolesResponse { Success = false, ErrorMessage = "Source roles are required" };
                }

                var direction = (MappingDirection)request.Direction;
                
                // Convert context
                var contextDict = request.Context?.ToDictionary(c => c.Key, c => (object)c.Value) 
                    ?? new Dictionary<string, object>();
                
                var results = await _rulesEngine.TransformRolesAsync(request.ApplicationId, request.SourceRoles, direction, contextDict);
                
                var response = new TransformRolesResponse { Success = true };
                
                if (results != null && results.Any())
                {
                    response.TargetRoles.AddRange(results);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransformRoles gRPC call");
                return new TransformRolesResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<GetMappingRulesResponse> GetMappingRules(GetMappingRulesRequest request, ServerCallContext context)
        {
            try
            {
                IEnumerable<MappingRule> rules;
                
                if (request.RuleType >= 0 && request.Direction >= 0)
                {
                    rules = await _ruleRepository.GetByTypeAndDirectionAsync(
                        request.ApplicationId, 
                        (MappingRuleType)request.RuleType, 
                        (MappingDirection)request.Direction);
                }
                else if (request.RuleType >= 0)
                {
                    rules = await _ruleRepository.GetByTypeAsync(
                        request.ApplicationId, 
                        (MappingRuleType)request.RuleType);
                }
                else
                {
                    rules = await _ruleRepository.GetByApplicationIdAsync(request.ApplicationId);
                }
                
                var response = new GetMappingRulesResponse { Success = true };
                
                foreach (var rule in rules)
                {
                    response.Rules.Add(ConvertToProto(rule));
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMappingRules gRPC call");
                return new GetMappingRulesResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<MappingRuleResponse> CreateMappingRule(CreateMappingRuleRequest request, ServerCallContext context)
        {
            try
            {
                if (request.Rule == null)
                {
                    return new MappingRuleResponse { Success = false, ErrorMessage = "Rule is required" };
                }
                
                var rule = ConvertFromProto(request.Rule);
                var result = await _ruleRepository.CreateAsync(rule);
                
                return new MappingRuleResponse
                {
                    Success = true,
                    Rule = ConvertToProto(result)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateMappingRule gRPC call");
                return new MappingRuleResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<MappingRuleResponse> UpdateMappingRule(UpdateMappingRuleRequest request, ServerCallContext context)
        {
            try
            {
                if (request.Rule == null || string.IsNullOrEmpty(request.Rule.Id))
                {
                    return new MappingRuleResponse { Success = false, ErrorMessage = "Rule with valid ID is required" };
                }
                
                var rule = ConvertFromProto(request.Rule);
                var result = await _ruleRepository.UpdateAsync(rule);
                
                return new MappingRuleResponse
                {
                    Success = true,
                    Rule = ConvertToProto(result)
                };
            }
            catch (KeyNotFoundException)
            {
                return new MappingRuleResponse { Success = false, ErrorMessage = $"Rule with ID {request.Rule.Id} not found" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMappingRule gRPC call");
                return new MappingRuleResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public override async Task<DeleteMappingRuleResponse> DeleteMappingRule(DeleteMappingRuleRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Id))
                {
                    return new DeleteMappingRuleResponse { Success = false, ErrorMessage = "Rule ID is required" };
                }
                
                var result = await _ruleRepository.DeleteAsync(request.Id);
                
                return new DeleteMappingRuleResponse
                {
                    Success = result,
                    ErrorMessage = result ? null : $"Rule with ID {request.Id} not found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMappingRule gRPC call");
                return new DeleteMappingRuleResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        private MappingRuleProto ConvertToProto(MappingRule rule)
        {
            return new MappingRuleProto
            {
                Id = rule.Id,
                ApplicationId = rule.ApplicationId,
                Name = rule.Name,
                Description = rule.Description ?? string.Empty,
                Condition = rule.Condition ?? string.Empty,
                RuleType = (int)rule.RuleType,
                SourceIdentifier = rule.SourceIdentifier,
                TargetIdentifier = rule.TargetIdentifier ?? string.Empty,
                TransformExpression = rule.TransformExpression ?? string.Empty,
                Direction = (int)rule.Direction,
                Priority = rule.Priority,
                IsEnabled = rule.IsEnabled,
                CreatedAt = rule.CreatedAt.ToString("o"),
                UpdatedAt = rule.UpdatedAt.ToString("o")
            };
        }

        private MappingRule ConvertFromProto(MappingRuleProto proto)
        {
            return new MappingRule
            {
                Id = proto.Id,
                ApplicationId = proto.ApplicationId,
                Name = proto.Name,
                Description = proto.Description,
                Condition = proto.Condition,
                RuleType = (MappingRuleType)proto.RuleType,
                SourceIdentifier = proto.SourceIdentifier,
                TargetIdentifier = proto.TargetIdentifier,
                TransformExpression = proto.TransformExpression,
                Direction = (MappingDirection)proto.Direction,
                Priority = proto.Priority,
                IsEnabled = proto.IsEnabled,
                CreatedAt = string.IsNullOrEmpty(proto.CreatedAt) ? DateTime.UtcNow : DateTime.Parse(proto.CreatedAt),
                UpdatedAt = string.IsNullOrEmpty(proto.UpdatedAt) ? DateTime.UtcNow : DateTime.Parse(proto.UpdatedAt)
            };
        }
    }
} 