#!/bin/sh
# Runtime replacement of NEXT_PUBLIC_ADMIN_API_URL placeholder.
# If ADMIN_API_URL env var is set at runtime, replace the build-time placeholder
# in all JS files with the actual value.
if [ -n "$ADMIN_API_URL" ]; then
  echo "Replacing Admin API URL placeholder with: $ADMIN_API_URL"
  find /app -name '*.js' -exec sed -i "s|__NEXT_PUBLIC_ADMIN_API_URL_PLACEHOLDER__|$ADMIN_API_URL|g" {} +
fi

exec node server.js
