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
    public class ApplicationsControllerTests
    {
        private readonly Mock<IApplicationRepository> _mockApplicationRepository;
        private readonly Mock<ILogger<ApplicationsController>> _mockLogger;
        private readonly ApplicationsController _controller;

        public ApplicationsControllerTests()
        {
            _mockApplicationRepository = new Mock<IApplicationRepository>();
            _mockLogger = new Mock<ILogger<ApplicationsController>>();
            _controller = new ApplicationsController(
                _mockApplicationRepository.Object,
                _mockLogger.Object);

            // Setup default admin user
            SetupAdminUser();
        }

        [Fact]
        public async Task GetAllApplications_ReturnsOkWithApplications()
        {
            // Arrange
            var applications = new List<Application>
            {
                new Application { Id = "app1", Name = "Application 1" },
                new Application { Id = "app2", Name = "Application 2" }
            };

            _mockApplicationRepository
                .Setup(r => r.GetAllAsync(0, 20))
                .ReturnsAsync(applications);

            // Act
            var result = await _controller.GetAllApplications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApplications = okResult.Value.Should().BeAssignableTo<IEnumerable<Application>>().Subject;
            returnedApplications.Should().HaveCount(2);
            returnedApplications.Should().Contain(a => a.Id == "app1");
            returnedApplications.Should().Contain(a => a.Id == "app2");
        }

        [Fact]
        public async Task GetApplicationById_WhenApplicationExists_ReturnsOkWithApplication()
        {
            // Arrange
            var application = new Application { Id = "app1", Name = "Application 1" };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app1"))
                .ReturnsAsync(application);

            // Act
            var result = await _controller.GetApplicationById("app1");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApplication = okResult.Value.Should().BeAssignableTo<Application>().Subject;
            returnedApplication.Id.Should().Be("app1");
            returnedApplication.Name.Should().Be("Application 1");
        }

        [Fact]
        public async Task GetApplicationById_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((Application)null);

            // Act
            var result = await _controller.GetApplicationById("nonexistent");

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateApplication_WithValidData_ReturnsCreatedApplication()
        {
            // Arrange
            var application = new Application { Name = "New Application" };
            var createdApplication = new Application { Id = "new-app", Name = "New Application" };

            _mockApplicationRepository
                .Setup(r => r.CreateAsync(application))
                .ReturnsAsync(createdApplication);

            // Act
            var result = await _controller.CreateApplication(application);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ApplicationsController.GetApplicationById));
            createdResult.RouteValues["id"].Should().Be(createdApplication.Id);
            
            var returnedApplication = createdResult.Value.Should().BeAssignableTo<Application>().Subject;
            returnedApplication.Id.Should().Be("new-app");
            returnedApplication.Name.Should().Be("New Application");
        }

        [Fact]
        public async Task UpdateApplication_WithValidData_ReturnsOkWithUpdatedApplication()
        {
            // Arrange
            var application = new Application { Id = "app1", Name = "Updated Application" };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app1"))
                .ReturnsAsync(new Application { Id = "app1", Name = "Original Application" });

            _mockApplicationRepository
                .Setup(r => r.UpdateAsync(application))
                .ReturnsAsync(application);

            // Act
            var result = await _controller.UpdateApplication("app1", application);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApplication = okResult.Value.Should().BeAssignableTo<Application>().Subject;
            returnedApplication.Id.Should().Be("app1");
            returnedApplication.Name.Should().Be("Updated Application");
        }

        [Fact]
        public async Task UpdateApplication_WithMismatchedIds_ReturnsBadRequest()
        {
            // Arrange
            var application = new Application { Id = "app1", Name = "Updated Application" };

            // Act
            var result = await _controller.UpdateApplication("app2", application);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateApplication_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var application = new Application { Id = "nonexistent", Name = "Updated Application" };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((Application)null);

            // Act
            var result = await _controller.UpdateApplication("nonexistent", application);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteApplication_WhenApplicationExists_ReturnsNoContent()
        {
            // Arrange
            _mockApplicationRepository
                .Setup(r => r.DeleteAsync("app1"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteApplication("app1");

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteApplication_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockApplicationRepository
                .Setup(r => r.DeleteAsync("nonexistent"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteApplication("nonexistent");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task RegenerateApiKey_WhenApplicationExists_ReturnsOkWithApiKey()
        {
            // Arrange
            const string newApiKey = "new-api-key-12345";

            _mockApplicationRepository
                .Setup(r => r.RegenerateApiKeyAsync("app1"))
                .ReturnsAsync(newApiKey);

            // Act
            var result = await _controller.RegenerateApiKey("app1");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic value = okResult.Value;
            ((string)value.apiKey).Should().Be(newApiKey);
        }

        [Fact]
        public async Task RegenerateApiKey_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockApplicationRepository
                .Setup(r => r.RegenerateApiKeyAsync("nonexistent"))
                .ThrowsAsync(new KeyNotFoundException("Application not found"));

            // Act
            var result = await _controller.RegenerateApiKey("nonexistent");

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
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
    }
} 