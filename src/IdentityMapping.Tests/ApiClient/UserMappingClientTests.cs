using FluentAssertions;
using IdentityMapping.ApiClient;
using IdentityMapping.ApiClient.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace IdentityMapping.Tests.ApiClient;

public class UserMappingClientTests
{
    private readonly Mock<ILogger<UserMappingClient>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly UserMappingClient _client;

    public UserMappingClientTests()
    {
        _loggerMock = new Mock<ILogger<UserMappingClient>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _client = new UserMappingClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserMappingAsync_ShouldReturnMapping_WhenMappingExists()
    {
        // Arrange
        var userId = "user123";
        var externalSystem = "system1";
        var expectedMapping = new UserMappingDto
        {
            Id = "mapping123",
            UserId = userId,
            ExternalSystem = externalSystem,
            ExternalId = "ext123",
            CreatedAt = DateTime.UtcNow
        };

        SetupMockResponse($"api/user-mappings/{userId}/{externalSystem}", expectedMapping);

        // Act
        var result = await _client.GetUserMappingAsync(userId, externalSystem);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedMapping.Id);
        result.UserId.Should().Be(userId);
        result.ExternalSystem.Should().Be(externalSystem);
    }

    [Fact]
    public async Task GetUserMappingsByPhoneNumberAsync_ShouldReturnListOfMappings_WhenUserHasMappings()
    {
        // Arrange
        var phoneNumber = "+1234567890";
        var expectedMappings = new List<UserMappingDto>
        {
            new() { Id = "mapping1", PhoneNumber = phoneNumber, ExternalSystem = "system1", ExternalId = "ext1" },
            new() { Id = "mapping2", PhoneNumber = phoneNumber, ExternalSystem = "system2", ExternalId = "ext2" }
        };

        SetupMockResponse($"api/user-mappings/phone/{phoneNumber}", expectedMappings);

        // Act
        var result = await _client.GetUserMappingsByPhoneNumberAsync(phoneNumber);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("mapping1");
        result[1].Id.Should().Be("mapping2");
        result[0].PhoneNumber.Should().Be(phoneNumber);
        result[1].PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public async Task GetUserMappingByPhoneNumberAsync_ShouldReturnMapping_WhenMappingExists()
    {
        // Arrange
        var phoneNumber = "+1234567890";
        var externalSystem = "system1";
        var expectedMapping = new UserMappingDto
        {
            Id = "mapping123",
            PhoneNumber = phoneNumber,
            ExternalSystem = externalSystem,
            ExternalId = "ext123",
            CreatedAt = DateTime.UtcNow
        };

        SetupMockResponse($"api/user-mappings/phone/{phoneNumber}/{externalSystem}", expectedMapping);

        // Act
        var result = await _client.GetUserMappingByPhoneNumberAsync(phoneNumber, externalSystem);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedMapping.Id);
        result.PhoneNumber.Should().Be(phoneNumber);
        result.ExternalSystem.Should().Be(externalSystem);
    }

    [Fact]
    public async Task GetUserMappingsAsync_ShouldReturnListOfMappings_WhenUserHasMappings()
    {
        // Arrange
        var userId = "user123";
        var expectedMappings = new List<UserMappingDto>
        {
            new() { Id = "mapping1", UserId = userId, ExternalSystem = "system1", ExternalId = "ext1" },
            new() { Id = "mapping2", UserId = userId, ExternalSystem = "system2", ExternalId = "ext2" }
        };

        SetupMockResponse($"api/user-mappings/{userId}", expectedMappings);

        // Act
        var result = await _client.GetUserMappingsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("mapping1");
        result[1].Id.Should().Be("mapping2");
    }

    [Fact]
    public async Task CreateUserMappingAsync_ShouldReturnNewMapping_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateUserMappingRequest 
        { 
            UserId = "user123",
            PhoneNumber = "+1234567890",
            ExternalSystem = "system1", 
            ExternalId = "ext123" 
        };
        
        var expectedMapping = new UserMappingDto
        {
            Id = "newMapping123",
            UserId = request.UserId,
            PhoneNumber = request.PhoneNumber,
            ExternalSystem = request.ExternalSystem,
            ExternalId = request.ExternalId,
            CreatedAt = DateTime.UtcNow
        };

        SetupMockResponse("api/user-mappings", expectedMapping, HttpMethod.Post);

        // Act
        var result = await _client.CreateUserMappingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedMapping.Id);
        result.UserId.Should().Be(request.UserId);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.ExternalSystem.Should().Be(request.ExternalSystem);
        result.ExternalId.Should().Be(request.ExternalId);
    }

    private void SetupMockResponse<T>(string requestUri, T responseContent, HttpMethod method = null)
    {
        method ??= HttpMethod.Get;
        
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(responseContent))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method && 
                    req.RequestUri!.ToString().EndsWith(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }
} 