# Data Protection key-ring continuity

Main persists ASP.NET Core Data Protection keys in Redis and protects them with the certificate stored in Redis. The host requires explicit Redis key names at startup; it does not fall back to the framework's ephemeral key repository. The template keeps the legacy `DataProtectionKeys` and `cert:pfx:{domain}` names so existing deployments continue reading their current material.

Every production instance must use the same Redis database and the same `Vitarish:Identity:DataProtection` key names. Back up the database containing both the Data Protection key-ring value and the certificate value together with the Intelligence database. Restore those values before restoring encrypted conversation history. A database backup without the matching key ring cannot decrypt existing history.

For a key-name migration, copy the existing Redis values to the new names, verify both values before rollout, and only then change configuration. Roll back by restoring the old names; never allow a host to start against an empty replacement key.

Rotate the protecting certificate by writing the new PFX under a new Redis key, setting that key as `CertificateRedisKey`, and listing the prior key under `PreviousCertificateRedisKeys`. Deploy all instances, run a restart/decryption check, then remove the previous key from configuration only after every old protected value has been rewrapped or expired. Never log key material, certificate bytes, ciphertext, or prompts.

Redis connectivity or missing key-name configuration fails host startup. Decryption failures should be alerted using the redacted application error category; recovery is to restore the matching Redis key-ring/certificate backup and restart the host. Do not delete encrypted conversation rows as a recovery shortcut.
