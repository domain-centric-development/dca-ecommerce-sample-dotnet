# syntax=docker/dockerfile:1
# Build context is this directory; the DomainCentric.* packages are restored from NuGet.org.
#   docker build -t dca-shop-dotnet .
#   docker run --rm -p 5080:8080 dca-shop-dotnet
# (Working against an unreleased ../dca-dotnet is a local-SDK affair: `dotnet test -p:UseLocalDcaDotnet=true`.)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first so restore is cached across source changes
COPY DcaShop.sln Directory.Build.props global.json ./
COPY src ./src
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/DcaShop.Web/DcaShop.Web.csproj

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/DcaShop.Web/DcaShop.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DcaShop.Web.dll"]
