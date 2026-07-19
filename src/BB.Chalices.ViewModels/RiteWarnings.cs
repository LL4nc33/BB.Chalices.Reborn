using BB.Chalices.Core.Binary;

namespace BB.Chalices.ViewModels;

// Warnings for rite combinations that misbehave, from the Tomb Prospectors research:
// two or more Fetid rites can corrupt enemy attack data and crash the game, and a
// dungeon with two or more Curse rites drops no gems at all. Shared by the slot
// editor and the dungeon builder so the copy stays in one place.
public static class RiteWarnings
{
    public static string? For(IEnumerable<Headstone.Rite> rites)
    {
        int fetid = 0, cursed = 0;
        foreach (var rite in rites)
        {
            if (rite == Headstone.Rite.Fetid) fetid++;
            else if (rite == Headstone.Rite.Cursed) cursed++;
        }

        if (fetid >= 2)
            return "Two or more Fetid rites can corrupt enemy data and crash the game.";
        if (cursed >= 2)
            return "Two or more Curse rites make the dungeon drop no gems.";
        return null;
    }
}
