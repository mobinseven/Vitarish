FROM caddy:2.10.0-builder AS builder

RUN xcaddy build v2.10.0 --with github.com/caddy-dns/cloudflare

FROM caddy:2.10.0

RUN apk add --no-cache ca-certificates openssl

COPY --from=builder /usr/bin/caddy /usr/bin/caddy
COPY caddy-entrypoint.sh /usr/local/bin/caddy-entrypoint
COPY validate-rsa-certificate.sh /usr/local/bin/validate-rsa-certificate
RUN chmod 0755 /usr/local/bin/caddy-entrypoint /usr/local/bin/validate-rsa-certificate

ENTRYPOINT ["/usr/local/bin/caddy-entrypoint"]
CMD ["run", "--config", "/etc/caddy/Caddyfile", "--adapter", "caddyfile"]
