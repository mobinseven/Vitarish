#!/bin/sh
set -eu

bundle=${CADDY_RSA_CERTIFICATE_BUNDLE:?CADDY_RSA_CERTIFICATE_BUNDLE is required}
hosts=${CADDY_RSA_CERTIFICATE_HOSTS:?CADDY_RSA_CERTIFICATE_HOSTS is required}

fail() {
    printf 'RSA TLS certificate validation failed: %s\n' "$1" >&2
    exit 1
}

[ -f "$bundle" ] && [ ! -L "$bundle" ] && [ -s "$bundle" ] || fail 'bundle is missing or unsafe'
command -v openssl >/dev/null 2>&1 || fail 'OpenSSL is unavailable'
command -v awk >/dev/null 2>&1 || fail 'awk is unavailable'

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT HUP INT TERM

awk -v directory="$work" '
    /-----BEGIN CERTIFICATE-----/ {
        count++
        current = sprintf("%s/certificate-%d.pem", directory, count)
        inside = 1
    }
    inside { print > current }
    /-----END CERTIFICATE-----/ {
        close(current)
        inside = 0
    }
    END { print count + 0 }
' "$bundle" > "$work/count"
certificate_count=$(cat "$work/count")
[ "$certificate_count" -ge 2 ] || fail 'bundle must contain the server certificate and at least one CA certificate'

awk -v directory="$work" '
    /-----BEGIN (RSA )?PRIVATE KEY-----/ {
        count++
        current = sprintf("%s/private-key-%d.pem", directory, count)
        inside = 1
    }
    inside { print > current }
    /-----END (RSA )?PRIVATE KEY-----/ {
        close(current)
        inside = 0
    }
    END { print count + 0 }
' "$bundle" > "$work/key-count"
[ "$(cat "$work/key-count")" -eq 1 ] || fail 'bundle must contain exactly one unencrypted private key'

leaf="$work/certificate-1.pem"
intermediates="$work/intermediates.pem"
private_key="$work/private-key-1.pem"
: > "$intermediates"
index=2
while [ "$index" -le "$certificate_count" ]; do
    certificate="$work/certificate-$index.pem"
    openssl x509 -in "$certificate" -noout >/dev/null 2>&1 || fail 'bundle contains an invalid certificate'
    constraints=$(openssl x509 -in "$certificate" -noout -ext basicConstraints 2>/dev/null) || fail 'CA basicConstraints extension is missing'
    printf '%s\n' "$constraints" | grep -Fq 'CA:TRUE' || fail 'a certificate in the CA chain does not have basicConstraints CA:TRUE'
    cat "$certificate" >> "$intermediates"
    index=$((index + 1))
done
openssl x509 -in "$leaf" -noout >/dev/null 2>&1 || fail 'server certificate is invalid'

extended_key_usage=$(openssl x509 -in "$leaf" -noout -ext extendedKeyUsage 2>/dev/null) || fail 'server certificate has no extendedKeyUsage extension'
printf '%s\n' "$extended_key_usage" | grep -Fq 'TLS Web Server Authentication' || fail 'server certificate lacks the Server Authentication EKU'

public_key=$(openssl x509 -in "$leaf" -noout -pubkey 2>/dev/null | openssl pkey -pubin -text -noout 2>/dev/null) || fail 'server public key is invalid'
printf '%s\n' "$public_key" | grep -Fq 'Public-Key: (2048 bit)' || fail 'RSA server certificate must use a 2048-bit key'
openssl x509 -in "$leaf" -noout -pubkey 2>/dev/null | openssl pkey -pubin -outform DER 2>/dev/null > "$work/certificate-public-key.der" || fail 'server public key cannot be exported'
openssl pkey -in "$private_key" -pubout -outform DER 2>/dev/null > "$work/private-public-key.der" || fail 'server private key is invalid'
cmp -s "$work/certificate-public-key.der" "$work/private-public-key.der" || fail 'server certificate and private key do not match'

printf '%s\n' "$hosts" | tr ',' '\n' > "$work/hosts"
first_host=
while IFS= read -r configured_host; do
    [ -n "$configured_host" ] || continue
    host=$configured_host
    case "$host" in
        \*.*) host="certificate-validation.${host#*.}" ;;
        *\**) fail 'configured host pattern is invalid' ;;
    esac
    case "$host" in
        *[!A-Za-z0-9.-]*|'') fail 'configured host name is invalid' ;;
    esac
    [ -n "$first_host" ] || first_host=$host
    openssl verify -trusted_first -x509_strict -purpose sslserver \
        -verify_hostname "$host" -CApath /etc/ssl/certs \
        -untrusted "$intermediates" "$leaf" >/dev/null 2>&1 || fail "certificate path or hostname validation failed for $configured_host"
done < "$work/hosts"
[ -n "$first_host" ] || fail 'no configured HTTPS hosts were supplied'

path_result=$(openssl verify -trusted_first -x509_strict -purpose sslserver \
    -verify_hostname "$first_host" -show_chain -CApath /etc/ssl/certs \
    -untrusted "$intermediates" "$leaf" 2>&1) || fail 'certificate path does not terminate at a trusted CA'
path_length=$(printf '%s\n' "$path_result" | awk '/^depth=/ { count++ } END { print count + 0 }')
[ "$path_length" -ge 2 ] || fail 'validated certification path contains fewer than two certificates'

index=1
while [ "$index" -le "$certificate_count" ]; do
    certificate="$work/certificate-$index.pem"
    subject=$(openssl x509 -in "$certificate" -noout -subject -nameopt RFC2253 2>/dev/null) || fail 'certificate subject cannot be read'
    issuer=$(openssl x509 -in "$certificate" -noout -issuer -nameopt RFC2253 2>/dev/null) || fail 'certificate issuer cannot be read'
    if [ "${subject#subject=}" != "${issuer#issuer=}" ]; then
        remaining="$work/remaining-$index.pem"
        : > "$remaining"
        next=$((index + 1))
        while [ "$next" -le "$certificate_count" ]; do
            cat "$work/certificate-$next.pem" >> "$remaining"
            next=$((next + 1))
        done
        if [ -s "$remaining" ]; then
            openssl verify -trusted_first -x509_strict -purpose any \
                -crl_download -crl_check -CApath /etc/ssl/certs \
                -untrusted "$remaining" "$certificate" >/dev/null 2>&1 || fail 'certificate or CA revocation status could not be established from a valid CRL'
        else
            openssl verify -trusted_first -x509_strict -purpose any \
                -crl_download -crl_check -CApath /etc/ssl/certs \
                "$certificate" >/dev/null 2>&1 || fail 'certificate or CA revocation status could not be established from a valid CRL'
        fi
    fi
    index=$((index + 1))
done

printf 'RSA TLS certificate bundle passed path, hostname, CA constraints, Server Authentication EKU, and CRL checks.\n'
