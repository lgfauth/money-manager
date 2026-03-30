#!/bin/sh
set -e

# Inject API_URL from environment variable into appsettings.json at container startup
if [ -n "$API_URL" ]; then
    echo "Configuring Blazor with API_URL: $API_URL"
    printf '{"ApiUrl":"%s"}' "$API_URL" > /usr/share/nginx/html/appsettings.json
else
    echo "WARNING: API_URL not set — Blazor will use HostEnvironment.BaseAddress as fallback"
fi

echo "Starting nginx..."
exec nginx -g 'daemon off;'
