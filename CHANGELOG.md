# Changelog

All notable changes to BB Chalices are listed here. Versions follow the
NexusMods release numbers.

## Unreleased

- Backups now live in the `data` folder next to the app, like everything else,
  so moving the app folder takes them along. **Upgrading from an older version:**
  earlier builds kept backups in `%LocalAppData%\BBChalices\Backups` and pinned
  that path in settings. On first launch your existing backups are copied into
  `data\Backups` once and the app switches to it automatically - nothing to do.
  You can delete the old `%LocalAppData%\BBChalices` folder afterwards. A backup
  folder you picked yourself is left unchanged.

## v0.98.5

- The editor now recognises the rites a dungeon actually has. It used to compare
  a rite slot against its own presets byte for byte, so rites the game generated
  read back as "None": bytes 2-3 hold a per-dungeon id that varies. Rites are now
  identified by their functional byte, including the ten Rotted enemy variants and
  the Curse variants (0x46-0x49). A slot holding a non-standard byte shows
  "custom effect (0xNN)" instead of looking empty.
- Poison can be set on Loran dungeons. Loran was missing from the poison table, so
  a Loran dungeon that already carried poison showed a ticked but greyed-out
  checkbox. Effects outside a dungeon's normal generation are now offered with an
  info hint instead of being locked out.
- The open save reloads automatically when something else rewrites it (the game
  saving its own state, a restore). Staged edits are never discarded: if you have
  unsaved changes the view is kept and you are told instead.
- The RITES tab scrolls, so MODIFIERS and the special-enemy row are no longer
  clipped behind the live byte panel when the editor is zoomed.
- The catalogue search box has a clear button; the window opens a little taller
  and the sidebar renders slightly smaller by default.

## v0.98.4

- Added an in-app hex reference for people editing strings directly: what every
  byte and string does, from the Tomb Prospectors byte map, including gem pools,
  coffin-group locking and the rite-stacking gotchas.
- Added a Launch shadPS4 button to the sidebar, and a setting to pick the shadPS4
  program yourself when it isn't found automatically.

## v0.98.3

- The app is now always portable: data (settings, database, catalogue cache,
  backups) lives in a data/ folder next to the executable, so moving the app
  folder takes everything with it. Removed the storage-location buttons; there
  is nothing to configure. Data from an earlier profile install is moved over
  automatically on first launch. Falls back to the user profile only if the app
  folder isn't writable.

## v0.98.2

- Renamed the "All" catalogue tab to "Community" (the Tomb Prospectors by-area
  set). It never contained Nox's dungeons, so "All" was misleading; the Nox tab
  now also states its dungeons need a one-time download.
- No more double backups: with auto-backup on you get one timestamped backup in
  the backup folder; with it off, one rolling backup/ folder next to the save.
  Never both.

## v0.98.1

- Fixed a backup being overwritten when two saves landed in the same millisecond
  (could corrupt a restore on macOS).
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
