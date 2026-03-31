using CookDating.Matching.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CookDating.Matching.Infrastructure;

public static class MatchingServiceRegistration
{
    public static IServiceCollection AddMatchingServices(this IServiceCollection services)
    {
        services.AddScoped<IMatchCandidateRepository, DynamoDbMatchCandidateRepository>();
        services.AddScoped<IMatchRepository, DynamoDbMatchRepository>();
        services.AddScoped<Application.Handlers.MatchingCommandHandlers>();
        return services;
    }
}
