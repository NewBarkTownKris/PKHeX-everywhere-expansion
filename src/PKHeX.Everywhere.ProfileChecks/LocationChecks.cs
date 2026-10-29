using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;

static class LocationChecks
{
    public static void Run()
    {
        foreach (var id in new[] { "heart-and-soul-2.0.6", "soulgold-1.2.1" })
        {
            var profile = SourceProfile.Find(id);
            foreach (var (place, region) in new[] { ("Cherrygrove City", "Johto"), ("New Bark Town", "Johto"), ("Violet City", "Johto"), ("Route 29", "Johto"), ("Saffron City", "Kanto"), ("Route 1", "Kanto"), ("Route 26", "Kanto"), ("Viridian Forest", "Kanto") })
            {
                var mon = new NativePokemon(profile) { Language = 2, ID32 = 0x11223344, OriginalTrainerName = "TESTER", Nickname = "CHECK", PID = 0x87654321 };
                mon.SpeciesIndex = (ushort)profile.Symbols["SPECIES"]["SPECIES_MAGNETON"];mon.Move1 = 85;mon.Move1_PP = 15;mon.MetLevel = 40;mon.CurrentLevel = 55;mon.Version = GameVersion.E;mon.Ball=(byte)Ball.Poke;
                mon.MetLocation = ushort.Parse(profile.Locations.First(x => x.Value.Equals(place, StringComparison.OrdinalIgnoreCase)).Key);
                var original = mon.Data.ToArray();
                var exact = mon.ToPortable();Check(exact is PK4 && NativeLocations.DisplayLocation(exact) == place, id + " exact " + place);
                Check(exact.Version == (id.StartsWith("heart") ? GameVersion.HG : GameVersion.SS), "remake origin");
                Check(exact.Nature == mon.Nature && exact.AbilityNumber == mon.AbilityNumber && exact.Ability == mon.Ability && exact.IsShiny == mon.IsShiny, "Gen4 identity");
                var modern = NativePortable.Export(mon, preferModern: true);
                Check(modern is PK6 && NativeLocations.DisplayLocation(modern) == "the " + region + " region", "modern " + region);
                mon.Ball=(byte)Ball.Love;Check(mon.ToPortable() is PK4 apricorn&&apricorn.Ball==(byte)Ball.Love,"HGSS Apricorn ball retained");mon.Ball=(byte)Ball.Poke;
                mon.Move1 = (ushort)Move.Moonblast;
                var later = mon.ToPortable();Check(later is PK6 && later.Move1 == (ushort)Move.Moonblast && NativeLocations.DisplayLocation(later) == "the " + region + " region", "modern move retained");
                mon.Move1 = 85;Check(mon.Data.SequenceEqual(original), "export does not mutate native record");
                mon.SetShinyState(true);mon.Nature = Nature.Modest;
                var shiny = mon.ToPortable();Check(shiny is PK4 && shiny.IsShiny && shiny.Nature == Nature.Modest && shiny.AbilityNumber == mon.AbilityNumber, "Gen4 forced shiny and nature");
                mon.SetShinyState(false);mon.AbilityNumber = 4;
                Check(mon.ToPortable() is PK6, "hidden ability is not dropped for PK4");
                mon.AbilityNumber = 1;mon.Nickname = "TWELVECHARS!";
                Check(mon.ToPortable() is PK6 && mon.ToPortable().Nickname == mon.Nickname, "long nickname is not truncated for PK4");
            }
            Console.WriteLine("PASS HGSS exact locations and Johto/Kanto regional fallbacks: " + id);
        }
        var hoenn = SourceProfile.Find("pokeemerald-expansion-1-17-1");
        var test = (NativePokemon)new NativeSave(hoenn, SyntheticSave.Create(hoenn)).GetPartySlotAtIndex(0);
        test.Ball=(byte)Ball.Poke;
        test.Move1 = (ushort)Move.Moonblast;
        var portable = test.ToPortable();Check(portable is PK6 && portable.Version == GameVersion.OR && NativeLocations.DisplayLocation(portable) == "Rustboro City", "Gen6 Hoenn exact ORAS mapping");
        test.Move1 = (ushort)Move.ThunderCage;
        portable = test.ToPortable();Check(portable is PK9 && portable.Version == GameVersion.OR && NativeLocations.DisplayLocation(portable) == "Rustboro City" && portable.Move1 == (ushort)Move.ThunderCage, "post-Gen6 Hoenn exact location");
        Console.WriteLine("PASS ORAS origins and exact Hoenn locations with modern moves");
    }

    private static void Check(bool pass, string label) { if (!pass) throw new Exception("FAIL: " + label); }
}
