#!/bin/sh
set -e

# Get API URL from environment or use default
API_URL=${API_URL:-https://localhost:5001}

echo "========================================"
echo "Configuring Blazor WebAssembly"
echo "API_URL: $API_URL"
echo "========================================"

# Replace placeholder in index.html
if [ -f /usr/share/nginx/html/index.html ]; then
    sed -i "s|__API_URL__|$API_URL|g" /usr/share/nginx/html/index.html
    echo "? Updated index.html with API URL"
else
    echo "? Warning: index.html not found"
fi

# Replace placeholder in appsettings.Production.json
if [ -f /usr/share/nginx/html/appsettings.Production.json ]; then
    sed -i "s|#{API_URL}#|$API_URL|g" /usr/share/nginx/html/appsettings.Production.json
    echo "? Updated appsettings.Production.json"
fi

echo "========================================"
echo "Starting nginx..."
echo "========================================"

# Start nginx in foreground
exec nginx -g 'daemon off;'
