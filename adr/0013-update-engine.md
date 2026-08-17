# 0013 — Alfred.Update: a lighter, precise updater

## Status

Accepted.

## Context

The update wizard lives inside `Alfred.App` as seven files that grew
organically: version check, release parsing, download, install, orchestration.
It works, but it is app-entangled, hard to test without a UI, and does more
ceremony than an updater needs.

## Decision

Updates move to a standalone library, `Alfred.Update`, rewritten around one
narrow pipeline with each step testable in isolation:

1. **Check** — query the GitHub releases API for the latest tag, compare
   against the running semantic version, honouring a minimum interval so
   startup never waits on the network.
2. **Fetch** — download the win-x64 zip to a staging directory with progress
   reporting and cancellation; verify the archive opens and the expected
   entry exists before calling it fetched. A torn download is deleted, never
   retried into.
3. **Apply** — stage the new executable beside the current one and swap on
   next launch; the previous version is kept for one cycle as an automatic
   fallback.

Design rules: no state machine beyond these three steps; every network and
filesystem boundary behind a small interface so the pipeline tests run
without touching either; all user-facing text via ADR
[0012](0012-localization.md); the UI in Settings reduces to one row —
current version, one action, one status line that always names the step it
is on ("Downloading 4.2 MB of 18 MB…"). Precise means the status never lies
and never says "Please wait".

## Consequences

`Alfred.App` keeps only the settings row and a call into the library.
Update logic gains real tests for version comparison, interval logic and
failure paths — the code least exercised in development is the code that
most needs them. The kept-previous-version fallback costs one build's disk
space and buys survivable bad releases.
