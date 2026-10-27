# ADR (number pending): Host the demo on a NainDev subdomain through GitHub Pages

- Status: Proposed
- Date: 2026-10-20

## Context

The demo must be viewable online as a subdomain, like the other NainDev projects, at no cost. Those
projects publish with GitHub Pages and a Cloudflare-proxied subdomain of `naindev.com`.

## Decision

- Deploy `demo/dist` with the existing `pages.yml` workflow to GitHub Pages.
- Serve it at `modernizer.naindev.com`. The domain is set in the repository's Pages settings; a
  Cloudflare DNS record for the subdomain points at `nain9dev.github.io` (proxied).
- No `CNAME` file is committed: with workflow deployment GitHub ignores it.
- All asset paths stay relative, so the page works at the domain root and under a project path.

## Alternatives

- Cloudflare Pages: a second build system, account binding and secrets, with no gain for a static page.
- Project path under the user site (`www.naindev.com/NainLegacyModernizer/`): works today but breaks
  the subdomain convention and disappears once a custom domain is set.

## Consequences

- Setting the Pages domain is a repository setting and the DNS record is a Cloudflare change: both are
  owner actions (task T-012).
- Once the domain is set, the old project path redirects to it.
- The portfolio card links to the subdomain.
