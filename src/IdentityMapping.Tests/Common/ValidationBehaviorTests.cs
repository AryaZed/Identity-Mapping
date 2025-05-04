using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using IdentityMapping.Common.Behaviors;
using MediatR;
using Moq;

namespace IdentityMapping.Tests.Common;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsExist()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var request = new TestRequest();
        var response = new TestResponse();
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        nextMock.Setup(x => x()).ReturnsAsync(response);
        
        // Act
        var result = await behavior.Handle(request, nextMock.Object, CancellationToken.None);
        
        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldCallNext_WhenAllValidatorsPass()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        var validators = new List<IValidator<TestRequest>> { validatorMock.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var request = new TestRequest();
        var response = new TestResponse();
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        nextMock.Setup(x => x()).ReturnsAsync(response);
        
        // Act
        var result = await behavior.Handle(request, nextMock.Object, CancellationToken.None);
        
        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Once);
        validatorMock.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var validationFailure = new ValidationFailure("Property", "Error message");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        var validators = new List<IValidator<TestRequest>> { validatorMock.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var request = new TestRequest();
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        
        // Act & Assert
        var action = () => behavior.Handle(request, nextMock.Object, CancellationToken.None);
        
        await action.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "Error message"));
        
        nextMock.Verify(x => x(), Times.Never);
    }
    
    // Test classes
    private class TestRequest { }
    private class TestResponse { }
} 