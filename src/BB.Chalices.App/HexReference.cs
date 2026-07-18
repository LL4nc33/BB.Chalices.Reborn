namespace BB.Chalices.App;

// One row of a reference table: a hex code (or a label in the code column) and
// what it means. An empty Code renders the meaning as a full-width note.
public sealed record HexRow(string Code, string Meaning);

// A section of the hex reference: a heading, an optional prose body, and rows.
public sealed record HexSection(string Heading, string? Body, IReadOnlyList<HexRow> Rows);

// The dungeon-record byte map, transcribed from the Tomb Prospectors research
// (DrAnger, with Trin and Kazin). Shown verbatim as a reference for people who
// edit the raw strings; the app's own logic lives in Core/Binary/Headstone.cs.
public static class HexReference
{
    private static HexRow R(string code, string meaning) => new(code, meaning);
    private static HexRow Note(string meaning) => new(string.Empty, meaning);

    public static readonly IReadOnlyList<HexSection> Sections = new[]
    {
        new HexSection("Dungeon ID (bytes 1-4)",
            "The basic dungeon: where it is, its depth, and which layout it uses. Dungeons are not fully random - the game picks one from the pool for that chalice, then adds random effects. The record starts with the byte 1D.",
            Array.Empty<HexRow>()),

        new HexSection("1  Area and depth  (byte 1)",
            "The area and depth. Matches the map the dungeons are stored in.",
            new[]
            {
                R("0A", "Pthumeru 1"),
                R("14", "Pthumeru 2"),
                R("15", "Hintertomb 2"),
                R("1E", "Pthumeru 3"),
                R("1F", "Hintertomb 3"),
                R("28", "Pthumeru 4"),
                R("2A", "Loran 4"),
                R("32", "Pthumeru 5"),
                R("34", "Loran 5"),
                R("35", "Isz 5"),
                Note("These are every combination the game uses. Hintertomb only exists at depths 2 to 3, Loran at 4 to 5, and Isz at 5, so there is no Hintertomb 1 or Loran 1 to 3."),
            }),

        new HexSection("2  Layout seed  (bytes 2-3)",
            "The first byte picks the map, the second (00-63) picks the dungeon within it. Depth 1-3 use one map (100 dungeons); depth 4-5 use two (200). 2300 root dungeons in total.",
            new[]
            {
                R("00", "Map 1 (normal)"),
                R("01", "Map 2 (normal)"),
                R("0A", "Map 1 defiled"),
                R("0B", "Map 2 defiled"),
                R("14", "Map 1 sinister"),
                R("15", "Map 2 sinister"),
                Note("Second byte: 00 to 63, the dungeon within the map."),
                Note("Curated and edited dungeons can use a first byte outside this set for a fixed, pre-made encounter that normal generation never makes - for example the Queen Yharnam fight (a Pthumeru Ihyll fixed shared dungeon) uses 5A. Byte 1 (area and depth), on the other hand, is always one of the standard values above."),
            }),

        new HexSection("3  Dungeon ID on server",
            "The serial number sent to the server. Change it and the whole dungeon becomes that other dungeon. Likely controls bloodstains and messages too.",
            Array.Empty<HexRow>()),

        new HexSection("4  Join requirements  (4 bytes)",
            "Which chalice is needed to enter. Use it to make false-depth dungeons (a depth-5 layout you enter with a depth-1 chalice); the game then names the dungeon after the requirement. It does not change the difficulty. Side effect: it controls boss shards and chunks.",
            new[]
            {
                R("00 00 17 DD", "Pthumeru 1"),
                R("00 00 17 D4", "Pthumeru 1 root"),
                R("00 00 18 41", "Pthumeru 2"),
                R("00 00 18 38", "Pthumeru 2 root"),
                R("00 00 18 4B", "Hintertomb 2"),
                R("00 00 18 42", "Hintertomb 2 root"),
                R("00 00 18 A5", "Pthumeru 3"),
                R("00 00 18 9C", "Pthumeru 3 root"),
                R("00 00 18 9E", "Pthumeru 3 sinister root"),
                R("00 00 18 AF", "Hintertomb 3"),
                R("00 00 18 A6", "Hintertomb 3 root"),
                R("00 00 18 A8", "Hintertomb 3 sinister root"),
                R("00 00 19 09", "Pthumeru 4 (defiled)"),
                R("00 00 19 01", "Pthumeru 4 root (defiled)"),
                R("00 00 19 1D", "Loran 4"),
                R("00 00 19 14", "Loran 4 root"),
                R("00 00 19 6D", "Pthumeru 5"),
                R("00 00 19 64", "Pthumeru 5 root"),
                R("00 00 19 66", "Pthumeru 5 sinister root"),
                R("00 00 19 81", "Loran 5"),
                R("00 00 19 78", "Loran 5 root"),
                R("00 00 19 7A", "Loran 5 sinister root"),
                R("00 00 19 8B", "Isz 5"),
                R("00 00 19 82", "Isz 5 root"),
                R("00 00 19 84", "Isz 5 sinister root"),
            }),

        new HexSection("Dungeon effects (strings 5-13)",
            "Effects inside the dungeon that the player cannot control (unlike rites). Each string is 8 bytes, but only the last byte does anything - the other 7 are dummy bytes. FF usually means default or disabled.",
            Array.Empty<HexRow>()),

        new HexSection("5  Special enemy / shop",
            "Bath messengers, Patches the Spider, or sleeping BPS (Beast-Possessed Souls). Six variants, chosen at random; percentages are the odds under normal generation. Sinister chalices cannot spawn BPS. Isz uses its own byte range.",
            new[]
            {
                R("FF", "Default, none  (Pthumeru / Loran / Hintertomb)  -  60%"),
                R("1E", "Single Bath  -  7%"),
                R("1F", "All BPS  -  10%"),
                R("20", "Single Patches the Spider  -  3%"),
                R("21", "Bath + BPS  -  14%"),
                R("22", "Patches + BPS  -  6%"),
                R("4F", "Default, none  (Isz)  -  60%"),
                R("50", "Single Bath  (Isz)  -  7%"),
                R("51", "All BPS  (Isz)  -  10%"),
                R("52", "Single Patches the Spider  (Isz)  -  3%"),
                R("53", "Bath + BPS  (Isz)  -  14%"),
                R("54", "Patches + BPS  (Isz)  -  6%"),
                Note("Sinister: default 70%, single bath 21%, single patches 9%, no BPS."),
            }),

        new HexSection("6  Unique item",
            "Spawns the unique items (weapons, runes) in coffins. Remove it and they become ritual materials or gems. Which item is fixed per dungeon and cannot be changed.",
            new[]
            {
                R("23", "Pthumeru 1, Hintertomb 2, Loran 4  -  100%"),
                R("24", "Pthumeru 2, 3, 4, 5  -  100%"),
                R("25", "Hintertomb 3  -  100%"),
                R("26", "Loran 5  -  100%"),
                R("27", "Isz 5  -  100%"),
            }),

        new HexSection("7  Gem effect",
            "Boosts the rarity of certain gem effects for non-fixed stats. On Pthumeru 5, byte 36 also buffs enemies: HP x1.104, echoes x1.369, damage x1.087, defence x1.015.",
            new[]
            {
                R("32", "Pthumeru 1, 2, 3, 4  -  100%"),
                R("33", "Hintertomb 2, 3  -  100%"),
                R("34", "Loran 4, 5  -  100%"),
                R("35", "Isz 5  -  100%"),
                R("36", "Difficulty UP  (Pthumeru 5)  -  100%"),
            }),

        new HexSection("8  4th layer",
            "Whether a 4th layer opens; percentages are the odds under normal generation. Sinister chalices and the Pthumeru root (d1) never have one. Bytes 3F-42 are unused.",
            new[]
            {
                R("3C", "Open  (Pthumeru 2, Hintertomb 2/3, Loran 4/5)  -  20%"),
                R("FF", "Closed  -  80%"),
                R("3D", "Open  (Pthumeru 3, 4, 5)  -  20%"),
                R("3E", "Closed  (Pthumeru 3, 4, 5)  -  80%"),
                R("43", "Open  (Isz 5)  -  20%"),
                R("44", "Closed  (Isz 5)  -  80%"),
                R("5C", "Closed  (Sinister Pthumeru 5)  -  100%"),
            }),

        new HexSection("9  Poison",
            "Whether water pools, smoke urns and some enemies are poisonous. Pthumeru 1-3, Loran and Isz are never poisonous.",
            new[]
            {
                R("0A", "Poisonous  (Hintertomb 2, 3)  -  100%"),
                R("0D", "Poisonous  (Pthumeru 4, 5)  -  20%"),
                R("0E", "No poison  (Pthumeru 4, 5)  -  80%"),
                R("0F", "No poison  (Isz 5)  -  100%"),
            }),

        new HexSection("10-13  Riteslots  (the SFRC rites)",
            "Effects the player can change. A rite is applied to one of these four slots; FF disables a slot. Fetid strengthens some enemies (red aura, better loot). Rotted spawns extra enemies and better treasure. Cursed curses your gems but strengthens their effects, halves your and your allies' HP, and adds a x1.2 echoes multiplier. Sinister adds a Bell-Ringing Woman on sinister chalices.",
            new[]
            {
                R("FF", "Disable the slot"),
                R("0B", "Fetid  (slot 1)"),
                R("2D", "Rotted, shared fixed  (slot 2)"),
                R("49", "Cursed  (slot 3)"),
                R("28", "Sinister  (slot 4, sinister chalices)"),
            }),

        new HexSection("Rotted variants",
            "Rotted has six random variants; bytes 17, 1A, 1C, 1D spawn cut Micolash Marionettes (the bytes work, the enemies not always). Percentages are non-Hintertomb / Hintertomb.",
            new[]
            {
                R("14", "Eye Collector  -  16% / 24%"),
                R("15", "Tomb Prospectors  -  17% / 13%"),
                R("16", "Labyrinth Ritekeeper  -  17% / 13%"),
                R("18", "Eye Collector + Tomb Prospectors  -  17% / 25%"),
                R("19", "Eye Collector + Labyrinth Ritekeeper  -  17% / 25%"),
                R("1B", "Tomb Prospectors + Labyrinth Ritekeeper  -  16% / 0%"),
            }),

        new HexSection("Rite availability by depth",
            "Which rites normal dungeon generation allows.",
            new[]
            {
                R("Depth 1", "No rites"),
                R("Depth 2", "Fetid only"),
                R("Depth 3, Loran 4", "Fetid, Rotted"),
                R("Pthumeru 4, depth 5", "Fetid, Rotted, Cursed"),
                R("Sinister", "Sinister rite only"),
            }),

        new HexSection("14-15  Player information",
            "The last two strings identify the dungeon's original creator: their PSN and their character name. Searching the first four letters of the name (reversed) finds a dungeon's hexcode.",
            Array.Empty<HexRow>()),

        new HexSection("Extras and rarely-used bytes",
            "FF brings rites and effects back to default. Changing the first 7 bytes of strings 5-13 to FF does nothing (dummy bytes).",
            new[]
            {
                R("57", "Difficulty UP - buffs enemies like byte 36"),
                Note("Bytes 55-59 buy sinister chalices from bath messengers (the dungeon also needs a bath / Patches byte):"),
                R("55", "Sinister Pthumeru 3"),
                R("56", "Sinister Hintertomb 3"),
                R("57", "Sinister Pthumeru 5"),
                R("58", "Sinister Loran 5"),
                R("59", "Sinister Isz 5"),
                R("46", "Curse the defiled chalice (+1000 hidden Discovery with any Discovery rune)"),
                R("47", "Cursed: drop to 25% HP  (terrible curse, +5000 hidden Discovery)"),
                R("48", "Cursed: drop to 12.5% HP  (terrible curse; gem drops break if combined)"),
                R("5A", "Story: drops Pthumeru root chalices (progressive - causes harmless error messages)"),
                R("5E", "Story: drops Hintertomb root chalices"),
                R("60", "Story: drops Loran root chalices"),
                R("62", "Story: drops Isz root chalice"),
                R("01-04", "Unused Main Feature bytes (01 is like poison but turns things to oil)"),
                R("4D, 4E", "Sometimes present, no known function"),
            }),
    };
}
