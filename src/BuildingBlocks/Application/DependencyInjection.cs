using System.Reflection;
using BazarKav.BuildingBlocks.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BazarKav.BuildingBlocks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBazarKavApplication(this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (handlerAssemblies.Length == 0)
            throw new ArgumentException("At least one handler assembly is required.", nameof(handlerAssemblies));
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(handlerAssemblies.Distinct().ToArray());
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblies(handlerAssemblies.Distinct());
        return services;
    }
}
