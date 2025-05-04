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
    public class RoleClaimMappingControllerTests
    {
        private readonly Mock<IRoleMappingService> _mockRoleMappingService;
        private readonly Mock<IClaimMappingService> _mockClaimMappingService;
        private readonly Mock<IApplicationRepository> _mockApplicationRepository;
        private readonly Mock<ILogger<RoleClaimMappingController>> _mockLogger;
        private readonly RoleClaimMappingController _controller;

        public RoleClaimMappingControllerTests()
        {
            _mockRoleMappingService = new Mock<IRoleMappingService>();
            _mockClaimMappingService = new Mock<IClaimMappingService>();
            _mockApplicationRepository = new Mock<IApplicationRepository>();
            _mockLogger = new Mock<ILogger<RoleClaimMappingController>>();

            _controller = new RoleClaimMappingController(
                _mockRoleMappingService.Object,
                _mockClaimMappingService.Object,
                _mockApplicationRepository.Object,
                _mockLogger.Object);

            // Setup default admin user
            SetupAdminUser();
        }

        #region Role Mapping Tests

        [Fact]
        public async Task GetAllRoleMappings_ReturnsOkWithMappings()
        {
            // Arrange
            var roleMappings = new List<RoleMapping>
            {
                new RoleMapping { Id = "rm1", SourceApplicationId = "app1", TargetApplicationId = "app2", SourceRole = "User", TargetRole = "StandardUser" },
                new RoleMapping { Id = "rm2", SourceApplicationId = "app1", TargetApplicationId = "app2", SourceRole = "Admin", TargetRole = "Administrator" }
            };

            _mockRoleMappingService
                .Setup(s => s.GetAllRoleMappingsAsync(0, 20))
                .ReturnsAsync(roleMappings);

            // Act
            var result = await _controller.GetAllRoleMappings();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<RoleMapping>>().Subject;
            returnedMappings.Should().HaveCount(2);
            returnedMappings.Should().Contain(m => m.Id == "rm1");
            returnedMappings.Should().Contain(m => m.Id == "rm2");
        }

        [Fact]
        public async Task GetRoleMappingById_WhenMappingExists_ReturnsOkWithMapping()
        {
            // Arrange
            var roleMapping = new RoleMapping 
            { 
                Id = "rm1", 
                SourceApplicationId = "app1", 
                TargetApplicationId = "app2", 
                SourceRole = "User", 
                TargetRole = "StandardUser" 
            };

            _mockRoleMappingService
                .Setup(s => s.GetRoleMappingByIdAsync("rm1"))
                .ReturnsAsync(roleMapping);

            // Act
            var result = await _controller.GetRoleMappingById("rm1");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMapping = okResult.Value.Should().BeAssignableTo<RoleMapping>().Subject;
            returnedMapping.Id.Should().Be("rm1");
            returnedMapping.SourceRole.Should().Be("User");
            returnedMapping.TargetRole.Should().Be("StandardUser");
        }

        [Fact]
        public async Task GetRoleMappingById_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockRoleMappingService
                .Setup(s => s.GetRoleMappingByIdAsync("nonexistent"))
                .ReturnsAsync((RoleMapping)null);

            // Act
            var result = await _controller.GetRoleMappingById("nonexistent");

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateRoleMapping_WithValidData_ReturnsCreatedMapping()
        {
            // Arrange
            var createDto = new CreateRoleMappingDto
            {
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceRole = "Admin",
                TargetRole = "Administrator"
            };

            var createdMapping = new RoleMapping
            {
                Id = "rm1",
                SourceApplicationId = createDto.SourceApplicationId,
                TargetApplicationId = createDto.TargetApplicationId,
                SourceRole = createDto.SourceRole,
                TargetRole = createDto.TargetRole,
                CreatedAt = DateTime.UtcNow
            };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app1"))
                .ReturnsAsync(new Application { Id = "app1" });

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app2"))
                .ReturnsAsync(new Application { Id = "app2" });

            _mockRoleMappingService
                .Setup(s => s.CreateRoleMappingAsync(
                    createDto.SourceApplicationId,
                    createDto.TargetApplicationId,
                    createDto.SourceRole,
                    createDto.TargetRole))
                .ReturnsAsync(createdMapping);

            // Act
            var result = await _controller.CreateRoleMapping(createDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(RoleClaimMappingController.GetRoleMappingById));
            createdResult.RouteValues["id"].Should().Be(createdMapping.Id);
            
            var returnedMapping = createdResult.Value.Should().BeAssignableTo<RoleMapping>().Subject;
            returnedMapping.Id.Should().Be("rm1");
            returnedMapping.SourceRole.Should().Be("Admin");
            returnedMapping.TargetRole.Should().Be("Administrator");
        }

        [Fact]
        public async Task TranslateRole_WithValidData_ReturnsTargetRole()
        {
            // Arrange
            string sourceApplicationId = "app1";
            string targetApplicationId = "app2";
            string sourceRole = "Admin";
            string targetRole = "Administrator";

            _mockRoleMappingService
                .Setup(s => s.TranslateRoleAsync(sourceApplicationId, targetApplicationId, sourceRole))
                .ReturnsAsync(targetRole);

            // Act
            var result = await _controller.TranslateRole(sourceApplicationId, targetApplicationId, sourceRole);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic value = okResult.Value;
            ((string)value.targetRole).Should().Be(targetRole);
        }

        [Fact]
        public async Task GetRoleMappingsByApplication_WhenApplicationExists_ReturnsOkWithMappings()
        {
            // Arrange
            string applicationId = "app1";
            var roleMappings = new List<RoleMapping>
            {
                new RoleMapping { Id = "rm1", SourceApplicationId = applicationId, TargetApplicationId = "app2", SourceRole = "User", TargetRole = "StandardUser" },
                new RoleMapping { Id = "rm2", SourceApplicationId = applicationId, TargetApplicationId = "app2", SourceRole = "Admin", TargetRole = "Administrator" }
            };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(new Application { Id = applicationId });

            _mockRoleMappingService
                .Setup(s => s.GetRoleMappingsByApplicationAsync(applicationId))
                .ReturnsAsync(roleMappings);

            // Act
            var result = await _controller.GetRoleMappingsByApplication(applicationId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<RoleMapping>>().Subject;
            returnedMappings.Should().HaveCount(2);
            returnedMappings.Should().Contain(m => m.Id == "rm1");
            returnedMappings.Should().Contain(m => m.Id == "rm2");
        }

        [Fact]
        public async Task GetRoleMappingsByApplication_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string applicationId = "nonexistent";

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync((Application)null);

            // Act
            var result = await _controller.GetRoleMappingsByApplication(applicationId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateRoleMapping_WithValidData_ReturnsUpdatedMapping()
        {
            // Arrange
            string id = "rm1";
            var updateDto = new UpdateRoleMappingDto
            {
                SourceRole = "UpdatedRole",
                TargetRole = "UpdatedTargetRole"
            };

            var existingMapping = new RoleMapping
            {
                Id = id,
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceRole = "OldRole",
                TargetRole = "OldTargetRole"
            };

            var updatedMapping = new RoleMapping
            {
                Id = id,
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceRole = updateDto.SourceRole,
                TargetRole = updateDto.TargetRole,
                UpdatedAt = DateTime.UtcNow
            };

            _mockRoleMappingService
                .Setup(s => s.GetRoleMappingByIdAsync(id))
                .ReturnsAsync(existingMapping);

            _mockRoleMappingService
                .Setup(s => s.UpdateRoleMappingAsync(
                    id,
                    updateDto.SourceRole,
                    updateDto.TargetRole))
                .ReturnsAsync(updatedMapping);

            // Act
            var result = await _controller.UpdateRoleMapping(id, updateDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMapping = okResult.Value.Should().BeAssignableTo<RoleMapping>().Subject;
            returnedMapping.Id.Should().Be(id);
            returnedMapping.SourceRole.Should().Be(updateDto.SourceRole);
            returnedMapping.TargetRole.Should().Be(updateDto.TargetRole);
        }

        [Fact]
        public async Task UpdateRoleMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string id = "nonexistent";
            var updateDto = new UpdateRoleMappingDto
            {
                SourceRole = "UpdatedRole",
                TargetRole = "UpdatedTargetRole"
            };

            _mockRoleMappingService
                .Setup(s => s.GetRoleMappingByIdAsync(id))
                .ReturnsAsync((RoleMapping)null);

            // Act
            var result = await _controller.UpdateRoleMapping(id, updateDto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteRoleMapping_WhenMappingExists_ReturnsNoContent()
        {
            // Arrange
            string id = "rm1";

            _mockRoleMappingService
                .Setup(s => s.DeleteRoleMappingAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteRoleMapping(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteRoleMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string id = "nonexistent";

            _mockRoleMappingService
                .Setup(s => s.DeleteRoleMappingAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteRoleMapping(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Claim Mapping Tests

        [Fact]
        public async Task GetAllClaimMappings_ReturnsOkWithMappings()
        {
            // Arrange
            var claimMappings = new List<ClaimMapping>
            {
                new ClaimMapping { Id = "cm1", SourceApplicationId = "app1", TargetApplicationId = "app2", SourceClaimType = "email", TargetClaimType = "email_address" },
                new ClaimMapping { Id = "cm2", SourceApplicationId = "app1", TargetApplicationId = "app2", SourceClaimType = "name", TargetClaimType = "full_name" }
            };

            _mockClaimMappingService
                .Setup(s => s.GetAllClaimMappingsAsync(0, 20))
                .ReturnsAsync(claimMappings);

            // Act
            var result = await _controller.GetAllClaimMappings();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<ClaimMapping>>().Subject;
            returnedMappings.Should().HaveCount(2);
            returnedMappings.Should().Contain(m => m.Id == "cm1");
            returnedMappings.Should().Contain(m => m.Id == "cm2");
        }

        [Fact]
        public async Task GetClaimMappingById_WhenMappingExists_ReturnsOkWithMapping()
        {
            // Arrange
            var claimMapping = new ClaimMapping 
            { 
                Id = "cm1", 
                SourceApplicationId = "app1", 
                TargetApplicationId = "app2", 
                SourceClaimType = "email", 
                TargetClaimType = "email_address" 
            };

            _mockClaimMappingService
                .Setup(s => s.GetClaimMappingByIdAsync("cm1"))
                .ReturnsAsync(claimMapping);

            // Act
            var result = await _controller.GetClaimMappingById("cm1");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMapping = okResult.Value.Should().BeAssignableTo<ClaimMapping>().Subject;
            returnedMapping.Id.Should().Be("cm1");
            returnedMapping.SourceClaimType.Should().Be("email");
            returnedMapping.TargetClaimType.Should().Be("email_address");
        }

        [Fact]
        public async Task CreateClaimMapping_WithValidData_ReturnsCreatedMapping()
        {
            // Arrange
            var createDto = new CreateClaimMappingDto
            {
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceClaimType = "email",
                TargetClaimType = "email_address",
                TransformationExpression = "value.ToLower()"
            };

            var createdMapping = new ClaimMapping
            {
                Id = "cm1",
                SourceApplicationId = createDto.SourceApplicationId,
                TargetApplicationId = createDto.TargetApplicationId,
                SourceClaimType = createDto.SourceClaimType,
                TargetClaimType = createDto.TargetClaimType,
                TransformationExpression = createDto.TransformationExpression,
                CreatedAt = DateTime.UtcNow
            };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app1"))
                .ReturnsAsync(new Application { Id = "app1" });

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync("app2"))
                .ReturnsAsync(new Application { Id = "app2" });

            _mockClaimMappingService
                .Setup(s => s.CreateClaimMappingAsync(
                    createDto.SourceApplicationId,
                    createDto.TargetApplicationId,
                    createDto.SourceClaimType,
                    createDto.TargetClaimType,
                    createDto.TransformationExpression))
                .ReturnsAsync(createdMapping);

            // Act
            var result = await _controller.CreateClaimMapping(createDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(RoleClaimMappingController.GetClaimMappingById));
            createdResult.RouteValues["id"].Should().Be(createdMapping.Id);
            
            var returnedMapping = createdResult.Value.Should().BeAssignableTo<ClaimMapping>().Subject;
            returnedMapping.Id.Should().Be("cm1");
            returnedMapping.SourceClaimType.Should().Be("email");
            returnedMapping.TargetClaimType.Should().Be("email_address");
            returnedMapping.TransformationExpression.Should().Be("value.ToLower()");
        }

        [Fact]
        public async Task TransformClaims_WithValidData_ReturnsTransformedClaims()
        {
            // Arrange
            var request = new ClaimTransformationRequest
            {
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                Claims = new List<UserClaim>
                {
                    new UserClaim { Type = "email", Value = "USER@example.com" },
                    new UserClaim { Type = "name", Value = "John Doe" }
                }
            };

            var transformedClaims = new List<ClaimTransformationResult>
            {
                new ClaimTransformationResult 
                { 
                    OriginalType = "email", 
                    OriginalValue = "USER@example.com",
                    TransformedType = "email_address",
                    TransformedValue = "user@example.com"
                },
                new ClaimTransformationResult 
                { 
                    OriginalType = "name", 
                    OriginalValue = "John Doe",
                    TransformedType = "full_name",
                    TransformedValue = "John Doe"
                }
            };

            _mockClaimMappingService
                .Setup(s => s.TransformClaimsAsync(
                    request.SourceApplicationId,
                    request.TargetApplicationId,
                    It.IsAny<IEnumerable<UserClaim>>()))
                .ReturnsAsync(transformedClaims);

            // Act
            var result = await _controller.TransformClaims(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedClaims = okResult.Value.Should().BeAssignableTo<IEnumerable<ClaimTransformationResult>>().Subject;
            returnedClaims.Should().HaveCount(2);
            returnedClaims.Should().Contain(c => c.OriginalType == "email" && c.TransformedType == "email_address");
            returnedClaims.Should().Contain(c => c.OriginalType == "name" && c.TransformedType == "full_name");
        }

        [Fact]
        public async Task GetClaimMappingsByApplication_WhenApplicationExists_ReturnsOkWithMappings()
        {
            // Arrange
            string applicationId = "app1";
            var claimMappings = new List<ClaimMapping>
            {
                new ClaimMapping { Id = "cm1", SourceApplicationId = applicationId, SourceClaimType = "email", TargetClaimType = "email_address" },
                new ClaimMapping { Id = "cm2", SourceApplicationId = applicationId, SourceClaimType = "name", TargetClaimType = "full_name" }
            };

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(new Application { Id = applicationId });

            _mockClaimMappingService
                .Setup(s => s.GetClaimMappingsByApplicationAsync(applicationId))
                .ReturnsAsync(claimMappings);

            // Act
            var result = await _controller.GetClaimMappingsByApplication(applicationId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMappings = okResult.Value.Should().BeAssignableTo<IEnumerable<ClaimMapping>>().Subject;
            returnedMappings.Should().HaveCount(2);
            returnedMappings.Should().Contain(m => m.Id == "cm1");
            returnedMappings.Should().Contain(m => m.Id == "cm2");
        }

        [Fact]
        public async Task GetClaimMappingsByApplication_WhenApplicationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string applicationId = "nonexistent";

            _mockApplicationRepository
                .Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync((Application)null);

            // Act
            var result = await _controller.GetClaimMappingsByApplication(applicationId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateClaimMapping_WithValidData_ReturnsUpdatedMapping()
        {
            // Arrange
            string id = "cm1";
            var updateDto = new UpdateClaimMappingDto
            {
                SourceClaimType = "updatedEmail",
                TargetClaimType = "updatedEmailAddress",
                TransformationExpression = "value.ToUpper()"
            };

            var existingMapping = new ClaimMapping
            {
                Id = id,
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceClaimType = "email",
                TargetClaimType = "email_address",
                TransformationExpression = "value.ToLower()"
            };

            var updatedMapping = new ClaimMapping
            {
                Id = id,
                SourceApplicationId = "app1",
                TargetApplicationId = "app2",
                SourceClaimType = updateDto.SourceClaimType,
                TargetClaimType = updateDto.TargetClaimType,
                TransformationExpression = updateDto.TransformationExpression,
                UpdatedAt = DateTime.UtcNow
            };

            _mockClaimMappingService
                .Setup(s => s.GetClaimMappingByIdAsync(id))
                .ReturnsAsync(existingMapping);

            _mockClaimMappingService
                .Setup(s => s.UpdateClaimMappingAsync(
                    id,
                    updateDto.SourceClaimType,
                    updateDto.TargetClaimType,
                    updateDto.TransformationExpression))
                .ReturnsAsync(updatedMapping);

            // Act
            var result = await _controller.UpdateClaimMapping(id, updateDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMapping = okResult.Value.Should().BeAssignableTo<ClaimMapping>().Subject;
            returnedMapping.Id.Should().Be(id);
            returnedMapping.SourceClaimType.Should().Be(updateDto.SourceClaimType);
            returnedMapping.TargetClaimType.Should().Be(updateDto.TargetClaimType);
            returnedMapping.TransformationExpression.Should().Be(updateDto.TransformationExpression);
        }

        [Fact]
        public async Task UpdateClaimMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string id = "nonexistent";
            var updateDto = new UpdateClaimMappingDto
            {
                SourceClaimType = "updatedEmail",
                TargetClaimType = "updatedEmailAddress",
                TransformationExpression = "value.ToUpper()"
            };

            _mockClaimMappingService
                .Setup(s => s.GetClaimMappingByIdAsync(id))
                .ReturnsAsync((ClaimMapping)null);

            // Act
            var result = await _controller.UpdateClaimMapping(id, updateDto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteClaimMapping_WhenMappingExists_ReturnsNoContent()
        {
            // Arrange
            string id = "cm1";

            _mockClaimMappingService
                .Setup(s => s.DeleteClaimMappingAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteClaimMapping(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteClaimMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string id = "nonexistent";

            _mockClaimMappingService
                .Setup(s => s.DeleteClaimMappingAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteClaimMapping(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

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