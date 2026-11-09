# Security Policy

## Supported versions

Security fixes are provided for the latest release on the `main` branch.

## Reporting a vulnerability

Please do not open a public issue for security problems.

Use GitHub's private vulnerability reporting:

1. Open the repository's **Security** tab.
2. Choose **Report a vulnerability**.
3. Include reproduction steps, affected versions, and any relevant logs.

You will receive a response as soon as reasonably possible.

## Update integrity

Quick Theme's updater downloads a single-file executable and verifies its SHA-256
hash against the update manifest before installing it. The manifest itself should
be served over HTTPS. Releases are built by GitHub Actions from this repository.

The executables are not code-signed yet, so Windows SmartScreen may show a warning.
You can verify a release asset against the `sha256` value in `update.json`.
