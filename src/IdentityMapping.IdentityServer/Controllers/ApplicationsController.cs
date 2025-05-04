using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityMapping.IdentityServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(
            IApplicationRepository applicationRepository,
            ILogger<ApplicationsController> logger)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Application>>> GetAllApplications([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            try
            {
                var applications = await _applicationRepository.GetAllAsync(skip, take);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, "An error occurred while retrieving applications");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Application>> GetApplicationById(string id)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(id);
                if (application == null)
                {
                    return NotFound();
                }

                return Ok(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving application with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the application");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Application>> CreateApplication([FromBody] Application application)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdApplication = await _applicationRepository.CreateAsync(application);
                return CreatedAtAction(nameof(GetApplicationById), new { id = createdApplication.Id }, createdApplication);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                return StatusCode(500, "An error occurred while creating the application");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Application>> UpdateApplication(string id, [FromBody] Application application)
        {
            try
            {
                if (id != application.Id)
                {
                    return BadRequest("The ID in the URL must match the ID in the application object");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingApplication = await _applicationRepository.GetByIdAsync(id);
                if (existingApplication == null)
                {
                    return NotFound();
                }

                var updatedApplication = await _applicationRepository.UpdateAsync(application);
                return Ok(updatedApplication);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating application with ID {id}");
                return StatusCode(500, "An error occurred while updating the application");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteApplication(string id)
        {
            try
            {
                var result = await _applicationRepository.DeleteAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting application with ID {id}");
                return StatusCode(500, "An error occurred while deleting the application");
            }
        }

        [HttpPost("{id}/regenerate-api-key")]
        public async Task<ActionResult<string>> RegenerateApiKey(string id)
        {
            try
            {
                var apiKey = await _applicationRepository.RegenerateApiKeyAsync(id);
                return Ok(new { apiKey });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error regenerating API key for application with ID {id}");
                return StatusCode(500, "An error occurred while regenerating the API key");
            }
        }
    }
} 