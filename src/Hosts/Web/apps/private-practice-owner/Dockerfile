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
COPY shared/account/package.json ./shared/account/
COPY shared/auth/package.json ./shared/auth/
COPY shared/clinic/package.json ./shared/clinic/
COPY shared/communications/package.json ./shared/communications/
COPY shared/journey/package.json ./shared/journey/
COPY shared/knowledge/package.json ./shared/knowledge/
COPY shared/platform/package.json ./shared/platform/
COPY shared/sea/package.json ./shared/sea/
COPY shared/api-client/package.json ./shared/api-client/
COPY shared/localization/package.json ./shared/localization/
COPY shared/ui/package.json ./shared/ui/
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true \
    --mount=type=cache,target=/root/.npm \
    HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" \
    npm ci

# --- Build ---
FROM base AS builder
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

ARG NEXT_PUBLIC_API_BASE_URL=
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
