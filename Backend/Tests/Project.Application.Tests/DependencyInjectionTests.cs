using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Auth.Commands.Login;
using Project.Application.Behaviors;

namespace Project.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatorValidatorsAndValidationBehavior()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMediator>().Should().NotBeNull();
        provider.GetRequiredService<IValidator<LoginCommand>>().Should().BeOfType<LoginCommandValidator>();
        services.Should().Contain(descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            descriptor.ImplementationType == typeof(ValidationBehavior<,>));
    }
}
