using Amazon.CognitoIdentityProvider;
using CookDating.Bff.Hubs;
using CookDating.Bff.Infrastructure;
using CookDating.Conversation.Infrastructure;
using CookDating.Matching.Infrastructure;
using CookDating.Profile.Infrastructure;
using CookDating.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// AWS services
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddAwsBootstrapper();

// Cognito client
var awsServiceUrl = builder.Configuration["AWS:ServiceURL"];
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
{
    var config = new AmazonCognitoIdentityProviderConfig { ServiceURL = awsServiceUrl };
    return new AmazonCognitoIdentityProviderClient("test", "test", config);
});

// Cognito bootstrap (creates user pool + client in floci, publishes IDs)
builder.Services.AddSingleton<CognitoSettings>();
builder.Services.AddHostedService<CognitoBootstrapHostedService>();

// Bounded context services
builder.Services.AddProfileServices();
builder.Services.AddMatchingServices();
builder.Services.AddConversationServices();

// Auth — simplified for prototype (accept any JWT without signature validation)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        // Use the legacy JwtSecurityTokenHandler so SignatureValidator works correctly
        options.UseSecurityTokenValidators = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            SignatureValidator = (token, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// CORS — must use AllowCredentials for SignalR (incompatible with AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// Controllers + SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CookDating.Bff.Infrastructure.UserIdLoggingScopeMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

// SignalR hub endpoints
app.MapHub<MatchingHub>("/hubs/matching");
app.MapHub<ConversationHub>("/hubs/conversation");

app.Run();
