# Evaluated single-machine topology

```mermaid
flowchart LR
    B[Browser / operator client\nEnvironmental user agent]
    X[TLS reverse proxy\nEnvironmental boundary]
    M[Main container\nMain.dll\nEvaluated build profile]
    P[(PostgreSQL 18.0\nEnvironmental state service)]
    R[(Redis 7.4.5\nKeys and cache\nEnvironmental state service)]
    O[Operator / release process\nHost and container boundary]

    B -->|HTTPS, cookies, bearer tokens, JSON| X
    X -->|Private HTTP upstream| M
    M -->|EF/Npgsql, audit, Hangfire| P
    M -->|Data Protection and cache| R
    O -->|image, profile, secrets, lifecycle| M
    O -->|volumes, ACLs, backups| P
    O -->|volume, ACLs, backup| R
```

The three runtime containers share one private network. Only Main is published to the host (`127.0.0.1:5000` in the reference compose file); PostgreSQL and Redis have no host port. A TLS reverse proxy is required for the browser boundary because Main enforces secure cookies and antiforgery. It is environmental and its certificate, listener, upstream, and ACL configuration must be recorded in release evidence.

The evaluated topology has no Web container, agent-runtime, Elasticsearch, MinIO AIStor, SMTP, SMS, AI, Tamin, payment, social, or other provider container.
