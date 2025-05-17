# Use the official .NET 9.0 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application files
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 9.0 runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app/out ./

# Expose the port the application runs on
EXPOSE 5000
EXPOSE 5001

# Set the entry point for the container
ENTRYPOINT ["dotnet", "wasteM.dll"]
