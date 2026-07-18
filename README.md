# BB Chalices

A small cross-platform editor for Bloodborne chalice dungeons. Drop a dungeon into
any of your save's seven altar slots - the six stored slots plus the makeshift altar -
build your own, keep them in named lists, and share them with other players. Handy
for farming runs, blood-gem setups, or just testing things without grinding the
glyphs by hand.

The catalogue is organised into lists. Two are built in: a large by-area set
(compiled from the public Tomb Prospectors research) is bundled and works fully
offline - the **Community** list - and Nox's 200+ curated dungeons (the **Nox** list)
are his work, so they are fetched once from his gist with your consent and cached
locally. You can also make your own named lists, save the current altar as a list,
and add or remove dungeons freely. The app is fully usable offline either way.

Built with Avalonia 12 and .NET 10, so it runs on Windows, Linux and macOS.

## Building and sharing dungeons

Beyond placing catalogue dungeons, you can **build** one from scratch: pick an area,
variant and layout (or roll a random one) and watch the coffin item and gem pool
update, then set rites, poison, the 4th layer and the special enemy before you place
it. **Share** a dungeon or a whole list as a short code, or save a list as a `.bbc`
file; others import it by pasting the code or dropping the file onto the window.
Nothing touches your save until you press **Save changes**, and a backup is made first.

## Credits

Understanding the save format is the hard part, and that work stands on the
shoulders of the Bloodborne data-mining community, plus my own research on top of it:

- **The Tomb Prospectors community** (notably DrAnger, with Trin and Kazin), for the
  reverse-engineering of the chalice save format that the rite, poison and 4th-layer
  byte logic builds on:
  - [Hex Research Central](https://docs.google.com/spreadsheets/d/1zFIzhnXHhYomlR-tFJcyk3cPywf1snqkDH900j6rtAI/edit?gid=935899711#gid=935899711) ([data-mining notes](https://www.bloodborne-wiki.com/2017/12/data-mining.html))
  - the glyph and hex research sheets
    ([one](https://docs.google.com/spreadsheets/d/1zFIzhnXHhYomlR-tFJcyk3cPywf1snqkDH900j6rtAI/edit?gid=1741467922#gid=1741467922),
    [two](https://docs.google.com/spreadsheets/d/1psfenhcQJ06EUQgcEHBIQcfLD5Iq-kyKGTZINvy6228/edit?gid=1625060027#gid=1625060027))
- **Nox's [Add Chalice Dungeons to your save](https://www.nexusmods.com/bloodborne/mods/121)**
  ([dungeon list](https://gist.github.com/Noxde/a29f699f4175bf315d9bd4baeebefb66)), for the
  dungeon glyph list (`dungeons.json`) and the original tool that first inspired this one.
- **The shadPS4 team**, for the emulator this pairs with.

## Saves

Under the shadPS4 emulator, Bloodborne saves live at:

```
<shadPS4>/user/home/<id>/savedata/<CUSAxxxxx>/SPRJ0005/userdataNNNN
```

The `CUSA` folder depends on your game's region/version, so the app doesn't
hard-code it. Hit **Detect** and it walks your shadPS4 folder looking for the
`SPRJ0005` save directory and lists the characters it finds. You can also browse to
a `userdata` file directly with **Open**, **drag-and-drop** one onto the window,
or just relaunch - it reopens the last save automatically.

The app is **portable**: settings, the database, the catalogue cache and backups
all live in a `data/` folder right next to the executable, so moving or copying the
app folder takes everything with it. (If that folder isn't writable, it falls back
to your user profile.) You can resize each column any time with the **- / +** buttons
(or Ctrl +/-), and drag the dividers between columns.

Every save is backed up first, so you can always roll back. With auto-backup on
(the default) a timestamped copy is kept in the **Backups** folder, listed in the
**Backups** tab where you can restore or delete any of them.

## Download

Grab a self-contained build for your OS from the
[Releases](https://github.com/LL4nc33/BB.Chalices.Reborn/releases) page - no .NET
install needed, it is one file:

- **Windows**: `*-win-x64.zip` - unzip and run `BB.Chalices.App.exe`.
- **Linux**: `*-linux-x64.tar.gz` - extract, `chmod +x`, run.
- **macOS**: `*-osx-x64` (Intel) or `*-osx-arm64` (Apple Silicon). Unsigned, so the
  first launch needs `xattr -dr com.apple.quarantine BB.Chalices.App` or a
  right-click - Open.

On first run, open **Settings** (or the catalogue prompt) and download the dungeon
catalogue from Nox's gist; after that it is cached and the app works offline.

Or build it yourself (below).

## Building

You need the .NET 10 SDK.

```sh
dotnet build
dotnet test
dotnet run --project src/BB.Chalices.App
```

## Layout

- **BB.Chalices.Core** is the save format itself: finding the inventory marker,
  reading and writing the 125-byte dungeon records, backups. No UI or framework dependencies.
- **BB.Chalices.Data** holds the dungeon catalogue in SQLite (EF Core), seeded from the
  bundled by-area set and, with your consent, Nox's gist (downloaded once, then cached).
- **BB.Chalices.Services** does load/save, catalogue queries and shadPS4 save discovery.
- **BB.Chalices.ViewModels** has the ReactiveUI view models.
- **BB.Chalices.App** is the Avalonia UI and theme.

## Font

Headings and body text use [Cormorant Garamond](https://github.com/CatharsisFonts/Cormorant)
(SIL Open Font License), bundled under `src/BB.Chalices.App/Assets/Fonts`.

## License

MIT. See [LICENSE](LICENSE).
