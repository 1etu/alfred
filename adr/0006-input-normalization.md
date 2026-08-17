# 0006 — Amount and date shorthand

## Status

Accepted.

## Context

People write money the way they say it: "rent 42k", "1.5k coffee budget",
"got 2m from the sale". V1 only accepts fully formatted amounts ("42.000"),
so the fastest users hit the parser's floor first. Dates already parse
naturally; amounts must reach the same bar.

## Decision

- A dedicated parser, `AmountHints`, lives in `Alfred.Core` beside
  `DateHints` and is the single implementation used by capture, page add
  bars, and editors.
- Recognized forms, case-insensitive, at word boundaries:
  - shorthand multipliers: `42k` → 42.000, `1.5k` / `1,5k` → 1.500,
    `2m` → 2.000.000
  - currency-marked: `₺500`, `500tl`, `500 TL`
  - formatted: `42.000`, `42.000,50`, `42,50`
- The parse prefers the trailing position of the text (amounts are usually
  written last), never consumes digits that belong to a parsed date or time,
  and returns both the amount and the exact span consumed so the UI can chip
  it and remove it from the title.
- Normalization is always **echoed** (ADR
  [0004](0004-interaction-clarity.md)): "42k" chips as `₺42.000` the moment
  it parses. The user's shorthand is accepted; the system's reading is shown.
- Every recognized form and every rejected near-miss ("42kk", "k42") is a
  unit test. The test file is the specification.

## Consequences

Locale honesty: V1 formats for Turkish conventions (dot thousands, comma
decimals); `AmountHints` inherits that and gains per-culture behaviour when
localization (ADR [0012](0012-localization.md)) lands. Shorthand introduces
ambiguity ("2m": millions, not minutes) — resolved by kind context: money
kinds read `m` as millions, time-bearing kinds leave it alone.
