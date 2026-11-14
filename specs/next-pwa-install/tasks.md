# Tasks: Installable demo

| ID | Tag | Task | Done when |
| :--- | :--- | :--- | :--- |
| T-001 | A | Spec and plan | Files reviewed |
| T-002 | A | Write `demo/test/pwa-install.test.ts` first | Test red: `ENOENT demo/manifest.webmanifest` |
| T-003 | A | Render the icons, add the manifest, the head tags and the build copy; raise the size budget | `npm run typecheck` and `npm test` green |
| T-004 | A | Ask Chrome about installability on `demo/dist` | `installabilityErrors` is empty |
| T-005 | M | After the merge: live manifest answers 200; the owner installs on Android and iPhone | Icon uncropped, label `Modernizer`, no browser bar |

## Session state

- Done: T-001 to T-004.
- Verified: `npm test` 59 pass, `npm run typecheck` clean, `verify-install.mjs` no failure, Chrome no error.
- Not run locally: the Lighthouse gate of `pages.yml` (downloads Lighthouse through `npx`).
- Next: T-005, needs the owner's phones.
