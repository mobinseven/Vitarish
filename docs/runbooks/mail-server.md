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

## Open items

- Reverse DNS: the host's PTR record belongs to the hosting provider and is
  not `mail.vitarish.com`. Ask the provider to change it before relying on
  delivery to large providers.
- Outgoing mail to external providers is not yet verified end to end. The
  public `verifier.port25.com` checker has an expired certificate and cannot
  be used.

## Rollback

`docker compose --env-file .env.shared -f docker-compose.mail.yml down` stops
the server and keeps the volumes. Reverting DNS means restoring the previous
`MX` and removing the `v1-*` DKIM and SPF records in Cloudflare.
