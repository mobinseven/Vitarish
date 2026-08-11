# syntax=docker/dockerfile:1.7
FROM node:22-alpine AS base
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps/vitarish-pm/package.json ./apps/vitarish-pm/
COPY shared/auth-web/package.json ./shared/auth-web/
COPY shared/intelligence-web/package.json ./shared/intelligence-web/
COPY shared/sea-web/package.json ./shared/sea-web/
COPY shared/web-api-client/package.json ./shared/web-api-client/
COPY shared/web-localization/package.json ./shared/web-localization/
COPY shared/web-mock-runtime/package.json ./shared/web-mock-runtime/
COPY shared/web-ui/package.json ./shared/web-ui/
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true --mount=type=cache,target=/root/.npm HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" npm ci
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL=https://localhost:7272
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1
RUN --mount=type=cache,id=vitarish-pm-next-cache,target=/app/apps/vitarish-pm/.next/cache npm run build --workspace @vitarish/vitarish-pm
FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3004
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-pm/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-pm/public ./apps/vitarish-pm/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/vitarish-pm/.next/static ./apps/vitarish-pm/.next/static
WORKDIR /app/apps/vitarish-pm
USER nextjs
EXPOSE 3004
CMD ["node", "server.js"]
