using FluentValidation;
using MediatR;
using Project.Application.Behaviors;

namespace Project.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_CallsNext_WhenNoValidatorsAreRegistered()
    {
        var called = false;
        var behavior = new ValidationBehavior<SampleCommand, string>(Array.Empty<IValidator<SampleCommand>>());

        var result = await behavior.Handle(
            new SampleCommand("Ada"),
            () =>
            {
                called = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CallsNext_WhenEveryValidatorPasses()
    {
        var called = false;
        var behavior = new ValidationBehavior<SampleCommand, string>([new NotEmptyValidator(), new MinLengthValidator()]);

        var result = await behavior.Handle(
            new SampleCommand("Ada"),
            () =>
            {
                called = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ThrowsGroupedValidationErrors_WhenValidatorsFail()
    {
        var called = false;
        var behavior = new ValidationBehavior<SampleCommand, string>([new NotEmptyValidator(), new MinLengthValidator()]);

        var act = async () => await behavior.Handle(
            new SampleCommand(""),
            () =>
            {
                called = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<Project.Application.Exceptions.ValidationException>();
        exception.Which.Message.Should().Be("Validation failed.");
        exception.Which.Errors["Name"].Should().BeEquivalentTo("Name is required.", "Name is too short.");
        called.Should().BeFalse();
    }

    private sealed record SampleCommand(string Name) : IRequest<string>;

    private sealed class NotEmptyValidator : AbstractValidator<SampleCommand>
    {
        public NotEmptyValidator() =>
            RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required.");
    }

    private sealed class MinLengthValidator : AbstractValidator<SampleCommand>
    {
        public MinLengthValidator() =>
            RuleFor(command => command.Name).MinimumLength(3).WithMessage("Name is too short.");
    }
}
