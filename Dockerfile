FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ServiceLifeOS.slnx
RUN dotnet publish src/ServiceLifeOS.Api/ServiceLifeOS.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrations
WORKDIR /src
COPY . .
RUN dotnet restore ServiceLifeOS.slnx
RUN dotnet tool install --global dotnet-ef --version 10.0.8
ENV PATH="${PATH}:/root/.dotnet/tools"
CMD ["dotnet", "ef", "database", "update", "--project", "src/ServiceLifeOS.Infrastructure/ServiceLifeOS.Infrastructure.csproj", "--startup-project", "src/ServiceLifeOS.Api/ServiceLifeOS.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
EXPOSE 8080

FROM runtime-base AS bootstrap
COPY --from=build /app/publish .
CMD ["dotnet", "ServiceLifeOS.Api.dll", "--bootstrap"]

FROM runtime-base AS runtime
COPY --from=build /app/publish .
CMD ["sh", "-c", "dotnet ServiceLifeOS.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
