using System.Reflection;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace Kasio.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    /// <summary>
    /// ErrorOr&lt;T&gt; không có interface factory nên buộc phải qua reflection.
    /// Delegate được dựng một lần cho mỗi TResponse đóng (không reflection mỗi request) và
    /// fail-fast nếu type không hỗ trợ, thay vì trả null rồi nổ NullReferenceException.
    /// </summary>
    private static readonly Func<List<Error>, TResponse> FromErrors = CreateFactory();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count > 0)
            return FromErrors(errors);

        return await next(cancellationToken);
    }

    private static Func<List<Error>, TResponse> CreateFactory()
    {
        var method = typeof(TResponse)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, "From", StringComparison.Ordinal)
                && candidate.ReturnType == typeof(TResponse)
                && candidate.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(List<Error>));

        if (method is null)
        {
            throw new InvalidOperationException(
                $"{typeof(TResponse)} không có static From(List<Error>); " +
                "ValidationBehavior chỉ hỗ trợ response kiểu ErrorOr<T>.");
        }

        return method.CreateDelegate<Func<List<Error>, TResponse>>();
    }
}
