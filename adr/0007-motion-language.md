# 0007 — Motion: confirm, never entertain

## Status

Accepted.

## Context

V1 has a consistent motion base: 110–220ms cubic ease-out, opacity and
transform only, out faster than in. V2 adds a small vocabulary of expressive
moments — a shimmer across a freshly parsed chip, a settle on a completed
row — without opening the door to decoration.

## Decision

**The base rules stand.** Nothing over 220ms, nothing blocking input,
opacity/transform only (the two sanctioned layout exceptions — sidebar width,
suggestion expansion — remain the only two).

**The expressive vocabulary is closed.** Exactly these, each used at the
moment it names:

| Moment                          | Motion                                        |
| ------------------------------- | --------------------------------------------- |
| A parse chip appears            | 140ms fade/rise + one 400ms shimmer sweep     |
| An item is completed            | check pop (existing) + row settles 120ms      |
| An item is created from capture | destination row fades in with 160ms rise      |
| A destructive action arms       | icon tint crossfade 120ms, no shake, no pulse |

A shimmer is a single gradient sweep across text or chip, once, never
looping. Anything that loops, bounces, or plays without a triggering user
action is banned.

**Reduced motion is honoured.** When Windows' "show animations" setting is
off (`SystemParameters.ClientAreaAnimation`), durations drop to zero and the
shimmer never runs; state changes remain fully legible because motion never
carries information alone (ADR [0003](0003-accessibility-standard.md)).

**One implementation.** Motion lives in the kit as named helpers
(`Motion.Shimmer`, `Motion.Rise`, …); pages never hand-roll storyboards for
these moments.

## Consequences

New expressive motion requires editing this ADR's table — a deliberate
speed bump. Centralizing in the kit makes the reduced-motion switch one
implementation point instead of a per-page audit.
