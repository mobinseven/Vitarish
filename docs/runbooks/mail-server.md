# Mail Server Runbook

Stalwart serves `@vitarish.com` mail (smoke mailboxes, see
[Production smoke accounts](production-smoke-accounts.md)). It runs on the
production host as its own Compose project, `vitarish-mail`, from
`src/Hosts/deployment/docker-compose.mail.yml`. The release workflow does not
deploy it: apply it by hand. The application's own outgoing mail is unrelated
and uses the SMTP settings in `.env.production`.

## What runs

- Image `stalwartlabs/stalwart`, pinned by digest, capped at 384 MB.
- Public ports: 25 (SMTP), 465 (authenticated submission), 993 (IMAP).
  Caddy owns 443, so Stalwart's own HTTPS listener and its WebUI are not
  published.
- Administration listener: `127.0.0.1:8080` on the host, plain HTTP. Never
  publish it; reach it only from the host or through an SSH tunnel.
- Public exposure is a validated security setting. The post-deploy port scan in
  `src/Hosts/Web/scripts/production-evidence-checks.mjs` expects exactly
  22, 25, 80, 443, 465 and 993 to be open and every other scanned port to be
  closed, so a release run fails if a mail port closes or the administration or
  a legacy mail port (110, 143, 8080) opens. The owner approved publishing 25,
  465 and 993 on 2026-11-07; change both the Compose file and that list
  together.
- Volumes `vitarish-mail-config` (only `config.json`, the datastore pointer)
  and `vitarish-mail-data`. Everything else lives in the datastore.
- Certificate: wildcard `*.vitarish.com` from Let's Encrypt, obtained and
  renewed by Stalwart through the Cloudflare DNS challenge. No inbound port is
  needed.

## Protected files and secrets

On the host under `/opt/vitarish/deployment`, owned by root, mode `600`:

- `.env.mail`: `STALWART_ADMIN_PASSWORD` (the `admin@vitarish.com` account) and
  one password per mailbox, for example
  `STALWART_SMOKE_NO_WORKSPACE_PASSWORD`. The Compose file loads it as the
  container environment when present.
- `.env.shared`: `VITARISH_CLOUDFLARE_API_TOKEN` stays owned here. Compose
  passes it to the container as `STALWART_CLOUDFLARE_API_TOKEN`, and Stalwart's
  Cloudflare DNS provider reads it from that environment variable. The token
  needs Zone read and DNS edit on the whole zone, not only `_acme-challenge`,
  because Stalwart also publishes the mail records.

Never print these files. Do not set `STALWART_RECOVERY_ADMIN` except for a
short bootstrap or recovery: while set, the server accepts that credential on
every start.

## Apply or update

The host's Docker Engine must unpack zstd image layers. Docker 20.10 cannot
(`invalid tar header`); the host runs 28.x.

```bash
cd /opt/vitarish/deployment
docker compose --env-file .env.shared -f docker-compose.mail.yml up -d
```

To update Stalwart, change the pinned digest in the Compose file through a pull
request, copy the file to the host and run the same command. Read the
upstream upgrade notes first: the v0.16 upgrade discarded stored settings.

## Administer

Settings are objects in the datastore, not a config file. Use the CLI image
against the loopback listener, with the admin password read from `.env.mail`
(never typed into a shell history or pasted into a chat):

```bash
set -a; . /opt/vitarish/deployment/.env.mail; set +a
docker run --rm -i --network host \
  -e STALWART_URL=http://127.0.0.1:8080 \
  -e STALWART_USER=admin@vitarish.com \
  -e STALWART_PASSWORD="$STALWART_ADMIN_PASSWORD" \
  ghcr.io/stalwartlabs/cli:latest query Account
```

`describe` lists objects and fields. Create commands print server-generated
secrets, so redirect their output. Pass payloads containing a password with
`--stdin` so they stay out of the process list.

New mailbox: create an `Account/User` with `name` (the local part), `domainId`
(from `query Domain`) and a `Password` credential, then store the password in
`.env.mail`.

Read a mailbox without the WebUI: IMAP over `127.0.0.1:993` with that
mailbox's address and password. The certificate is for the public hostname, so
skip hostname verification for the loopback connection.

## DNS

Stalwart publishes `MX`, `SPF` and `DKIM` for `vitarish.com` through the
Cloudflare provider and rotates DKIM keys itself. Its MTA-STS, autoconfig,
`SRV`, `CAA`, `TLSA` and DMARC records are switched off on the domain:
Caddy owns 443, so Stalwart could not serve the MTA-STS policy or autoconfig.
`_dmarc` is a manual record, currently `p=none`; move it to `quarantine` and
then `reject` after outgoing mail has passed for a while. `mail.vitarish.com`
is an `A` record, not a CNAME, because an MX target must not be a CNAME.

## Outgoing mail

The host's address is on a Spamhaus list, and Outlook refuses mail sent from it
directly (`550 5.7.1 ... blocked using Spamhaus`). All mail for other domains
therefore leaves through a relay: the `resend` route of the outbound strategy,
a relay to `smtp.resend.com:465` (implicit TLS, user `resend`). The password is
the Resend API key, read from the container environment variable
`STALWART_RESEND_API_KEY` in `.env.mail`; the key only needs sending access.
Mail for `vitarish.com` is still delivered locally. `vitarish.com` is a verified
sending domain in Resend, which is required for it to accept the sender.

The application sends through Stalwart, not the relay directly: its
`VITARISH_PRODUCTION_SMTP_*` settings point at `mail.vitarish.com:465` with the
`no-reply@vitarish.com` account, because the application uses the SMTP user name
as the sender address and Resend's user name is not an address.

Stalwart does not pick up a changed route or resolver until it restarts.

## The host resolver is unreliable

The host's own DNS resolver returns a fake address (`125.125.125.125`) for some
domains, for example `live.com` and `smtp.resend.com`. Stalwart is therefore
configured with Cloudflare's public resolver instead of the system one. Other
containers and host tools still use the system resolver, so a lookup that fails
on the host should be checked against `1.1.1.1` before suspecting the target.
Mailgun also drops connections from this host's address range entirely, so it
cannot be used from here.

## Open items

- Reverse DNS: the host's PTR record belongs to the hosting provider and is
  not `mail.vitarish.com`. With the relay this matters only for inbound
  reputation checks, not for delivery of outgoing mail.

## Rollback

`docker compose --env-file .env.shared -f docker-compose.mail.yml down` stops
the server and keeps the volumes. Reverting DNS means restoring the previous
`MX` and removing the `v1-*` DKIM and SPF records in Cloudflare.
