#!/bin/sh
# Runtime replacement of NEXT_PUBLIC_API_URL placeholder
# If API_URL env var is set at runtime, replace the build-time placeholder
# in all JS files with the actual value.
if [ -n "$API_URL" ]; then
  echo "Replacing API URL placeholder with: $API_URL"
  find /app -name '*.js' -exec sed -i "s|__NEXT_PUBLIC_API_URL_PLACEHOLDER__|$API_URL|g" {} +
fi

exec node server.js
