FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/CookDating.Conversation.Worker/CookDating.Conversation.Worker.csproj src/CookDating.Conversation.Worker/
COPY src/CookDating.Conversation/CookDating.Conversation.csproj src/CookDating.Conversation/
COPY src/CookDating.Matching/CookDating.Matching.csproj src/CookDating.Matching/
COPY src/CookDating.SharedKernel/CookDating.SharedKernel.csproj src/CookDating.SharedKernel/
COPY src/CookDating.ServiceDefaults/CookDating.ServiceDefaults.csproj src/CookDating.ServiceDefaults/

RUN dotnet restore src/CookDating.Conversation.Worker/CookDating.Conversation.Worker.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/CookDating.Conversation.Worker/CookDating.Conversation.Worker.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CookDating.Conversation.Worker.dll"]
