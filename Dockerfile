# Use the official Microsoft .NET 8 source image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["IbnElgm3a.csproj", "./"]
RUN dotnet restore

# Copy everything else and build the app
COPY . .
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose the port Railway expects (usually it picks up on the PORT env var)
# But standard .NET 8 in Docker usually listens on 8080 by default.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "IbnElgm3a.dll"]
