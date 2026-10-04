# Caddy RSA certificate renewal

## Ownership and schedule

The production deployment operator runs this procedure weekly. The certificate
operator uses `lego run` with a 30-day renewal window. It leaves the current
certificate in place when renewal is not yet due. Run only one certificate
operation at a time; the ACME account state and active bundle are shared.

The operator container receives the Cloudflare token from the root-owned,
mode-600 `.env.shared` file. Never put the token, ACME account key, certificate
private key, or bundle contents in a command argument, terminal transcript,
ticket, or repository file.

## First issuance

The production deploy script checks for the RSA bundle before validating Caddy.
If it is absent, the script runs the certificate operator first. Issuance creates
a pending bundle; the certificate validator checks it before promotion. A DNS,
token, ACME, trust-path, hostname, EKU, CA-constraint, or CRL failure stops that
release. On a new host, ingress remains stopped. On an existing host, Caddy
keeps serving the previously loaded certificate. Fix the cause and retry the
release. The ACME account and certificate volumes are persistent and should
not be deleted during recovery.

## Renewal

On the production host:

```sh
cd /opt/vitarish/deployment
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile certificate-operations run --interactive=false --rm --no-deps rsa-certificate-renewal
```

The command asks the ACME provider to renew when 30 or fewer days remain. It
writes the resulting certificate chain and private key to a pending PEM bundle;
it does not replace the active certificate.

Validate the pending certificate before promotion. The validator checks every
configured test and production hostname, the trusted RFC 5280 path (including
CA `basicConstraints`), the Server Authentication EKU, a path length of at
least two certificates, and revocation through signed CRLs. It fails closed if
it cannot obtain or validate a current CRL.

```sh
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile validation run --interactive=false --rm --no-deps --env CADDY_RSA_CERTIFICATE_BUNDLE=/var/lib/caddy-rsa-staging/rsa.pending.pem caddy-certificate-validator validate-certificate
```

If validation succeeds, promote the pending bundle. Promotion atomically replaces
the active bundle and saves its previous state for rollback.

```sh
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile certificate-operations run --interactive=false --rm --no-deps --env CADDY_RSA_CERTIFICATE_OPERATION=promote rsa-certificate-renewal
```

Validate the Caddy configuration with the offline configuration validator,
then reload Caddy:

```sh
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile validation run --interactive=false --rm --no-deps --entrypoint /usr/bin/caddy caddy-config-validator validate --config /etc/caddy/Caddyfile --adapter caddyfile
docker compose --env-file .env.shared -f docker-compose.ingress.yml exec -T caddy caddy reload --config /etc/caddy/Caddyfile --adapter caddyfile
```

If pending-certificate validation fails, the active bundle has not changed.
Discard the pending candidate after recording the failure:

```sh
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile certificate-operations run --interactive=false --rm --no-deps --env CADDY_RSA_CERTIFICATE_OPERATION=discard rsa-certificate-renewal
```

Verify the production public host negotiates an RSA-authenticated TLS 1.2
cipher. This command prints handshake details, not certificate private
material:

```sh
host=$(sed -n 's/^VITARISH_PRODUCTION_PUBLIC_HOST=//p' .env.shared)
openssl s_client -connect "${host}:443" -servername "$host" -tls1_2 -cipher ECDHE-RSA-AES128-GCM-SHA256 -verify_hostname "$host" -verify_return_error -brief </dev/null
openssl s_client -connect "${host}:443" -servername "$host" -tls1_2 -cipher ECDHE-RSA-AES128-GCM-SHA256 -showcerts </dev/null 2>/dev/null | openssl x509 -noout -enddate
unset host
```

Confirm the negotiated cipher is `ECDHE-RSA-AES128-GCM-SHA256` and certificate
verification succeeds. Repeat with a representative hostname from each DNS
pattern if the release evidence needs to cover all configured host classes.

## Recovery

If Caddy configuration validation or reload fails after promotion, restore the
previous bundle with the certificate operator image. For first issuance, restore
removes the newly promoted bundle because there was no previous active bundle:

```sh
docker compose --env-file .env.shared -f docker-compose.ingress.yml --profile certificate-operations run --interactive=false --rm --no-deps --env CADDY_RSA_CERTIFICATE_OPERATION=restore rsa-certificate-renewal
```

Then run certificate validation, validate the Caddy configuration, and reload
the restored bundle. If first issuance failed before promotion, no restore is
needed. Keep Caddy stopped on a new host until a valid bundle is available.
Correct DNS or token scope, then retry issuance. Do not remove the ACME state,
certificate, or backup volumes to recover from an issuance error.

If reload fails after validation, inspect Caddy's service logs. Restore the
previous bundle and retry validation and reload. Restart Caddy only after the
restored bundle passes validation.

## Operational evidence

Record the operation time, release hostname, certificate expiry, validation
result, reload result, and observed RSA TLS 1.2 cipher in the deployment
record. Do not attach the `.env.shared` file or any PEM data. A Compose render
proves repository configuration only; ACME issuance and the external handshake
must be observed on the production host.
