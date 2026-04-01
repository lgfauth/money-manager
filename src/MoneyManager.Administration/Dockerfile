# Build
FROM node:20-alpine AS builder
WORKDIR /app
COPY src/MoneyManager.Administration/package*.json ./
RUN npm ci
COPY src/MoneyManager.Administration/. ./

# Build arg for admin API URL — replaced at runtime via entrypoint if not set at build time
ARG NEXT_PUBLIC_ADMIN_API_URL=__NEXT_PUBLIC_ADMIN_API_URL_PLACEHOLDER__
ENV NEXT_PUBLIC_ADMIN_API_URL=$NEXT_PUBLIC_ADMIN_API_URL

RUN npm run build

# Runtime
FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production

COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

EXPOSE 3000
CMD ["node", "server.js"]
