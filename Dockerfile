################ BUILD STAGE

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# copy project file firstly (better Docker layer caching)
COPY ["src/TeamsIntegration.Api/TeamsIntegration.Api.csproj", "src/TeamsIntegration.Api/"]

# restore nuget packages
RUN dotnet restore "src/TeamsIntegration.Api/TeamsIntegration.Api.csproj"

# Copy the remaining source code
COPY . .

# publish the application
WORKDIR "/src/src/TeamsIntegration.Api"

RUN dotnet publish "TeamsIntegration.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


################ RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "TeamsIntegration.Api.dll"]