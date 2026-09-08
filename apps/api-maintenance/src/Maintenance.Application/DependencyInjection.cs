using FluentValidation;
using Maintenance.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application services and every <see cref="IValidator{T}"/> in this assembly.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();

        var validatorInterface = typeof(IValidator<>);
        var validators = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => (Service: i, Implementation: type)));

        foreach (var (service, implementation) in validators)
        {
            services.AddScoped(service, implementation);
        }

        return services;
    }
}
