#!/bin/sh
# Run SonarQube server (in background) and execute scanner container against it.
# Usage: ./scripts/run-sonar.sh [SONAR_TOKEN]

TOKEN=${1:-$SONAR_TOKEN}
if [ -z "$TOKEN" ]; then
  printf "Enter Sonar token: "; read -r TOKEN
fi

docker-compose up -d sonarqube

echo "Waiting for SonarQube to be available at http://localhost:9000..."
until curl -sSf http://localhost:9000 >/dev/null 2>&1; do sleep 2; echo "waiting..."; done

docker-compose run --rm -e SONAR_TOKEN=$TOKEN scanner
