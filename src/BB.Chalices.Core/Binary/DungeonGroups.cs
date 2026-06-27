namespace BB.Chalices.Core.Binary;

// The unique coffin item each dungeon holds, by area and dungeon id. From the
// Tomb Prospectors "HolygrailExParam" research (DrAnger/Trin). Dungeons are split
// into id ranges, each tied to one item; sinister dungeons share their regular
// version's loot, so this is keyed on the area byte only.
public static class DungeonGroups
{
    private readonly record struct Group(int Start, int End, string Item);

    private static readonly Dictionary<byte, Group[]> ByArea = new()
    {
        [0x0A] = // Pthumeru 1
        [
            new(0, 19, "Uncanny Saw Cleaver"), new(20, 39, "Frenzied Coldblood (7)"),
            new(40, 59, "10x Bone Marrow Ash"), new(60, 79, "6x Fire Paper"),
            new(80, 99, "Blood ATK Up Circle gem"),
        ],
        [0x14] = // Pthumeru 2
        [
            new(0, 19, "Uncanny Saw Spear"), new(20, 39, "Uncanny Threaded Cane"),
            new(40, 59, "Frenzied Coldblood (9)"), new(60, 79, "4x Shaman Bone Blade"),
            new(80, 99, "Physical ATK Up Droplet gem"),
        ],
        [0x15] = // Hintertomb 2
        [
            new(0, 19, "Uncanny Hunter Axe"), new(20, 39, "Blood Stone Chunk"),
            new(40, 59, "Frenzied Coldblood (9)"), new(60, 79, "6x Bolt Paper"),
            new(80, 99, "Heavy Droplet gem"),
        ],
        [0x1E] = // Pthumeru 3
        [
            new(0, 14, "Uncanny Kirkhammer"), new(15, 29, "Lost Saw Spear"),
            new(30, 43, "Lost Saw Cleaver"), new(44, 57, "Uncanny Riflespear"),
            new(58, 71, "Formless Oedon (QS bullets +2)"), new(72, 85, "Great One Coldblood"),
            new(86, 99, "Fire ATK Up Radial gem"),
        ],
        [0x1F] = // Hintertomb 3
        [
            new(0, 14, "Uncanny Stake Driver"), new(15, 29, "Great Deep Sea (all res +50)"),
            new(30, 43, "Deep Sea (frenzy res +100)"), new(44, 57, "Stunning Deep Sea (rapid poison res +100)"),
            new(58, 71, "Great Lake (all dmg -3%)"), new(72, 85, "Great One Coldblood"),
            new(86, 99, "ATK vs Beasts Up Triangle gem"),
        ],
        [0x28] = // Pthumeru 4 (Defiled)
        [
            new(0, 29, "Ludwig's Uncanny Holy Blade"), new(30, 59, "Uncanny Chikage"),
            new(60, 87, "Uncanny Logarius Wheel"), new(88, 115, "Uncanny Reiterpallasch"),
            new(116, 143, "Blood Rapture (V.ATK restore HP +250)"), new(144, 171, "Lake (Phys. dmg -5%)"),
            new(172, 199, "Physical ATK Up Radial gem"),
        ],
        [0x2A] = // Loran 4
        [
            new(0, 29, "Uncanny Beast Claw"), new(30, 59, "Lost Hunter Axe"),
            new(60, 87, "Uncanny Tonitrus"), new(88, 115, "Lost Riflespear"),
            new(116, 143, "Dissipating Lake (Bolt dmg -7%)"), new(144, 171, "Fading Lake (Fire dmg -7%)"),
            new(172, 199, "Fire ATK Up Waning gem"),
        ],
        [0x32] = // Pthumeru 5 (Ihyll)
        [
            new(0, 13, "Lost Burial Blade"), new(14, 27, "Lost Blade of Mercy"),
            new(28, 41, "Lost Chikage"), new(42, 55, "Lost Logarius Wheel"),
            new(56, 69, "Lost Reiterpallasch"), new(70, 83, "Formless Oedon (QS bullets +5)"),
            new(84, 97, "Communion (vials +5)"), new(98, 111, "Lake (Phys. dmg -7%)"),
            new(112, 125, "Heir (echoes from V.ATKs +50%)"), new(126, 139, "Oedon Writhe (V.ATK grants QS bullets +3)"),
            new(140, 151, "Clockwise Metamorphosis (max HP +15%)"), new(152, 163, "Ring of Betrothal"),
            new(164, 175, "Blood Rock"), new(176, 187, "Physical ATK Up Radial gem"),
            new(188, 199, "Blood ATK Up Circle gem"),
        ],
        [0x34] = // Loran 5
        [
            new(0, 13, "Lost Beast Claw"), new(14, 27, "Uncanny Burial Blade"),
            new(28, 41, "Uncanny Blade of Mercy"), new(42, 55, "Lost Tonitrus"),
            new(56, 69, "Lost Stake Driver"), new(70, 83, "Beast (transform boost +100)"),
            new(84, 97, "Stunning Deep Sea (rapid poison res +300)"), new(98, 111, "Clear Deep Sea (slow poison res +300)"),
            new(112, 125, "Dissipating Lake (Bolt dmg -10%)"), new(126, 139, "Fading Lake (Fire dmg -10%)"),
            new(140, 151, "Clawmark (V.ATK +30%)"), new(152, 163, "Anti-Clockwise Metamorphosis (max stamina +20%)"),
            new(164, 175, "Blood Rock"), new(176, 187, "Fire ATK Up Waning gem"),
            new(188, 199, "Bolt ATK Up Waning gem"),
        ],
        [0x35] = // Isz 5
        [
            new(0, 17, "Ludwig's Lost Holy Blade"), new(18, 35, "Lost Kirkhammer"),
            new(36, 53, "Lost Threaded Cane"), new(54, 71, "Great Deep Sea (all res +150)"),
            new(72, 87, "Deep Sea (frenzy res +300)"), new(88, 103, "Great Lake (all dmg -5%)"),
            new(104, 119, "Arcane Lake (Arcane dmg -10%)"), new(120, 135, "Eye (items from fallen enemies +100)"),
            new(136, 151, "Blood Rock"), new(152, 167, "Arcane ATK Up Triangle gem"),
            new(168, 183, "Isz glitch (no unique item)"), new(184, 199, "Isz glitch (no unique item)"),
        ],
    };

    // The unique coffin item for a dungeon, from its area byte, layout-seed map
    // byte and dungeon number. Depth 4-5 chalices have two maps of 100, so the
    // second map's dungeons are ids 100-199. Returns null if unknown.
    public static string? UniqueItem(byte areaByte, byte layoutMap, byte dungeonNumber)
    {
        int id = (layoutMap & 0x01) * 100 + dungeonNumber;
        if (ByArea.TryGetValue(areaByte, out var groups))
            foreach (var group in groups)
                if (id >= group.Start && id <= group.End)
                    return group.Item;
        return null;
    }
}
