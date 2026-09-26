# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps ./apps
COPY shared ./shared
RUN --mount=type=secret,id=HEROUI_AUTH_TOKEN,required=true \
    --mount=type=cache,target=/root/.npm \
    HEROUI_AUTH_TOKEN="$(cat /run/secrets/HEROUI_AUTH_TOKEN)" npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL=
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build:patients

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3001
ENV HOSTNAME=0.0.0.0
RUN addgroup --system --gid 1001 nodejs && adduser --system --uid 1001 nextjs
COPY --from=builder --chown=nextjs:nodejs /app/apps/patients/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/patients/public ./apps/patients/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/patients/.next/static ./apps/patients/.next/static
WORKDIR /app/apps/patients
USER nextjs
EXPOSE 3001
CMD ["node", "server.js"]
