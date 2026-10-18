FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Backend/API/Project.API/Project.API.csproj API/Project.API/
COPY Backend/Core/Project.Application/Project.Application.csproj Core/Project.Application/
COPY Backend/Core/Project.Domain/Project.Domain.csproj Core/Project.Domain/
COPY Backend/Infrastructure/Project.Infrastructure/Project.Infrastructure.csproj Infrastructure/Project.Infrastructure/
RUN dotnet restore API/Project.API/Project.API.csproj

COPY Backend/ ./
RUN dotnet publish API/Project.API/Project.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Project.API.dll"]
