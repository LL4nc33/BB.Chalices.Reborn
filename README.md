# BB Chalices

A small cross-platform editor for Bloodborne chalice dungeons. Pick one of 200+
ready-made dungeons and drop it into one of your save's six altar slots — useful
for farming runs, blood-gem setups or just testing things without grinding the
glyphs by hand.

Built with Avalonia 12 and .NET 10, so it runs on Windows, Linux and macOS.

## Credits

The hard part — understanding the save format — stands on the shoulders of the
Bloodborne data-mining community, plus my own research on top of it:

- **The Tomb Prospectors community** and **[Hex Research Central](https://www.bloodborne-wiki.com/)**
  ([data-mining notes](https://www.bloodborne-wiki.com/2017/12/data-mining.html)) —
  the reverse-engineering of the headstone format that the rite, poison and
  4th-layer byte logic is based on.
- **Noxde's [Add Chalice Dungeons to your save](https://gist.github.com/Noxde/a29f699f4175bf315d9bd4baeebefb66)**
  — the dungeon glyph list (`dungeons.json`) and the original tool that first inspired this one.
- **The shadPS4 team** — the emulator this pairs with.

## Saves

Under the shadPS4 emulator, Bloodborne saves live at:

```
<shadPS4>/user/home/<id>/savedata/<CUSAxxxxx>/SPRJ0005/userdataNNNN
```

The `CUSA…` folder depends on your game's region/version, so the app doesn't
hard-code it — hit **Detect saves** and it walks your shadPS4 folder looking for
the `SPRJ0005` save directory and lists the characters it finds. You can also use
**Open Save…** to point it straight at a `userdata` file.

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

- **BB.Chalices.Core** – the save format itself: locating the inventory marker,
  reading/writing the 125-byte dungeon records, backups. No UI or framework deps.
- **BB.Chalices.Data** – the dungeon catalogue in SQLite (EF Core), seeded from
  `dungeons.json` the first time you run it.
- **BB.Chalices.Services** – load/save, catalogue queries, shadPS4 save discovery.
- **BB.Chalices.ViewModels** – ReactiveUI view models.
- **BB.Chalices.App** – the Avalonia UI and theme.

## Font

Headings and body text use [Cormorant Garamond](https://github.com/CatharsisFonts/Cormorant)
(SIL Open Font License), bundled under `src/BB.Chalices.App/Assets/Fonts`.

## License

MIT — see [LICENSE](LICENSE).
