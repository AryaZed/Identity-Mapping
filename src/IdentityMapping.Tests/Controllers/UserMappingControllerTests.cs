using FluentAssertions;
using IdentityMapping.Core.Interfaces;
using IdentityMapping.Core.Models;
using IdentityMapping.IdentityServer.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace IdentityMapping.Tests.Controllers
{
    public class UserMappingControllerTests
    {
        private readonly Mock<IUserMappingService> _mockUserMappingService;
        private readonly Mock<IApplicationRepository> _mockApplicationRepository;
        private readonly Mock<ILogger<UserMappingController>> _mockLogger;
        private readonly UserMappingController _controller;

        public UserMappingControllerTests()
        {
            _mockUserMappingService = new Mock<IUserMappingService>();
            _mockApplicationRepository = new Mock<IApplicationRepository>();
            _mockLogger = new Mock<ILogger<UserMappingController>>();
            _controller = new UserMappingController(
                _mockUserMappingService.Object,
                _mockApplicationRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetCentralizedIdentityId_WhenMappingExists_ReturnsOkWithId()
        {
            // Arrange
            const string applicationId = "app123";
            const string legacyUserId = "legacy123";
            const string centralizedId = "centralized123";

            _mockUserMappingService
                .Setup(s => s.GetCentralizedIdentityIdAsync(applicationId, legacyUserId))
                .ReturnsAsync(centralizedId);

            SetupAdminUser();

            // Act
            var result = await _controller.GetCentralizedIdentityId(applicationId, legacyUserId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic value = okResult.Value;
            ((string)value.centralizedIdentityId).Should().Be(centralizedId);
        }

        [Fact]
        public async Task GetCentralizedIdentityId_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            const string applicationId = "app123";
            const string legacyUserId = "legacy123";

            _mockUserMappingService
                .Setup(s => s.GetCentralizedIdentityIdAsync(applicationId, legacyUserId))
                .ReturnsAsync((string)null);

            SetupAdminUser();

            // Act
            var result = await _controller.GetCentralizedIdentityId(applicationId, legacyUserId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetCentralizedIdentityId_WithApiClientAndValidApiKey_ReturnsOkWithId()
        {
            // Arrange
            const string applicationId = "app123";
            const string legacyUserId = "legacy123";
            const string centralizedId = "centralized123";
            const string apiKey = "valid-api-key";

            _mockUserMappingService
                .Setup(s => s.GetCentralizedIdentityIdAsync(applicationId, legacyUserId))
                .ReturnsAsync(centralizedId);

            _mockApplicationRepository
                .Setup(r => r.ValidateApiKeyAsync(applicationId, apiKey))
                .ReturnsAsync(true);

            SetupApiClientUser();
            SetApiKeyHeader(apiKey);

            // Act
            var result = await _controller.GetCentralizedIdentityId(applicationId, legacyUserId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic value = okResult.Value;
            ((string)value.centralizedIdentityId).Should().Be(centralizedId);
        }

        [Fact]
        public async Task GetCentralizedIdentityId_WithApiClientAndInvalidApiKey_ReturnsUnauthorized()
        {
            // Arrange
            const string applicationId = "app123";
            const string legacyUserId = "legacy123";
            const string apiKey = "invalid-api-key";

            _mockApplicationRepository
                .Setup(r => r.ValidateApiKeyAsync(applicationId, apiKey))
                .ReturnsAsync(false);

            SetupApiClientUser();
            SetApiKeyHeader(apiKey);

            // Act
            var result = await _controller.GetCentralizedIdentityId(applicationId, legacyUserId);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CreateMapping_WithValidData_ReturnsCreatedMapping()
        {
            // Arrange
            var dto = new CreateMappingDto
            {
                ApplicationId = "app123",
                LegacyUserId = "legacy123",
                CentralizedIdentityId = "centralized123"
            };

            var mapping = new UserIdMapping
            {
                Id = "mapping123",
                ApplicationId = dto.ApplicationId,
                LegacyUserId = dto.LegacyUserId,
                CentralizedIdentityId = dto.CentralizedIdentityId
            };

            _mockUserMappingService
                .Setup(s => s.CreateMappingAsync(dto.ApplicationId, dto.LegacyUserId, dto.CentralizedIdentityId))
                .ReturnsAsync(mapping);

            SetupAdminUser();

            // Act
            var result = await _controller.CreateMapping(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(UserMappingController.GetMapping));
            createdResult.RouteValues["id"].Should().Be(mapping.Id);
            createdResult.Value.Should().Be(mapping);
        }

        [Fact]
        public async Task CreateMapping_WithInvalidApplication_ReturnsNotFound()
        {
            // Arrange
            var dto = new CreateMappingDto
            {
                ApplicationId = "invalid-app",
                LegacyUserId = "legacy123",
                CentralizedIdentityId = "centralized123"
            };

            _mockUserMappingService
                .Setup(s => s.CreateMappingAsync(dto.ApplicationId, dto.LegacyUserId, dto.CentralizedIdentityId))
                .ThrowsAsync(new KeyNotFoundException("Application not found"));

            SetupAdminUser();

            // Act
            var result = await _controller.CreateMapping(dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteMapping_WhenMappingExists_ReturnsNoContent()
        {
            // Arrange
            const string mappingId = "mapping123";

            _mockUserMappingService
                .Setup(s => s.DeleteMappingAsync(mappingId))
                .ReturnsAsync(true);

            SetupAdminUser();

            // Act
            var result = await _controller.DeleteMapping(mappingId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            const string mappingId = "invalid-mapping";

            _mockUserMappingService
                .Setup(s => s.DeleteMappingAsync(mappingId))
                .ReturnsAsync(false);

            SetupAdminUser();

            // Act
            var result = await _controller.DeleteMapping(mappingId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        private void SetupAdminUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void SetupApiClientUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "ApiClient")
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void SetApiKeyHeader(string apiKey)
        {
            _controller.ControllerContext.HttpContext.Request.Headers["X-Api-Key"] = apiKey;
        }
    }
} 