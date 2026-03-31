using CookDating.Profile.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CookDating.Profile.Infrastructure;

public static class ProfileServiceRegistration
{
    public static IServiceCollection AddProfileServices(this IServiceCollection services)
    {
        services.AddScoped<IProfileRepository, DynamoDbProfileRepository>();
        services.AddScoped<Application.Handlers.ProfileCommandHandlers>();
        return services;
    }
}
