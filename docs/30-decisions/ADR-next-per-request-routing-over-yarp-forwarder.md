# ADR (number pending): Per-request routing on top of YARP's IHttpForwarder

Status: Proposed
Date: 2026-10-22

## Context

The first scaffold used YARP's configuration-driven routes (`ReverseProxy:Routes`). A strangler-fig harness needs
decisions that static routes cannot express: switching a route's mode at runtime from a panel, sending a percentage of
requests to the modern backend, and capturing the legacy response of a shadowed request to compare it afterwards.

## Decision

The gateway keeps YARP but uses its direct forwarding API (`IHttpForwarder.SendAsync`) from a single catch-all
endpoint. `StranglerProxy` resolves the route from `MigrationRouteRegistry`, selects the destination per request and,
in `Shadow` mode, tees the response body and replays the buffered request against the modern backend.

## Alternatives considered

- **YARP config routes + `InMemoryConfigProvider` reloads**: supports runtime mode changes but not canary percentages
  or response capture without custom middleware anyway.
- **Hand-written proxy with `HttpClient`**: loses YARP's streaming, header hygiene and HTTP/2 handling.

## Consequences

- One small matcher (`PathPatternMatcher`) replaces the routing table; patterns support literals, `{param}` and a
  trailing `{**catch-all}`.
- Request bodies of shadowed routes are buffered in memory (bounded by `MaxCapturedBodyBytes`).
- Canary selection is random per request; sticky sessions are a follow-up if a client needs them.
