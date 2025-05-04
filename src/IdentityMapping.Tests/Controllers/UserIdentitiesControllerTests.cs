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
    public class UserIdentitiesControllerTests
    {
        private readonly Mock<IUserIdentityRepository> _mockUserIdentityRepository;
        private readonly Mock<ILogger<UserIdentitiesController>> _mockLogger;
        private readonly UserIdentitiesController _controller;

        public UserIdentitiesControllerTests()
        {
            _mockUserIdentityRepository = new Mock<IUserIdentityRepository>();
            _mockLogger = new Mock<ILogger<UserIdentitiesController>>();
            _controller = new UserIdentitiesController(
                _mockUserIdentityRepository.Object,
                _mockLogger.Object);

            // Setup default admin user
            SetupAdminUser();
        }

        [Fact]
        public async Task GetAllUserIdentities_ReturnsOkWithUserIdentities()
        {
            // Arrange
            var userIdentities = new List<UserIdentity>
            {
                new UserIdentity { Id = "user1", Email = "user1@example.com" },
                new UserIdentity { Id = "user2", Email = "user2@example.com" }
            };

            _mockUserIdentityRepository
                .Setup(r => r.GetAllAsync(0, 20))
                .ReturnsAsync(userIdentities);

            // Act
            var result = await _controller.GetAllUserIdentities();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUserIdentities = okResult.Value.Should().BeAssignableTo<IEnumerable<UserIdentity>>().Subject;
            returnedUserIdentities.Should().HaveCount(2);
            returnedUserIdentities.Should().Contain(u => u.Id == "user1");
            returnedUserIdentities.Should().Contain(u => u.Id == "user2");
        }

        [Fact]
        public async Task GetUserIdentityById_WhenUserExists_ReturnsOkWithUser()
        {
            // Arrange
            var userIdentity = new UserIdentity { Id = "user1", Email = "user1@example.com" };

            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync("user1"))
                .ReturnsAsync(userIdentity);

            // Act
            var result = await _controller.GetUserIdentityById("user1");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeAssignableTo<UserIdentity>().Subject;
            returnedUser.Id.Should().Be("user1");
            returnedUser.Email.Should().Be("user1@example.com");
        }

        [Fact]
        public async Task GetUserIdentityById_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((UserIdentity)null);

            // Act
            var result = await _controller.GetUserIdentityById("nonexistent");

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetUserIdentityByEmail_WhenUserExists_ReturnsOkWithUser()
        {
            // Arrange
            var email = "user1@example.com";
            var userIdentity = new UserIdentity { Id = "user1", Email = email };

            _mockUserIdentityRepository
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(userIdentity);

            // Act
            var result = await _controller.GetUserIdentityByEmail(email);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeAssignableTo<UserIdentity>().Subject;
            returnedUser.Id.Should().Be("user1");
            returnedUser.Email.Should().Be(email);
        }

        [Fact]
        public async Task GetUserIdentityByEmail_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _mockUserIdentityRepository
                .Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync((UserIdentity)null);

            // Act
            var result = await _controller.GetUserIdentityByEmail(email);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateUserIdentity_WithValidData_ReturnsCreatedUser()
        {
            // Arrange
            var userIdentity = new UserIdentity { Email = "new@example.com", PhoneNumber = "+1234567890" };
            var createdUserIdentity = new UserIdentity { Id = "new-user", Email = "new@example.com", PhoneNumber = "+1234567890" };

            _mockUserIdentityRepository
                .Setup(r => r.CreateAsync(userIdentity))
                .ReturnsAsync(createdUserIdentity);

            // Act
            var result = await _controller.CreateUserIdentity(userIdentity);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(UserIdentitiesController.GetUserIdentityById));
            createdResult.RouteValues["id"].Should().Be(createdUserIdentity.Id);
            
            var returnedUser = createdResult.Value.Should().BeAssignableTo<UserIdentity>().Subject;
            returnedUser.Id.Should().Be("new-user");
            returnedUser.Email.Should().Be("new@example.com");
            returnedUser.PhoneNumber.Should().Be("+1234567890");
        }

        [Fact]
        public async Task UpdateUserIdentity_WithValidData_ReturnsOkWithUpdatedUser()
        {
            // Arrange
            var userIdentity = new UserIdentity { Id = "user1", Email = "updated@example.com" };

            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync("user1"))
                .ReturnsAsync(new UserIdentity { Id = "user1", Email = "original@example.com" });

            _mockUserIdentityRepository
                .Setup(r => r.UpdateAsync(userIdentity))
                .ReturnsAsync(userIdentity);

            // Act
            var result = await _controller.UpdateUserIdentity("user1", userIdentity);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeAssignableTo<UserIdentity>().Subject;
            returnedUser.Id.Should().Be("user1");
            returnedUser.Email.Should().Be("updated@example.com");
        }

        [Fact]
        public async Task UpdateUserIdentity_WithMismatchedIds_ReturnsBadRequest()
        {
            // Arrange
            var userIdentity = new UserIdentity { Id = "user1", Email = "updated@example.com" };

            // Act
            var result = await _controller.UpdateUserIdentity("user2", userIdentity);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateUserIdentity_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userIdentity = new UserIdentity { Id = "nonexistent", Email = "updated@example.com" };

            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((UserIdentity)null);

            // Act
            var result = await _controller.UpdateUserIdentity("nonexistent", userIdentity);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteUserIdentity_WhenUserExists_ReturnsNoContent()
        {
            // Arrange
            _mockUserIdentityRepository
                .Setup(r => r.DeleteAsync("user1"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUserIdentity("user1");

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteUserIdentity_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockUserIdentityRepository
                .Setup(r => r.DeleteAsync("nonexistent"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUserIdentity("nonexistent");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetUserIdentityByLegacyId_WithValidParameters_ReturnsOkWithUser()
        {
            // Arrange
            const string applicationId = "app1";
            const string legacyUserId = "legacy1";
            var userIdentity = new UserIdentity { Id = "user1", Email = "user1@example.com" };

            _mockUserIdentityRepository
                .Setup(r => r.FindByLegacyUserIdAsync(applicationId, legacyUserId))
                .ReturnsAsync(userIdentity);

            // Act
            var result = await _controller.GetUserIdentityByLegacyId(applicationId, legacyUserId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeAssignableTo<UserIdentity>().Subject;
            returnedUser.Id.Should().Be("user1");
            returnedUser.Email.Should().Be("user1@example.com");
        }

        [Fact]
        public async Task GetUserIdentityByLegacyId_WithMissingParameters_ReturnsBadRequest()
        {
            // Arrange
            const string applicationId = "app1";
            const string legacyUserId = "";

            // Act
            var result = await _controller.GetUserIdentityByLegacyId(applicationId, legacyUserId);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AddLegacyUserId_WithValidData_ReturnsOk()
        {
            // Arrange
            const string userId = "user1";
            var dto = new LegacyUserIdDto
            {
                ApplicationId = "app1",
                LegacyUserId = "legacy1"
            };

            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(new UserIdentity { Id = userId });

            _mockUserIdentityRepository
                .Setup(r => r.AddLegacyUserIdAsync(userId, dto.ApplicationId, dto.LegacyUserId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddLegacyUserId(userId, dto);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task AddLegacyUserId_WithInvalidUser_ReturnsNotFound()
        {
            // Arrange
            const string userId = "nonexistent";
            var dto = new LegacyUserIdDto
            {
                ApplicationId = "app1",
                LegacyUserId = "legacy1"
            };

            _mockUserIdentityRepository
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((UserIdentity)null);

            // Act
            var result = await _controller.AddLegacyUserId(userId, dto);

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
    }
} 