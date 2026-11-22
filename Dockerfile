ARG DOTNET_VERSION=11.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY StackDuel.Domain/StackDuel.Domain.csproj StackDuel.Domain/
COPY StackDuel.Application/StackDuel.Application.csproj StackDuel.Application/
COPY StackDuel.Infrastructure/StackDuel.Infrastructure.csproj StackDuel.Infrastructure/
COPY StackDuel.Api/StackDuel.Api.csproj StackDuel.Api/
RUN dotnet restore StackDuel.Api/StackDuel.Api.csproj

COPY StackDuel.Domain/ StackDuel.Domain/
COPY StackDuel.Application/ StackDuel.Application/
COPY StackDuel.Infrastructure/ StackDuel.Infrastructure/
COPY StackDuel.Api/ StackDuel.Api/
RUN dotnet publish StackDuel.Api/StackDuel.Api.csproj \
        --configuration ${BUILD_CONFIGURATION} \
        --no-restore \
        --output /app/publish \
    && rm -rf /app/publish/Tools

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0

COPY --from=build /app/publish ./

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "StackDuel.Api.dll"]
