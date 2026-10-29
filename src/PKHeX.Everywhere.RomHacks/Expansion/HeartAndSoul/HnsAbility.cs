using System.Text.Json;

namespace PKHeX.Everywhere.RomHacks.Expansion.HeartAndSoul;

// Heart & Soul 2.0.6 randomizer.c, random.h and its source-built species data.
internal static class HnsAbility
{
    private static readonly int[] Whitelist;
    private static readonly Dictionary<int, int[]> SpeciesAbilities;
    static HnsAbility()
    {
        using var stream = typeof(HnsAbility).Assembly.GetManifestResourceStream("hns.randomizer-abilities.json")!;
        using var json = JsonDocument.Parse(stream);
        Whitelist = json.RootElement.GetProperty("whitelist").EnumerateArray().Select(x => x.GetInt32()).ToArray();
        SpeciesAbilities = json.RootElement.GetProperty("speciesAbilities").EnumerateObject().ToDictionary(x => int.Parse(x.Name), x => x.Value.EnumerateArray().Select(y => y.GetInt32()).ToArray());
    }

    internal static int Resolve(SourceProfile profile, ushort species, int slot, uint seed)
    {
        if (!SpeciesAbilities.TryGetValue(species, out var abilities)) throw new InvalidOperationException("No source ability data for this Heart & Soul species.");
        int effective = slot;
        if (slot < 0 || slot >= abilities.Length || abilities[slot] == 0)
            effective = slot >= 2 && abilities[2] != 0 ? 2 : Array.FindIndex(abilities, a => a != 0);
        else if (slot == 1 && abilities[1] == abilities[0]) effective = 0;
        if (effective < 0) return 0;

        unchecked
        {
            // RANDOMIZER_REASON_ABILITIES = 9; stream = 17.
            uint a = seed + 9, b = seed ^ species, c = ((uint)species << 8) | (uint)effective, counter = 17;
            uint Next()
            {
                uint result = a + b + counter; counter += 17;
                a = b ^ (b >> 9); b = c * 9; c = result + ((c << 21) | (c >> 11));
                return result;
            }
            for (int i = 0; i < 10; i++) Next();
            uint mask = 1; while (mask < Whitelist.Length) mask <<= 1; mask--;
            int native;
            do
            {
                uint index; do index = Next() & mask; while (index >= Whitelist.Length);
                native = Whitelist[index];
            } while (native == 0 || native == profile.Symbols["ABILITY"]["ABILITY_WONDER_GUARD"]);
            if (!profile.Abilities.TryGetValue(native, out int mapped) || mapped == 0)
                throw new InvalidOperationException("The randomized ability has no standard Pokémon file mapping.");
            return mapped;
        }
    }
}
