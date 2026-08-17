# 0010 — Payments split into subcategories

## Status

Accepted.

## Context

The Payments page is one undifferentiated ledger. A Netflix subscription, a
one-off electrician, rent, and a friend's IOU all render identically, so the
page answers "what did I record" but not the real questions: what recurs,
what is fixed, where does the money actually go.

## Decision

**Subcategory is derived first, declared second.** Every ledger entry gets a
subcategory: Subscriptions, Bills, Rent & Home, Debts, One-off, Income
streams. The system infers it — a recurring schedule with a known brand is a
Subscription; a recurring utility-like entry is a Bill; income with a cadence
is a stream — and the user can override with one click. Inference is visible
and correctable, never silent (ADR
[0004](0004-interaction-clarity.md)).

**Model** (`Alfred.Core`): a `LedgerCategory` enum-backed value on
`LedgerEntry` plus the inference rules as a tested pure function. Existing
data migrates by running inference once.

**Page**: Payments gains a segmented scope control (All · Subscriptions ·
Bills · Debts · Income) and per-scope summaries — Subscriptions shows the
monthly total and the next three renewals; Bills shows due-this-month;
Debts shows net position. Rows keep the single-ledger visual language; only
the lens changes.

**Capture** infers subcategory from brand + cadence and chips it.

## Consequences

Subscriptions stop being a mental tag and become a real answer ("₺1.240/mo
across 9 services"). Inference will be wrong sometimes; the correction must
be one interaction and remembered per entry. The widget snapshot gains the
subscription monthly total — the number people actually want on their
desktop.
