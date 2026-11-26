---
description: "Security documentation for Cratis Identity: compromise matrix, recovery runbooks, resource token validation, client secret digests and outage behaviour."
---

# Security

- [Compromise matrix and recovery runbooks v1](compromise-matrix-v1.md) — draft containment boundaries, decision dependencies and per-case recovery.
- [Independent fence log and restore](runbooks/fence-log-restore.md) — packages, provisioning split and the restore procedure that retains the independent log.
- [Resource token validation](resource-token-validation.md) — local JWT checks at resources, edge introspection with a short cache, and the revocation window.
- [Outage behavior](outage-behavior.md) — what keeps working and what is refused when Chronicle, a store or an upstream is down.
- [Personal-data erasure completion boundary](erasure-completion-boundary.md) — outline of where personal data lives, what erasure covers, the proposed workflow and open questions.
