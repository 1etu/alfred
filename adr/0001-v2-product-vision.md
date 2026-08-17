# 0001 — V2 vision: a product, one page at a time

## Status

Accepted.

## Context

Alfred started as a personal weekend project. It works, but it was built for
one person who forgives it everything. V2 is the version a stranger installs,
understands in a minute, and trusts with their life admin. The reference bar
is the best personal software on any platform — apps where restraint, motion
and clarity make organizing feel effortless — built with native Windows
mechanisms.

## Decision

- Every page is redesigned by hand, one at a time, iterated until finished.
  A page in flight may look better than its neighbours; a finished page is
  actually finished — both themes, all DPI scales, keyboard paths, empty
  states, accessibility.
- The foundation came first and stays load-bearing: `Alfred.Theme` owns every
  colour, `Alfred.UIKit` owns every reusable control and style. Pages compose
  the kit; a page that needs a new control adds it to the kit.
- V2 grows the product surface deliberately: folders for todos, a Notes
  domain, payment subcategories, a real calendar, localization, a rebuilt
  updater. The inventory lives in [features.md](features.md); the order lives
  in [phases.md](phases.md).
- Work ships to `main` as a stream of small commits — the whole V2 arc is
  budgeted at roughly 100–150 commits, each one buildable and green.

## Consequences

The app will be visibly asymmetric for a while: finished pages next to empty
placeholders that say so honestly. That is accepted — an honest empty page
beats a half-designed one. Scope is fixed by the phase list, not by mood;
new ideas enter through a new ADR, not a detour.
