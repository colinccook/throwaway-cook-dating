using CookDating.Conversation.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CookDating.Conversation.Infrastructure;

public static class ConversationServiceRegistration
{
    public static IServiceCollection AddConversationServices(this IServiceCollection services)
    {
        services.AddScoped<IConversationRepository, DynamoDbConversationRepository>();
        services.AddScoped<Application.Handlers.ConversationCommandHandlers>();
        return services;
    }
}
