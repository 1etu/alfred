# 0012 — Alfred.Localization: no hardcoded messages

## Status

Accepted.

## Context

Every user-facing string is hardcoded English. Alfred's first audience is
bilingual (English/Turkish) and the money formatting is already Turkish —
the words should be able to follow. Standard .NET resx satellite assemblies
are heavier than this app wants: designer files, satellite deployment,
culture probing at startup.

## Decision

A fifth library, `Alfred.Localization`, built on the same pattern as the
theme framework — because a language pack and a theme are the same shape:
a named set of keyed values, swapped live.

- **Catalogs are code.** `EnglishCatalog` and `TurkishCatalog` are
  dictionaries keyed by constants in `LocalizationKeys` (dot-namespaced:
  `Capture.Placeholder.Payment`). Compile-checked keys, zero parse cost,
  trivially diffable.
- **English is the base.** Other catalogs overlay it; a missing key falls
  back to English silently in release and fails a test in CI — the test
  asserts every Turkish key exists in English and reports coverage the other
  way.
- **Live switch.** `LocalizationService.Apply(language)` builds a
  `ResourceDictionary` of strings and swaps it into application resources —
  XAML consumes `{DynamicResource}` string keys exactly as it consumes theme
  brushes, and language changes take effect without restart. Code paths use
  `LocalizationService.Text(key)` / `Text(key, args)` which formats with the
  language's culture.
- **No hardcoded messages** becomes a rule: any user-visible literal in a
  view, view model, or kit control is a defect. The sweep is phased —
  strings move as pages are redesigned, plus one dedicated cleanup phase.
- **Preference**: `Language` in preferences, default `en`; `tr` ships in V2.
  The OS language is not auto-followed — an explicit choice in Settings,
  because money vocabulary and date formats changing silently would violate
  ADR [0004](0004-interaction-clarity.md).

## Consequences

Adding a language is one file and one settings row. Culture-sensitive
formatting (dates, plurals) stays in code near the catalogs, not scattered.
The dependency graph grows by one leaf: everything may reference
Localization; it references nothing.
