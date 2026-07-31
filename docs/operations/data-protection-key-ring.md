# Data Protection key-ring continuity

Main persists ASP.NET Core Data Protection keys in Redis and protects them with the certificate stored in Redis. The host requires explicit Redis key names at startup; it does not fall back to the framework's ephemeral key repository.

Every production instance must use the same Redis database and the same `Vitarish:Identity:DataProtection` key names. Back up the database containing both the Data Protection key-ring value and the certificate value together with the Intelligence database. Restore those values before restoring encrypted conversation history. A database backup without the matching key ring cannot decrypt existing history.

Rotate the protecting certificate only through a planned overlap: retain the previous certificate/key-ring backup until all instances have loaded the new material and a restart/decryption check has passed. Never log key material, certificate bytes, ciphertext, or prompts.

Redis connectivity or missing key-name configuration fails host startup. Decryption failures should be alerted using the redacted application error category; recovery is to restore the matching Redis key-ring/certificate backup and restart the host. Do not delete encrypted conversation rows as a recovery shortcut.
