using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityMapping.IdentityServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserIdentitiesController : ControllerBase
    {
        private readonly IUserIdentityRepository _userIdentityRepository;
        private readonly ILogger<UserIdentitiesController> _logger;

        public UserIdentitiesController(
            IUserIdentityRepository userIdentityRepository,
            ILogger<UserIdentitiesController> logger)
        {
            _userIdentityRepository = userIdentityRepository ?? throw new ArgumentNullException(nameof(userIdentityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserIdentity>>> GetAllUserIdentities([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            try
            {
                var userIdentities = await _userIdentityRepository.GetAllAsync(skip, take);
                return Ok(userIdentities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user identities");
                return StatusCode(500, "An error occurred while retrieving user identities");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserIdentity>> GetUserIdentityById(string id)
        {
            try
            {
                var userIdentity = await _userIdentityRepository.GetByIdAsync(id);
                if (userIdentity == null)
                {
                    return NotFound();
                }

                return Ok(userIdentity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user identity with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the user identity");
            }
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserIdentity>> GetUserIdentityByEmail(string email)
        {
            try
            {
                var userIdentity = await _userIdentityRepository.GetByEmailAsync(email);
                if (userIdentity == null)
                {
                    return NotFound();
                }

                return Ok(userIdentity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user identity with email {email}");
                return StatusCode(500, "An error occurred while retrieving the user identity");
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserIdentity>> CreateUserIdentity([FromBody] UserIdentity userIdentity)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdUserIdentity = await _userIdentityRepository.CreateAsync(userIdentity);
                return CreatedAtAction(nameof(GetUserIdentityById), new { id = createdUserIdentity.Id }, createdUserIdentity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user identity");
                return StatusCode(500, "An error occurred while creating the user identity");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserIdentity>> UpdateUserIdentity(string id, [FromBody] UserIdentity userIdentity)
        {
            try
            {
                if (id != userIdentity.Id)
                {
                    return BadRequest("The ID in the URL must match the ID in the user identity object");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingUserIdentity = await _userIdentityRepository.GetByIdAsync(id);
                if (existingUserIdentity == null)
                {
                    return NotFound();
                }

                var updatedUserIdentity = await _userIdentityRepository.UpdateAsync(userIdentity);
                return Ok(updatedUserIdentity);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user identity with ID {id}");
                return StatusCode(500, "An error occurred while updating the user identity");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUserIdentity(string id)
        {
            try
            {
                var result = await _userIdentityRepository.DeleteAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user identity with ID {id}");
                return StatusCode(500, "An error occurred while deleting the user identity");
            }
        }

        [HttpGet("by-legacy-id")]
        public async Task<ActionResult<UserIdentity>> GetUserIdentityByLegacyId([FromQuery] string applicationId, [FromQuery] string legacyUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(applicationId) || string.IsNullOrEmpty(legacyUserId))
                {
                    return BadRequest("Application ID and legacy user ID are required");
                }

                var userIdentity = await _userIdentityRepository.FindByLegacyUserIdAsync(applicationId, legacyUserId);
                if (userIdentity == null)
                {
                    return NotFound();
                }

                return Ok(userIdentity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user identity with application ID {applicationId} and legacy user ID {legacyUserId}");
                return StatusCode(500, "An error occurred while retrieving the user identity");
            }
        }

        [HttpPost("{id}/legacy-user-id")]
        public async Task<ActionResult> AddLegacyUserId(string id, [FromBody] LegacyUserIdDto legacyUserIdDto)
        {
            try
            {
                if (string.IsNullOrEmpty(legacyUserIdDto.ApplicationId) || string.IsNullOrEmpty(legacyUserIdDto.LegacyUserId))
                {
                    return BadRequest("Application ID and legacy user ID are required");
                }

                var userIdentity = await _userIdentityRepository.GetByIdAsync(id);
                if (userIdentity == null)
                {
                    return NotFound();
                }

                var result = await _userIdentityRepository.AddLegacyUserIdAsync(id, legacyUserIdDto.ApplicationId, legacyUserIdDto.LegacyUserId);
                if (result)
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(500, "Failed to add legacy user ID");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding legacy user ID to user identity with ID {id}");
                return StatusCode(500, "An error occurred while adding the legacy user ID");
            }
        }
    }

    public class LegacyUserIdDto
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string LegacyUserId { get; set; } = string.Empty;
    }
} 