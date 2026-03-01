# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/MedCareHub.Api/MedCareHub.Api.csproj backend/src/MedCareHub.Api/
RUN dotnet restore backend/src/MedCareHub.Api/MedCareHub.Api.csproj

COPY backend ./backend
WORKDIR /src/backend/src/MedCareHub.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MedCareHub.Api.dll"]