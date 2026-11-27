using ErrorOr;
using FluentValidation;
using Kasio.Application.Common.Behaviors;
using MediatR;
using Xunit;

namespace Kasio.UnitTests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_returns_validation_error_when_a_rule_fails()
    {
        var behavior = new ValidationBehavior<Ping, ErrorOr<string>>([new PingValidator()]);
        RequestHandlerDelegate<ErrorOr<string>> next = _ => Task.FromResult<ErrorOr<string>>("ok");

        var response = await behavior.Handle(new Ping(string.Empty), next, CancellationToken.None);

        Assert.True(response.IsError);
        Assert.NotEmpty(response.Errors);
        Assert.Equal("Value", response.FirstError.Code);
    }

    [Fact]
    public async Task Handle_calls_next_when_all_rules_pass()
    {
        var behavior = new ValidationBehavior<Ping, ErrorOr<string>>([new PingValidator()]);
        RequestHandlerDelegate<ErrorOr<string>> next = _ => Task.FromResult<ErrorOr<string>>("ok");

        var response = await behavior.Handle(new Ping("value"), next, CancellationToken.None);

        Assert.Equal("ok", response.Value);
    }

    public sealed record Ping(string Value) : IRequest<ErrorOr<string>>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(request => request.Value).NotEmpty();
    }
}
