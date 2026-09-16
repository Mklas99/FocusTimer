# Devcontainer Dockerfile for FocusTimer

# Use the .NET 8.0 SDK image as the base for development
#FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime

# Set environment variables for the devcontainer
ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_ENVIRONMENT=Development

# Set the working directory
WORKDIR /workspace

# Copy only the project file and restore as distinct layers
# ...existing code...
COPY ./src/FocusTimer.App/FocusTimer.App.csproj ./src/FocusTimer.App/
# ...existing code...
RUN dotnet restore "./src/FocusTimer.App/FocusTimer.App.csproj"

# Copy the rest of the source code.
# .dockerignore excludes VCS metadata, build output, and common secret/credential
# file patterns (.env, *.pem, *.key, appsettings.*.json, .ssh, .aws, etc.) so this
# recursive copy does not leak sensitive data into the image.
COPY . .

# Install Node.js (LTS) and npm, then install openspec globally
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/* \
    && npm install -g @fission-ai/openspec@latest

# Expose the port your app runs on (adjust if needed)
EXPOSE 5000

# Set the default command for the devcontainer
CMD ["dotnet", "watch", "run", "--project", "FocusTimer.App.csproj"]
