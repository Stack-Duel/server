# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props","."]
COPY ["Directory.Build.props","."]
COPY ["nuget.config","."]
COPY ["StackDuel.Api/StackDuel.Api.csproj","StackDuel.Api/"]
COPY ["StackDuel.Application/StackDuel.Application.csproj","StackDuel.Application/"]
COPY ["StackDuel.Domain/StackDuel.Domain.csproj","StackDuel.Domain/"]
COPY ["StackDuel.Infrastructure/StackDuel.Infrastructure.csproj","StackDuel.Infrastructure/"]
RUN dotnet restore "./StackDuel.Api/StackDuel.Api.csproj"
COPY . .
WORKDIR "/src/StackDuel.Api"
RUN dotnet build "./StackDuel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StackDuel.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StackDuel.Api.dll"]