# Changelog

All notable changes to BB Chalices are listed here. Versions follow the
NexusMods release numbers.

## v0.98.1

- Storage location is now fully configurable in Settings: keep data in your
  user profile, make the install portable (a data/ folder next to the app), or
  pick any folder with "Choose folder...". Switching moves your data over on the
  next launch automatically.
- Show the app version under the logo.
- Larger, easier-to-read tooltips.

## v0.98

First public release of the rewrite.

- Seven editable altar slots: the six stored slots plus the makeshift altar.
- Two-part dungeon catalogue: a large by-area set bundled for offline use (the
  All view), and Nox's 200+ curated dungeons fetched once from his gist with
  your consent, then cached for offline use (the Nox view).
- Portable mode: keep settings, database and catalogue cache in a data/ folder
  next to the app.
- Resize the UI with the A- / A+ buttons or Ctrl +/-, with brighter text.
- Load a save by dropping it onto the window, and reopen the last save on launch.
- Save your own dungeons to a persistent My-dungeons catalogue.
- Copy or paste a dungeon as a 125-byte hex string, and export or import a whole
  altar at once.
- Search the catalogue by gems, coffin or area; fill all six slots at once.
- Show each dungeon's favoured gem effects, plus Special Enemy and Difficulty Up
  modifiers.
- Colour-coded live byte view with a legend; STRINGS grouped into byte pairs.
- Built-in shadPS4 sound-crash fix.
- Every save writes a backup of the original first, with a Backups view to
  restore or delete them.
- Self-contained single-file builds for Windows, Linux and macOS.
