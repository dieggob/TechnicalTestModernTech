using System.Text.Json;
using FluentValidation;
using ValidationException = Maintenance.Application.Exceptions.ValidationException;

namespace Maintenance.Application.Validation;

/// <summary>
/// The single way services validate input: runs a validator and throws the application's
/// <see cref="ValidationException"/> with field errors grouped and camelCased for the client.
/// </summary>
public static class ValidationRunner
{
    public static void Validate<T>(IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        throw new ValidationException(errors);
    }
}
