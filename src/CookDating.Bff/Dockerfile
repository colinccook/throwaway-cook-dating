FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/CookDating.Bff/CookDating.Bff.csproj src/CookDating.Bff/
COPY src/CookDating.Profile/CookDating.Profile.csproj src/CookDating.Profile/
COPY src/CookDating.Matching/CookDating.Matching.csproj src/CookDating.Matching/
COPY src/CookDating.Conversation/CookDating.Conversation.csproj src/CookDating.Conversation/
COPY src/CookDating.SharedKernel/CookDating.SharedKernel.csproj src/CookDating.SharedKernel/
COPY src/CookDating.ServiceDefaults/CookDating.ServiceDefaults.csproj src/CookDating.ServiceDefaults/

RUN dotnet restore src/CookDating.Bff/CookDating.Bff.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/CookDating.Bff/CookDating.Bff.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CookDating.Bff.dll"]
