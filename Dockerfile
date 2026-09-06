# syntax=docker/dockerfile:1
# Build context is the PARENT directory: until the DomainCentric.* packages are on NuGet, the sample
# references the sibling checkout ../dca-dotnet as projects (Directory.Build.props).
#   docker build -f dca-ecommerce-sample-dotnet/Dockerfile -t dca-shop-dotnet ..
#   docker run --rm -p 5080:8080 dca-shop-dotnet
# Once the packages are published, drop the dca-dotnet COPY and build with context "." like the Java sample.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Sibling library — only what the project references need
COPY dca-dotnet/Directory.Build.props dca-dotnet/global.json ./dca-dotnet/
COPY dca-dotnet/branding ./dca-dotnet/branding
COPY dca-dotnet/src ./dca-dotnet/src

# Sample: project files first so restore is cached across source changes
WORKDIR /src/dca-ecommerce-sample-dotnet
COPY dca-ecommerce-sample-dotnet/DcaShop.sln dca-ecommerce-sample-dotnet/Directory.Build.props dca-ecommerce-sample-dotnet/global.json ./
COPY dca-ecommerce-sample-dotnet/src ./src
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
