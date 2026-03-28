#!/bin/sh
# Run SonarScanner for .NET (MSBuild) locally.
# Usage: ./scripts/run-sonar-dotnet.sh <SONAR_TOKEN> [SONAR_HOST_URL]

TOKEN=${1:-$SONAR_TOKEN}
HOST=${2:-${SONAR_HOST_URL:-http://localhost:9000}}

if [ -z "$TOKEN" ]; then
  echo "Error: SONAR token not provided. Pass as first arg or set SONAR_TOKEN env var."
  exit 1
fi

# Ensure dotnet-sonarscanner is installed as a global tool
if ! dotnet tool list -g | grep -q dotnet-sonarscanner; then
  echo "Installing dotnet-sonarscanner global tool..."
  dotnet tool install --global dotnet-sonarscanner || {
    echo "Failed to install dotnet-sonarscanner. Aborting."; exit 1
  }
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

echo "Starting SonarScanner (MSBuild) begin... (verbose)"
dotnet sonarscanner begin /k:"focustimer" /d:sonar.host.url="$HOST" /d:sonar.login="$TOKEN" /d:sonar.verbose=true

echo "Building solution"
dotnet build FocusTimer.sln -c Release

echo "Ending SonarScanner (MSBuild)"
dotnet sonarscanner end /d:sonar.login="$TOKEN"
