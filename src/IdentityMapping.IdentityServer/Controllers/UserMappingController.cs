using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityMapping.IdentityServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserMappingController : ControllerBase
    {
        private readonly IUserMappingService _userMappingService;
        private readonly IApplicationRepository _applicationRepository;
        private readonly ILogger<UserMappingController> _logger;

        public UserMappingController(
            IUserMappingService userMappingService,
            IApplicationRepository applicationRepository,
            ILogger<UserMappingController> logger)
        {
            _userMappingService = userMappingService ?? throw new ArgumentNullException(nameof(userMappingService));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("identity/{applicationId}/{legacyUserId}")]
        [Authorize(Roles = "Admin,ApiClient")]
        public async Task<ActionResult<string>> GetCentralizedIdentityId(string applicationId, string legacyUserId)
        {
            try
            {
                // Validate API key for application if called by API client
                if (User.IsInRole("ApiClient"))
                {
                    var apiKey = Request.Headers["X-Api-Key"].ToString();
                    if (string.IsNullOrEmpty(apiKey) || !await _applicationRepository.ValidateApiKeyAsync(applicationId, apiKey))
                    {
                        return Unauthorized("Invalid API key");
                    }
                }

                var centralizedIdentityId = await _userMappingService.GetCentralizedIdentityIdAsync(applicationId, legacyUserId);
                if (centralizedIdentityId == null)
                {
                    return NotFound();
                }

                return Ok(new { centralizedIdentityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving centralized identity ID for application {applicationId} and legacy user ID {legacyUserId}");
                return StatusCode(500, "An error occurred while retrieving the centralized identity ID");
            }
        }

        [HttpGet("legacy/{applicationId}/{centralizedIdentityId}")]
        [Authorize(Roles = "Admin,ApiClient")]
        public async Task<ActionResult<string>> GetLegacyUserId(string applicationId, string centralizedIdentityId)
        {
            try
            {
                // Validate API key for application if called by API client
                if (User.IsInRole("ApiClient"))
                {
                    var apiKey = Request.Headers["X-Api-Key"].ToString();
                    if (string.IsNullOrEmpty(apiKey) || !await _applicationRepository.ValidateApiKeyAsync(applicationId, apiKey))
                    {
                        return Unauthorized("Invalid API key");
                    }
                }

                var legacyUserId = await _userMappingService.GetLegacyUserIdAsync(applicationId, centralizedIdentityId);
                if (legacyUserId == null)
                {
                    return NotFound();
                }

                return Ok(new { legacyUserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving legacy user ID for application {applicationId} and centralized identity ID {centralizedIdentityId}");
                return StatusCode(500, "An error occurred while retrieving the legacy user ID");
            }
        }

        [HttpPost("mapping")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserIdMapping>> CreateMapping([FromBody] CreateMappingDto createMappingDto)
        {
            try
            {
                var mapping = await _userMappingService.CreateMappingAsync(
                    createMappingDto.ApplicationId,
                    createMappingDto.LegacyUserId,
                    createMappingDto.CentralizedIdentityId);

                return CreatedAtAction(nameof(GetMapping), new { id = mapping.Id }, mapping);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mapping");
                return StatusCode(500, "An error occurred while creating the mapping");
            }
        }

        [HttpGet("mapping/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserIdMapping>> GetMapping(string id)
        {
            try
            {
                var mapping = await _userMappingService.GetMappingByIdAsync(id);
                if (mapping == null)
                {
                    return NotFound();
                }

                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving mapping with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the mapping");
            }
        }

        [HttpGet("mappings/{applicationId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserIdMapping>>> GetMappingsByApplication(string applicationId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            try
            {
                var mappings = await _userMappingService.GetMappingsByApplicationAsync(applicationId, skip, take);
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving mappings for application {applicationId}");
                return StatusCode(500, "An error occurred while retrieving the mappings");
            }
        }

        [HttpPost("mapping/{id}/validate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ValidateMapping(string id)
        {
            try
            {
                var result = await _userMappingService.ValidateMappingAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating mapping with ID {id}");
                return StatusCode(500, "An error occurred while validating the mapping");
            }
        }

        [HttpDelete("mapping/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteMapping(string id)
        {
            try
            {
                var result = await _userMappingService.DeleteMappingAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting mapping with ID {id}");
                return StatusCode(500, "An error occurred while deleting the mapping");
            }
        }

        [HttpPost("migrate/{applicationId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MigrationReport>> MigrateApplication(string applicationId, [FromBody] MigrationOptions options)
        {
            try
            {
                var report = await _userMappingService.MigrateApplicationToIdentityServerAsync(applicationId, options);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating application {applicationId}");
                return StatusCode(500, "An error occurred while migrating the application");
            }
        }
    }

    public class CreateMappingDto
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string LegacyUserId { get; set; } = string.Empty;
        public string CentralizedIdentityId { get; set; } = string.Empty;
    }
} 