# 0004 — The user never guesses: interpretation is visible

## Status

Accepted.

## Context

V1's quick capture accepts natural language ("rent 42.000 in 3 days") but
gives almost nothing back while typing. The user cannot tell what kind of
item they are creating, whether the amount was recognized, or what date "in 3
days" resolved to — they find out after pressing Enter. Smart input that
hides its understanding is worse than a dumb form: it demands trust it never
earns.

## Decision

A standing interaction rule for every surface that interprets input:

1. **Echo the interpretation live.** As the user types, every recognized
   token becomes a small chip beside the input: the parsed amount
   (`₺42.000`), the resolved date (`in 3 days · Thu, 21 Aug`), the matched
   brand, the inferred kind. Chips appear within one keystroke of the parse.
2. **Chips are removable.** Clicking a chip's dismiss target (or Backspace
   into it) rejects that interpretation and returns the literal text. The
   system proposes; the user disposes.
3. **The commit is previewed.** Before Enter, the surface states plainly what
   will happen ("adds a payment to Payments"), in words, near the primary
   affordance.
4. **Keys are visible.** Surfaces that live on the keyboard show their two or
   three keys as quiet keycaps (Enter saves · Esc closes · Tab completes).
   No memorized invisible contract.
5. **Relative phrases carry resolved values** everywhere, not only in
   capture — list rows, suggestions, date pickers.

## Consequences

Parsing must be incremental and cheap (it runs per keystroke); parsers move
into `Alfred.Core` where they are unit-tested against the exact strings a
user types. Suggestion popups get slightly busier — accepted, because every
extra element answers a question the user actually has.
