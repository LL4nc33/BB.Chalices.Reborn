# BB Chalices

A small cross-platform editor for Bloodborne chalice dungeons. Pick one of 200+
ready-made dungeons and drop it into one of your save's six altar slots. Handy for
farming runs, blood-gem setups, or just testing things without grinding the glyphs
by hand.

Built with Avalonia 12 and .NET 10, so it runs on Windows, Linux and macOS.

## Credits

Understanding the save format is the hard part, and that work stands on the
shoulders of the Bloodborne data-mining community, plus my own research on top of it:

- **The Tomb Prospectors community** (notably DrAnger, with Trin and Kazin), for the
  reverse-engineering of the chalice save format that the rite, poison and 4th-layer
  byte logic builds on:
  - [Hex Research Central](https://www.bloodborne-wiki.com/) ([data-mining notes](https://www.bloodborne-wiki.com/2017/12/data-mining.html))
  - the glyph and hex research sheets
    ([one](https://docs.google.com/spreadsheets/d/1zFIzhnXHhYomlR-tFJcyk3cPywf1snqkDH900j6rtAI/edit?gid=1741467922#gid=1741467922),
    [two](https://docs.google.com/spreadsheets/d/1psfenhcQJ06EUQgcEHBIQcfLD5Iq-kyKGTZINvy6228/edit?gid=1625060027#gid=1625060027))
- **Noxde's [Add Chalice Dungeons to your save](https://www.nexusmods.com/bloodborne/mods/121)**
  ([dungeon list](https://gist.github.com/Noxde/a29f699f4175bf315d9bd4baeebefb66)), for the
  dungeon glyph list (`dungeons.json`) and the original tool that first inspired this one.
- **The shadPS4 team**, for the emulator this pairs with.

## Saves

Under the shadPS4 emulator, Bloodborne saves live at:

```
<shadPS4>/user/home/<id>/savedata/<CUSAxxxxx>/SPRJ0005/userdataNNNN
```

The `CUSA` folder depends on your game's region/version, so the app doesn't
hard-code it. Hit **Detect saves** and it walks your shadPS4 folder looking for the
`SPRJ0005` save directory and lists the characters it finds. You can also browse to
a `userdata` file directly with **Open Save**.

Every time you save, the original file is copied to a `backup/` folder next to it
first, so you can always roll back.

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
- **BB.Chalices.Data** holds the dungeon catalogue in SQLite (EF Core), seeded from
  `dungeons.json` the first time you run it.
- **BB.Chalices.Services** does load/save, catalogue queries and shadPS4 save discovery.
- **BB.Chalices.ViewModels** has the ReactiveUI view models.
- **BB.Chalices.App** is the Avalonia UI and theme.

## Font

Headings and body text use [Cormorant Garamond](https://github.com/CatharsisFonts/Cormorant)
(SIL Open Font License), bundled under `src/BB.Chalices.App/Assets/Fonts`.

## License

MIT. See [LICENSE](LICENSE).
