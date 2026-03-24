# Use the official .NET 8.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY BackendVBNet.vbproj .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET 8.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
# Example: Override connection string via environment variable
# ENV ConnectionStrings__MariaDB=server=host.docker.internal;port=3306;database=game;user=root;password=yourpassword;

# Set the entry point
ENTRYPOINT ["dotnet", "BackendVBNet.dll"]