# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps/vitarish-landing/package.json ./apps/vitarish-landing/
COPY apps/knowledge-qa-demo/package.json ./apps/knowledge-qa-demo/
COPY apps/private-practice-owner/package.json ./apps/private-practice-owner/
COPY shared/account-web/package.json ./shared/account-web/
COPY shared/auth-web/package.json ./shared/auth-web/
COPY shared/clinic-web/package.json ./shared/clinic-web/
COPY shared/communications-web/package.json ./shared/communications-web/
COPY shared/journey-web/package.json ./shared/journey-web/
COPY shared/knowledge-web/package.json ./shared/knowledge-web/
COPY shared/platform-web/package.json ./shared/platform-web/
COPY shared/sea-web/package.json ./shared/sea-web/
COPY shared/web-api-client/package.json ./shared/web-api-client/
COPY shared/web-localization/package.json ./shared/web-localization/
COPY shared/web-ui/package.json ./shared/web-ui/
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true --mount=type=cache,target=/root/.npm HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN --mount=type=cache,id=vitarish-landing-next-cache,target=/app/apps/vitarish-landing/.next/cache npm run build:vitarish-landing

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3002
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-landing/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-landing/public ./apps/vitarish-landing/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-landing/.next/static ./apps/vitarish-landing/.next/static
WORKDIR /app/apps/vitarish-landing
USER nextjs
EXPOSE 3002
CMD ["node", "server.js"]
