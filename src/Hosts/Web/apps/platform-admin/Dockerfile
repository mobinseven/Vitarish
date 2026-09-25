# syntax=docker/dockerfile:1.7
FROM node:22-alpine AS base
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps ./apps
COPY shared ./shared
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true --mount=type=cache,target=/root/.npm HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" npm ci
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL=https://localhost:7272
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1
RUN --mount=type=cache,id=vitarish-platform-admin-next-cache,target=/app/apps/platform-admin/.next/cache npm run build:platform-admin
FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3003
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder --chown=nextjs:nodejs /app/apps/platform-admin/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/platform-admin/public ./apps/platform-admin/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/platform-admin/.next/static ./apps/platform-admin/.next/static
WORKDIR /app/apps/platform-admin
USER nextjs
EXPOSE 3003
CMD ["node", "server.js"]
