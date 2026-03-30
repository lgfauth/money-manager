#!/bin/sh
set -e

# Substitute API_URL from Railway environment variable into static files
if [ -z "$API_URL" ]; then
    echo "WARNING: API_URL not set, placeholders will not be replaced"
else
    echo "Configuring Blazor with API_URL: $API_URL"

    if [ -f /usr/share/nginx/html/index.html ]; then
        sed -i "s|__API_URL__|$API_URL|g" /usr/share/nginx/html/index.html
    fi

    if [ -f /usr/share/nginx/html/appsettings.Production.json ]; then
        sed -i "s|#{API_URL}#|$API_URL|g" /usr/share/nginx/html/appsettings.Production.json
    fi
fi

echo "Starting nginx..."

# Start nginx in foreground
exec nginx -g 'daemon off;'
