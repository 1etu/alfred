# adding a language

1. copy `en.json` to `<code>.json` — the file name is the BCP-47 code
   (`de.json`, `fr.json`, `pt-BR.json`).
2. set `"name"` to the language's own name for itself (`"Deutsch"`).
3. translate the values. keys stay exactly as they are.
4. delete any entry you can't translate — it falls back to English.
5. `dotnet test` — the test suite verifies your file parses, contains no
   unknown keys, and reports translation coverage.

that's the whole process. the language appears in Settings automatically.
`{0}`-style placeholders are substituted at runtime; keep them in your
translation, reordering is fine.
