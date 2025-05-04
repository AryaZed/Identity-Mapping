using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityMapping.IdentityServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RoleClaimMappingController : ControllerBase
    {
        private readonly IRoleMappingService _roleMappingService;
        private readonly IClaimMappingService _claimMappingService;
        private readonly IApplicationRepository _applicationRepository;
        private readonly ILogger<RoleClaimMappingController> _logger;

        public RoleClaimMappingController(
            IRoleMappingService roleMappingService,
            IClaimMappingService claimMappingService,
            IApplicationRepository applicationRepository,
            ILogger<RoleClaimMappingController> logger)
        {
            _roleMappingService = roleMappingService ?? throw new ArgumentNullException(nameof(roleMappingService));
            _claimMappingService = claimMappingService ?? throw new ArgumentNullException(nameof(claimMappingService));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Role Mappings

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleMapping>>> GetAllRoleMappings([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            try
            {
                var roleMappings = await _roleMappingService.GetAllRoleMappingsAsync(skip, take);
                return Ok(roleMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role mappings");
                return StatusCode(500, "An error occurred while retrieving role mappings");
            }
        }

        [HttpGet("roles/{id}")]
        public async Task<ActionResult<RoleMapping>> GetRoleMappingById(string id)
        {
            try
            {
                var roleMapping = await _roleMappingService.GetRoleMappingByIdAsync(id);
                if (roleMapping == null)
                {
                    return NotFound();
                }

                return Ok(roleMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role mapping with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the role mapping");
            }
        }

        [HttpGet("roles/application/{applicationId}")]
        public async Task<ActionResult<IEnumerable<RoleMapping>>> GetRoleMappingsByApplication(string applicationId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return NotFound($"Application with ID {applicationId} not found");
                }

                var roleMappings = await _roleMappingService.GetRoleMappingsByApplicationAsync(applicationId);
                return Ok(roleMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving role mappings for application {applicationId}");
                return StatusCode(500, "An error occurred while retrieving role mappings");
            }
        }

        [HttpPost("roles")]
        public async Task<ActionResult<RoleMapping>> CreateRoleMapping([FromBody] CreateRoleMappingDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sourceApplication = await _applicationRepository.GetByIdAsync(createDto.SourceApplicationId);
                if (sourceApplication == null)
                {
                    return NotFound($"Source application with ID {createDto.SourceApplicationId} not found");
                }

                var targetApplication = await _applicationRepository.GetByIdAsync(createDto.TargetApplicationId);
                if (targetApplication == null)
                {
                    return NotFound($"Target application with ID {createDto.TargetApplicationId} not found");
                }

                var roleMapping = await _roleMappingService.CreateRoleMappingAsync(
                    createDto.SourceApplicationId,
                    createDto.TargetApplicationId,
                    createDto.SourceRole,
                    createDto.TargetRole);

                return CreatedAtAction(nameof(GetRoleMappingById), new { id = roleMapping.Id }, roleMapping);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role mapping");
                return StatusCode(500, "An error occurred while creating the role mapping");
            }
        }

        [HttpPut("roles/{id}")]
        public async Task<ActionResult<RoleMapping>> UpdateRoleMapping(string id, [FromBody] UpdateRoleMappingDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingMapping = await _roleMappingService.GetRoleMappingByIdAsync(id);
                if (existingMapping == null)
                {
                    return NotFound($"Role mapping with ID {id} not found");
                }

                var updatedMapping = await _roleMappingService.UpdateRoleMappingAsync(
                    id,
                    updateDto.SourceRole,
                    updateDto.TargetRole);

                return Ok(updatedMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role mapping with ID {id}");
                return StatusCode(500, "An error occurred while updating the role mapping");
            }
        }

        [HttpDelete("roles/{id}")]
        public async Task<ActionResult> DeleteRoleMapping(string id)
        {
            try
            {
                var result = await _roleMappingService.DeleteRoleMappingAsync(id);
                if (!result)
                {
                    return NotFound($"Role mapping with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role mapping with ID {id}");
                return StatusCode(500, "An error occurred while deleting the role mapping");
            }
        }

        [HttpGet("roles/translate")]
        public async Task<ActionResult<string>> TranslateRole(
            [FromQuery] string sourceApplicationId,
            [FromQuery] string targetApplicationId,
            [FromQuery] string sourceRole)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceApplicationId) || 
                    string.IsNullOrEmpty(targetApplicationId) || 
                    string.IsNullOrEmpty(sourceRole))
                {
                    return BadRequest("Source application ID, target application ID, and source role are required");
                }

                var targetRole = await _roleMappingService.TranslateRoleAsync(
                    sourceApplicationId,
                    targetApplicationId,
                    sourceRole);

                if (string.IsNullOrEmpty(targetRole))
                {
                    return NotFound($"No mapping found for role '{sourceRole}' from application {sourceApplicationId} to {targetApplicationId}");
                }

                return Ok(new { targetRole });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error translating role from application {sourceApplicationId} to {targetApplicationId}");
                return StatusCode(500, "An error occurred while translating the role");
            }
        }

        #endregion

        #region Claim Mappings

        [HttpGet("claims")]
        public async Task<ActionResult<IEnumerable<ClaimMapping>>> GetAllClaimMappings([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            try
            {
                var claimMappings = await _claimMappingService.GetAllClaimMappingsAsync(skip, take);
                return Ok(claimMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving claim mappings");
                return StatusCode(500, "An error occurred while retrieving claim mappings");
            }
        }

        [HttpGet("claims/{id}")]
        public async Task<ActionResult<ClaimMapping>> GetClaimMappingById(string id)
        {
            try
            {
                var claimMapping = await _claimMappingService.GetClaimMappingByIdAsync(id);
                if (claimMapping == null)
                {
                    return NotFound();
                }

                return Ok(claimMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim mapping with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the claim mapping");
            }
        }

        [HttpGet("claims/application/{applicationId}")]
        public async Task<ActionResult<IEnumerable<ClaimMapping>>> GetClaimMappingsByApplication(string applicationId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return NotFound($"Application with ID {applicationId} not found");
                }

                var claimMappings = await _claimMappingService.GetClaimMappingsByApplicationAsync(applicationId);
                return Ok(claimMappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving claim mappings for application {applicationId}");
                return StatusCode(500, "An error occurred while retrieving claim mappings");
            }
        }

        [HttpPost("claims")]
        public async Task<ActionResult<ClaimMapping>> CreateClaimMapping([FromBody] CreateClaimMappingDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sourceApplication = await _applicationRepository.GetByIdAsync(createDto.SourceApplicationId);
                if (sourceApplication == null)
                {
                    return NotFound($"Source application with ID {createDto.SourceApplicationId} not found");
                }

                var targetApplication = await _applicationRepository.GetByIdAsync(createDto.TargetApplicationId);
                if (targetApplication == null)
                {
                    return NotFound($"Target application with ID {createDto.TargetApplicationId} not found");
                }

                var claimMapping = await _claimMappingService.CreateClaimMappingAsync(
                    createDto.SourceApplicationId,
                    createDto.TargetApplicationId,
                    createDto.SourceClaimType,
                    createDto.TargetClaimType,
                    createDto.TransformationExpression);

                return CreatedAtAction(nameof(GetClaimMappingById), new { id = claimMapping.Id }, claimMapping);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim mapping");
                return StatusCode(500, "An error occurred while creating the claim mapping");
            }
        }

        [HttpPut("claims/{id}")]
        public async Task<ActionResult<ClaimMapping>> UpdateClaimMapping(string id, [FromBody] UpdateClaimMappingDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingMapping = await _claimMappingService.GetClaimMappingByIdAsync(id);
                if (existingMapping == null)
                {
                    return NotFound($"Claim mapping with ID {id} not found");
                }

                var updatedMapping = await _claimMappingService.UpdateClaimMappingAsync(
                    id,
                    updateDto.SourceClaimType,
                    updateDto.TargetClaimType,
                    updateDto.TransformationExpression);

                return Ok(updatedMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating claim mapping with ID {id}");
                return StatusCode(500, "An error occurred while updating the claim mapping");
            }
        }

        [HttpDelete("claims/{id}")]
        public async Task<ActionResult> DeleteClaimMapping(string id)
        {
            try
            {
                var result = await _claimMappingService.DeleteClaimMappingAsync(id);
                if (!result)
                {
                    return NotFound($"Claim mapping with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting claim mapping with ID {id}");
                return StatusCode(500, "An error occurred while deleting the claim mapping");
            }
        }

        [HttpPost("claims/transform")]
        public async Task<ActionResult<IEnumerable<ClaimTransformationResult>>> TransformClaims(
            [FromBody] ClaimTransformationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.Claims == null || !request.Claims.Any())
                {
                    return BadRequest("No claims provided for transformation");
                }

                var transformedClaims = await _claimMappingService.TransformClaimsAsync(
                    request.SourceApplicationId,
                    request.TargetApplicationId,
                    request.Claims);

                return Ok(transformedClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error transforming claims from application {request.SourceApplicationId} to {request.TargetApplicationId}");
                return StatusCode(500, "An error occurred while transforming claims");
            }
        }

        #endregion
    }

    #region DTOs

    public class CreateRoleMappingDto
    {
        public string SourceApplicationId { get; set; } = string.Empty;
        public string TargetApplicationId { get; set; } = string.Empty;
        public string SourceRole { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
    }

    public class UpdateRoleMappingDto
    {
        public string SourceRole { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
    }

    public class CreateClaimMappingDto
    {
        public string SourceApplicationId { get; set; } = string.Empty;
        public string TargetApplicationId { get; set; } = string.Empty;
        public string SourceClaimType { get; set; } = string.Empty;
        public string TargetClaimType { get; set; } = string.Empty;
        public string? TransformationExpression { get; set; }
    }

    public class UpdateClaimMappingDto
    {
        public string SourceClaimType { get; set; } = string.Empty;
        public string TargetClaimType { get; set; } = string.Empty;
        public string? TransformationExpression { get; set; }
    }

    public class ClaimTransformationRequest
    {
        public string SourceApplicationId { get; set; } = string.Empty;
        public string TargetApplicationId { get; set; } = string.Empty;
        public IEnumerable<UserClaim> Claims { get; set; } = new List<UserClaim>();
    }

    public class UserClaim
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ClaimTransformationResult
    {
        public string OriginalType { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;
        public string TransformedType { get; set; } = string.Empty;
        public string TransformedValue { get; set; } = string.Empty;
    }

    #endregion
} 