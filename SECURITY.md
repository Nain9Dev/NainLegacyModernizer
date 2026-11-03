# Security

## Reporting a vulnerability

Use GitHub's private vulnerability reporting on this repository (Security tab, "Report a vulnerability").
Do not open a public issue for a security problem. Reports are acknowledged within seven days.

## Scope and known constraints

- The control panel (`/_modernizer`) has no authentication: deploy the gateway inside your network boundary
  and never expose port 8080 to the internet.
- Shadow replay sends a copy of each request, including its headers and body, to the modern backend.
  Put a route in `Shadow` only when its handler is idempotent or the owner of that route has signed off.
- Audits keep response bodies in memory up to `MaxCapturedBodyBytes`; treat the gateway process as holding
  whatever data your APIs return.
- `nain-parity` sends `GET` requests only; pass credentials with `--header` from environment variables and
  never commit them.
- The static demo at modernizer.naindev.com loads one external script (the NainDev ecosystem bar) and sends
  nothing: the setup generator runs entirely in the browser.

## Supported versions

The `main` branch. Dependencies are pinned through `Directory.Packages.props` and `demo/package-lock.json`.
