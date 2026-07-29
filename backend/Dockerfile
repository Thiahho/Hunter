FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY Hunter.sln .
COPY src/Hunter.Api/Hunter.Api.csproj src/Hunter.Api/
COPY src/Hunter.Application/Hunter.Application.csproj src/Hunter.Application/
COPY src/Hunter.Domain/Hunter.Domain.csproj src/Hunter.Domain/
COPY src/Hunter.Infrastructure/Hunter.Infrastructure.csproj src/Hunter.Infrastructure/
COPY src/Hunter.Shared/Hunter.Shared.csproj src/Hunter.Shared/
RUN dotnet restore src/Hunter.Api/Hunter.Api.csproj

COPY src/ src/
RUN dotnet publish src/Hunter.Api/Hunter.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Hunter.Api.dll"]
