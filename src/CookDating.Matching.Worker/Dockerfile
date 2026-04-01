FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/CookDating.Matching.Worker/CookDating.Matching.Worker.csproj src/CookDating.Matching.Worker/
COPY src/CookDating.Matching/CookDating.Matching.csproj src/CookDating.Matching/
COPY src/CookDating.SharedKernel/CookDating.SharedKernel.csproj src/CookDating.SharedKernel/
COPY src/CookDating.ServiceDefaults/CookDating.ServiceDefaults.csproj src/CookDating.ServiceDefaults/

RUN dotnet restore src/CookDating.Matching.Worker/CookDating.Matching.Worker.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/CookDating.Matching.Worker/CookDating.Matching.Worker.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CookDating.Matching.Worker.dll"]
