# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base

# --- Dependencies ---
FROM base AS deps
WORKDIR /app

# Copy workspace root package files
COPY package.json package-lock.json ./
# Copy app package.json for workspace resolution
COPY apps/knowledge-qa-demo/package.json ./apps/knowledge-qa-demo/
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

RUN --mount=type=cache,target=/app/.next/cache \
    npm run build:knowledge-qa-demo

# --- Production ---
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3001
ENV HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /app/apps/knowledge-qa-demo/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/apps/knowledge-qa-demo/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/knowledge-qa-demo/.next/static ./.next/static

USER nextjs

EXPOSE 3001

CMD ["node", "server.js"]
