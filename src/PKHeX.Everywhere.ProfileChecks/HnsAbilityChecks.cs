using System.Buffers.Binary;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
using PKHeX.Facade;
using PKHeX.Facade.Pokemons;

internal static class HnsAbilityChecks
{
    public static void Run()
    {
        var profile = SourceProfile.Find("heart-and-soul-2.0.6");
        var bytes = SyntheticSave.Create(profile);
        // Enable random abilities in both possible active sector-zero copies.
        for (int i = 0; i < 28; i++)
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i * 4096 + 4084)) == 0)
                bytes[i * 4096 + 3968 + 20] |= 0x80;
        var save = new NativeSave(profile, bytes) { ID32 = 0x11223344 };
        var mon = (NativePokemon)save.GetPartySlotAtIndex(0);
        // Reference values were generated using the unmodified HnS C randomizer functions.
        (ushort species, int slot, Ability expected)[] cases =
        [
            (6, 0, Ability.ShadowShield), (6, 1, Ability.ShadowShield), (6, 2, Ability.Hospitality),
            (25, 0, Ability.AirLock), (25, 1, Ability.AirLock), (25, 2, Ability.WaterAbsorb),
            (82, 0, Ability.Electromorphosis), (82, 1, Ability.ShellArmor), (82, 2, Ability.SandVeil),
            (150, 0, Ability.Triage), (150, 1, Ability.Triage), (150, 2, Ability.UnseenFist),
            (202, 0, Ability.ThermalExchange), (202, 1, Ability.ThermalExchange), (202, 2, Ability.VesselofRuin),
            (479, 0, Ability.Sharpness), (479, 1, Ability.Sharpness), (479, 2, Ability.Sharpness),
        ];
        foreach (var (species, slot, expected) in cases)
        {
            mon.SpeciesIndex = species; mon.Set("abilityNum", (uint)slot);
            if (mon.Ability != (int)expected || mon.Clone().Ability != (int)expected)
                throw new Exception($"HnS randomized ability mismatch: {species}/{slot}");
        }
        mon.SpeciesIndex = 82; mon.Set("abilityNum", 0);
        foreach (var (seed, expected) in new[] { (689u, Ability.AsOneI), (188u, Ability.AsOneG), (700u, Ability.EmbodyAspect0), (612u, Ability.EmbodyAspect1), (528u, Ability.EmbodyAspect2), (215u, Ability.EmbodyAspect3) })
        {
            save.ID32 = seed;
            if (mon.Ability != (int)expected) throw new Exception("HnS ability alias mapping mismatch.");
        }
        save.ID32 = 0x11223344;
        mon.SpeciesIndex = 82; mon.Set("abilityNum", 1);
        var editor = new Pokemon(mon, new Game(save));
        var choices = editor.Options().Abilities.Select(x => x.Id).ToHashSet();
        if (!choices.SetEquals([(int)Ability.Electromorphosis, (int)Ability.ShellArmor, (int)Ability.SandVeil]))
            throw new Exception("HnS editor ability choices do not match randomized slots.");
        editor.Update(new PokemonPatch(Ability: (int)Ability.SandVeil));
        if (editor.Pkm.Ability != (int)Ability.SandVeil) throw new Exception("HnS editor ability update failed.");
        editor.Update(new PokemonPatch(Ability: (int)Ability.ShellArmor));
        mon.Ability = (int)Ability.SandVeil;
        if (mon.Read("abilityNum") != 2) throw new Exception("HnS randomized ability selection did not select its native slot.");
        bool rejected = false; try { mon.Ability = (int)Ability.WonderGuard; } catch (InvalidOperationException) { rejected = true; }
        if (!rejected || mon.Read("abilityNum") != 2) throw new Exception("HnS invalid randomized ability selection was not refused.");
        mon.Ability = (int)Ability.ShellArmor;
        var portable = mon.ToPortable();
        if (portable is PK4 || portable.Ability != (int)Ability.ShellArmor)
            throw new Exception("HnS randomized ability was not preserved in a modern portable export.");
        save.SetPartySlotAtIndex(mon, 0);
        var reopened = new NativeSave(profile, save.Write().ToArray());
        if (reopened.GetPartySlotAtIndex(0).Ability != (int)Ability.ShellArmor)
            throw new Exception("HnS randomized ability changed after save edit.");
        reopened.ID32 = 1;
        if (reopened.GetPartySlotAtIndex(0).Ability == (int)Ability.ShellArmor)
            throw new Exception("HnS ability did not use the player trainer ID as seed.");
        Console.WriteLine("PASS Heart & Soul randomized ability reference vectors, save round trip and portable export");
    }
}
