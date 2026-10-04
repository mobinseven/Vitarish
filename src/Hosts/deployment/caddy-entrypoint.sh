#!/bin/sh
set -eu

bundle=${CADDY_RSA_CERTIFICATE_BUNDLE:?CADDY_RSA_CERTIFICATE_BUNDLE is required}
if [ ! -f "$bundle" ] || [ -L "$bundle" ] || [ ! -s "$bundle" ]; then
    printf 'Required RSA TLS certificate bundle is missing or unsafe: %s\n' "$bundle" >&2
    exit 1
fi

if [ "${1:-}" = validate-certificate ]; then
    shift
    exec /usr/local/bin/validate-rsa-certificate "$@"
fi

if [ "${1:-}" = caddy ]; then
    shift
fi

/usr/local/bin/validate-rsa-certificate
exec /usr/bin/caddy "$@"
