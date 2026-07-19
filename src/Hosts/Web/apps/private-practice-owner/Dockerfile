# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base

# --- Dependencies ---
FROM base AS deps
WORKDIR /app

# Copy workspace root package files
COPY package.json package-lock.json ./
# Copy every workspace manifest so npm can install and link local packages
COPY apps/knowledge-qa-demo/package.json ./apps/knowledge-qa-demo/
COPY apps/private-practice-owner/package.json ./apps/private-practice-owner/
COPY shared/account-web/package.json ./shared/account-web/
COPY shared/auth-web/package.json ./shared/auth-web/
COPY shared/clinic-web/package.json ./shared/clinic-web/
COPY shared/journey-web/package.json ./shared/journey-web/
COPY shared/knowledge-web/package.json ./shared/knowledge-web/
COPY shared/platform-web/package.json ./shared/platform-web/
COPY shared/sea-web/package.json ./shared/sea-web/
COPY shared/web-api-client/package.json ./shared/web-api-client/
COPY shared/web-localization/package.json ./shared/web-localization/
COPY shared/web-ui/package.json ./shared/web-ui/
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true \
    --mount=type=cache,target=/root/.npm \
    HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" \
    npm ci

# --- Build ---
FROM base AS builder
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

ARG NEXT_PUBLIC_API_BASE_URL=https://localhost:7272
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1

RUN --mount=type=cache,id=vitarish-private-practice-owner-next-cache,target=/app/apps/private-practice-owner/.next/cache \
    npm run build:private-practice-owner

# --- Production ---
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /app/apps/private-practice-owner/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/private-practice-owner/public ./apps/private-practice-owner/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/private-practice-owner/.next/static ./apps/private-practice-owner/.next/static

WORKDIR /app/apps/private-practice-owner

USER nextjs

EXPOSE 3000

CMD ["node", "server.js"]
