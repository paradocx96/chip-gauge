# Security Policy

## Reporting a vulnerability

Please **do not** open a public issue for security vulnerabilities.

Instead, report it privately through GitHub's
[private vulnerability reporting](https://github.com/paradocx96/system-app/security/advisories/new)
(Security → Report a vulnerability), or by contacting the maintainers directly.

Include:

- a description of the issue and its impact,
- steps to reproduce or a proof of concept,
- affected version / commit, and
- any suggested remediation.

We aim to acknowledge reports within a few business days and will keep you
updated as we investigate and prepare a fix. Please give us a reasonable window
to remediate before any public disclosure.

## Scope & context

ChipGauge reads low-level hardware sensors, which on Windows requires elevated
privileges and a signed kernel driver (via LibreHardwareMonitor). Reports about
privilege handling, the elevation prompt, update integrity, or the driver
interaction surface are especially valuable.

## Supported versions

The project is pre-release (Phase 0). Until a `1.0` release, only the latest
`main` is supported for security fixes.
