#!/bin/sh
# Wait for the database to accept connections, apply EF Core migrations,
# then start the PathlyAI API.

cd /src

echo "Applying database migrations..."
attempt=1
until dotnet ef database update \
        --project "Pathly Data/Pathly Data.csproj" \
        --startup-project "PathlyAI_API/PathlyAI_API.csproj"
do
    attempt=$((attempt + 1))
    if [ "$attempt" -gt 30 ]; then
        echo "Migrations failed after 30 attempts."
        exit 1
    fi
    echo "Migration attempt $attempt failed, retrying in 5s..."
    sleep 5
done

echo "Migrations applied. Starting PathlyAI API..."
exec dotnet /app/PathlyAI_API.dll
