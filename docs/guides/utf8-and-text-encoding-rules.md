# UTF-8 And Text Encoding Rules

## Purpose

This guide defines mandatory rules to avoid text corruption, broken accents and inconsistent localization behavior across the solution.

## Mandatory Rules

1. All documentation files must be saved as UTF-8.
2. JSON localization files must be UTF-8.
3. HTML responses and static files that contain text should declare UTF-8 where applicable.
4. Do not mix different encodings in the same feature flow.

## Current Relevant Areas

1. Blazor UI text and Razor components.
2. JSON files under [src/MoneyManager.Web/wwwroot/i18n](../../src/MoneyManager.Web/wwwroot/i18n)
3. static hosting behavior in [src/MoneyManager.Web.Host/Program.cs](../../src/MoneyManager.Web.Host/Program.cs)
4. documentation files under `docs/`

## Practical Rules For Developers

### When editing documentation

1. Save `.md` files as UTF-8.
2. After editing, reopen the file and verify accented text was preserved.
3. If a file already shows corruption, do not trust its current text blindly. Compare with code and newer documents.

### When editing localization JSON

1. Update every active locale file for new user-facing keys.
2. Keep key names stable across all locale files.
3. Validate the rendered text in the browser.

### When returning text from the host

Current state:

- `MoneyManager.Web.Host` already sets UTF-8 content types for `.json`, `.js`, `.css` and `.html`.

Reference: [src/MoneyManager.Web.Host/Program.cs](../../src/MoneyManager.Web.Host/Program.cs)

## Culture And Formatting Rules

1. Be explicit about culture-sensitive formatting when showing currency, dates or numbers.
2. Do not assume server culture and browser culture are the same.
3. When a string is intended for UI, prefer localization keys over hardcoded translated text.

## Serialization Rules

1. Keep JSON payloads UTF-8.
2. Avoid manual string rewriting of JSON files.
3. If tooling rewrites files with the wrong encoding, stop and correct the editor or tool configuration before continuing.

## Examples

Good practice:

- Add a new UI label in all locale JSON files.
- Validate the rendered accent marks in the browser.
- Keep the documentation note in UTF-8.

Bad practice:

- Edit only `pt-BR.json` and leave the other locale files stale.
- Copy text from a corrupted `.md` file back into the UI.
- Save a Markdown or JSON file with legacy encoding and commit it without review.

## Review Checklist

1. Is the file UTF-8?
2. Were all locales updated?
3. Was the text rendered and visually checked?
4. Did any tooling introduce corrupted characters?
