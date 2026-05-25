FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY metavix.sln ./
COPY src/API/API.csproj ./src/API/
COPY src/Application/Application.csproj ./src/Application/
COPY src/Domain/Domain.csproj ./src/Domain/
COPY src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY src/Contracts/Contracts.csproj ./src/Contracts/

RUN dotnet restore

COPY src/ ./src/

RUN dotnet publish src/API/API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
