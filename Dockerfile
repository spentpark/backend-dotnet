# syntax=docker/dockerfile:1

# ---------- Stage 1: build ----------
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

# ---------- Stage 2: runtime ----------
# Use the official .NET 8.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published app from the build stage.
# The files stay owned by root and are only readable: a containerized web app
# does not need write access to its own binaries.
COPY --from=build /app/publish .

# Expose the port the app runs on (unprivileged, so a non-root user can bind it)
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
# Example: Override connection string via environment variable
# ENV ConnectionStrings__MariaDB=server=host.docker.internal;port=3306;database=game;user=root;password=yourpassword;

# ASP.NET Core stores data-protection keys in $HOME/.aspnet. Pre-create that
# folder owned by the non-root "app" user so it still works without root.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

# Run as the non-root "app" user that ships in the .NET 8 Linux images
# (UID 1654) instead of the default root user. This is what silences the
# "This image runs with 'root' or 'containerAdministrator' as the default user"
# warning (hadolint DL3002 / image scanners), and limits the blast radius if
# the process is compromised.
USER app

# Set the entry point
ENTRYPOINT ["dotnet", "BackendVBNet.dll"]
