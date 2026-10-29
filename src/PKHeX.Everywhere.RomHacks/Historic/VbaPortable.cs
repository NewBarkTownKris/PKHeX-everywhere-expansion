using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;

namespace PKHeX.Everywhere.RomHacks.Historic;

/// <summary>Conservative best-effort conversion of verified, real-species historic party records.</summary>
public static class VbaPortable
{
    public static PK6 Convert(PK3 source, JsonElement sourceTables, string? profileId = null)
    {
        if (source.IsEgg) throw new InvalidOperationException("Egg conversion is not yet verified for this game.");
        if (source.Species == 0 || !PersonalTable.XY.IsPresentInGame(source.Species, source.Form))
            throw new InvalidOperationException("This species or form has no verified Generation 6 equivalent.");
        var moveIds = sourceTables.GetProperty("moves").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToHashSet();
        if (source.Moves.Any(move => move != 0 && !moveIds.Contains(move)))
            throw new InvalidOperationException("A move has no verified standard-name mapping for this ROM.");
        int held = 0;
        if (source.HeldItem != 0)
        {
            var items = sourceTables.GetProperty("items").EnumerateArray().Where(x => x.GetProperty("id").GetInt32() == source.HeldItem).ToArray();
            if (items.Length != 1) throw new InvalidOperationException("The held item has no verified standard-name mapping for this ROM.");
            held = items[0].GetProperty("modern").GetInt32();
        }
        // The sampled states use Poké Balls. More ball IDs require separate game evidence.
        if (source.Ball != 4) throw new InvalidOperationException("Only the standard Poké Ball has been checked for this ROM.");
        var pk = new PK6
        {
            Species = source.Species, Form = source.Form, EncryptionConstant = source.PID, PID = source.PID,
            ID32 = source.ID32, Language = source.Language, OriginalTrainerName = source.OriginalTrainerName,
            OriginalTrainerGender = source.OriginalTrainerGender, Nickname = source.Nickname, IsNicknamed = source.IsNicknamed,
            EXP = source.EXP, Nature = source.Nature, StatAlignment = source.Nature, Gender = source.Gender,
            Ability = source.Ability, AbilityNumber = source.AbilityNumber, HeldItem = held,
            OriginalTrainerFriendship = source.OriginalTrainerFriendship, Version = source.Version,
            MetLevel = source.MetLevel, MetLocation = 0, Ball = source.Ball,
            FatefulEncounter = source.FatefulEncounter, PokerusDays = source.PokerusDays, PokerusStrain = source.PokerusStrain,
            Move1_PP = source.Move1_PP, Move2_PP = source.Move2_PP, Move3_PP = source.Move3_PP, Move4_PP = source.Move4_PP,
            Move1_PPUps = source.Move1_PPUps, Move2_PPUps = source.Move2_PPUps, Move3_PPUps = source.Move3_PPUps, Move4_PPUps = source.Move4_PPUps,
        };
        pk.Moves = source.Moves;
        for (int i=0; i<6; i++) { pk.SetIV(i, source.GetIV(i)); pk.SetEV(i, source.GetEV(i)); }
        if (!source.IsShiny && pk.IsShiny) pk.PID ^= 0x00100000; // preserve shiny state without changing gender byte
        if (source.IsShiny != pk.IsShiny) throw new InvalidOperationException("Could not preserve standard PID-derived shiny status.");
        if (profileId == "shiny-gold" && source.MetLocation == 143)
        {
            // Observed starter was obtained in New Bark Town; the ROM table labels ID 143 accordingly.
            pk.Version = GameVersion.HG;
            pk.MetLocation = NativeLocations.PortableLocation(new NativeOriginMapping(GameVersion.HG, "johto", 0), pk);
        }
        if (profileId is "lightning-yellow" or "gary-edition" && source.MetLocation == 88)
        {
            // The starter was received in Pallet Town; the ROM's map table labels ID 88 accordingly.
            pk.Version = GameVersion.HG;
            pk.MetLocation = NativeLocations.PortableLocation(new NativeOriginMapping(GameVersion.HG, "kanto", 0), pk);
        }
        pk.ResetPartyStats(); pk.RefreshChecksum();
        return pk;
    }
}
