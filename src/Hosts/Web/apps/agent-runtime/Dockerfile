# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
COPY apps/agent-runtime/package.json ./apps/agent-runtime/
RUN --mount=type=cache,target=/root/.npm npm ci --workspace @vitarish/agent-runtime --include-workspace-root=false

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY package.json package-lock.json ./
COPY apps/agent-runtime ./apps/agent-runtime
RUN npm run build --workspace @vitarish/agent-runtime

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3003
ENV HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /app/apps/agent-runtime/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/agent-runtime/.next/static ./apps/agent-runtime/.next/static

WORKDIR /app/apps/agent-runtime
USER nextjs

EXPOSE 3003

CMD ["node", "server.js"]
