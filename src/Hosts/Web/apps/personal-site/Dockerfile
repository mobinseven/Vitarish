# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps/personal-site/package.json ./apps/personal-site/
COPY shared/localization/package.json ./shared/localization/
COPY shared/ui/package.json ./shared/ui/
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true --mount=type=cache,target=/root/.npm HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN --mount=type=cache,id=personal-site-next-cache,target=/app/apps/personal-site/.next/cache npm run build --workspace @vitarish/personal-site

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3010
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder --chown=nextjs:nodejs /app/apps/personal-site/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/personal-site/.next/static ./apps/personal-site/.next/static
WORKDIR /app/apps/personal-site
USER nextjs
EXPOSE 3010
CMD ["node", "server.js"]
