using PKHeX.Core;
namespace PKHeX.Everywhere.RomHacks.Expansion;

public static class NativePortable
{
    public static PKM Export(NativePokemon source, GameVersion? origin = null)
    {
        PKM target = new PK6();
        if (source.Species > target.MaxSpeciesID || source.Ability > target.MaxAbilityID || source.HeldItem > target.MaxItemID || source.Ball > target.MaxBallID || source.Moves.Any(m => m > target.MaxMoveID)) target = new PK9();
        if (source.Species > target.MaxSpeciesID || source.HeldItem > target.MaxItemID || source.Moves.Any(m => m > target.MaxMoveID)) throw new InvalidOperationException("A custom species, move or item has no standard Pokémon file equivalent.");
        target.Species = source.Species; target.Form = source.Form; target.EncryptionConstant = source.PID; target.PID = source.PID; target.ID32 = source.ID32;
        target.Language = source.Language; target.OriginalTrainerName = source.OriginalTrainerName; target.OriginalTrainerGender = source.OriginalTrainerGender;
        target.Nickname = source.Nickname; target.IsNicknamed = source.IsNicknamed; target.EXP = source.EXP; target.Nature = source.Nature; target.StatAlignment = source.Nature; target.Gender = source.Gender;
        target.Ability = source.Ability; target.AbilityNumber = source.AbilityNumber; target.Ball = source.Ball; target.HeldItem = source.HeldItem;
        target.OriginalTrainerFriendship = source.OriginalTrainerFriendship; target.Version = origin ?? source.Version; target.MetLevel = source.MetLevel;
        target.FatefulEncounter = source.FatefulEncounter; target.IsEgg = source.IsEgg; target.PokerusDays = source.PokerusDays; target.PokerusStrain = source.PokerusStrain;
        target.Moves = source.Moves; for (int i = 0; i < 6; i++) { target.SetIV(i, source.GetIV(i)); target.SetEV(i, source.GetEV(i)); }
        target.Move1_PP = source.Move1_PP; target.Move2_PP = source.Move2_PP; target.Move3_PP = source.Move3_PP; target.Move4_PP = source.Move4_PP;
        target.Move1_PPUps = source.Move1_PPUps; target.Move2_PPUps = source.Move2_PPUps; target.Move3_PPUps = source.Move3_PPUps; target.Move4_PPUps = source.Move4_PPUps;
        var place = source.Profile.Locations.GetValueOrDefault(source.MetLocation.ToString());
        if (target is PK6 && source.Profile.Region == "hoenn" && target.Version == GameVersion.E)
        {
            var matches = GameInfo.GetLocationList(GameVersion.OR, EntityContext.Gen6, false).Where(p => string.Equals(p.Text, place, StringComparison.OrdinalIgnoreCase)).ToArray();
            target.MetLocation = matches.Length == 1 ? (ushort)matches[0].Value : (ushort)0;
        }
        uint markings=source.Read("outer.markings");
        if (target is IAppliedMarkings4 marked)
        {
            marked.MarkingCircle=(markings&1)!=0;marked.MarkingSquare=(markings&2)!=0;marked.MarkingTriangle=(markings&4)!=0;marked.MarkingHeart=(markings&8)!=0;
        }
        if(target is IAppliedMarkings7 modernMarks)
        {
            modernMarks.MarkingCircle=(markings&1)!=0?MarkingColor.Blue:MarkingColor.None;modernMarks.MarkingSquare=(markings&2)!=0?MarkingColor.Blue:MarkingColor.None;
            modernMarks.MarkingTriangle=(markings&4)!=0?MarkingColor.Blue:MarkingColor.None;modernMarks.MarkingHeart=(markings&8)!=0?MarkingColor.Blue:MarkingColor.None;
        }
        if (target is IContestStats contest)
        {
            contest.ContestCool=(byte)source.Read("cool");contest.ContestBeauty=(byte)source.Read("beauty");contest.ContestCute=(byte)source.Read("cute");
            contest.ContestSmart=(byte)source.Read("smart");contest.ContestTough=(byte)source.Read("tough");contest.ContestSheen=(byte)source.Read("sheen");
        }
        if(target is IRibbonSetCommon3 common)
        {
            common.RibbonChampionG3=source.Read("championRibbon")!=0;common.RibbonArtist=source.Read("artistRibbon")!=0;common.RibbonEffort=source.Read("effortRibbon")!=0;
        }
        if(target is IRibbonSetEvent3 events)
        {
            events.RibbonChampionBattle=source.Read("marineRibbon")!=0;events.RibbonChampionRegional=source.Read("landRibbon")!=0;events.RibbonChampionNational=source.Read("skyRibbon")!=0;
            events.RibbonCountry=source.Read("countryRibbon")!=0;events.RibbonNational=source.Read("nationalRibbon")!=0;events.RibbonEarth=source.Read("earthRibbon")!=0;events.RibbonWorld=source.Read("worldRibbon")!=0;
        }
        if(target is IRibbonSetMemory6 memory)
        {
            memory.RibbonCountMemoryContest=(byte)(source.Read("coolRibbon")+source.Read("beautyRibbon")+source.Read("cuteRibbon")+source.Read("smartRibbon")+source.Read("toughRibbon"));memory.HasContestMemoryRibbon=memory.RibbonCountMemoryContest!=0;
            memory.RibbonCountMemoryBattle=(byte)(source.Read("winningRibbon")+source.Read("victoryRibbon"));memory.HasBattleMemoryRibbon=memory.RibbonCountMemoryBattle!=0;
        }
        if (!source.IsShiny && target.IsShiny)
        {
            for (uint i = 1; i < 65536; i++)
            {
                uint candidate = unchecked(source.PID + 50 * i);
                uint xor = (candidate & 65535) ^ (candidate >> 16) ^ source.TID16 ^ source.SID16;
                if (xor < 16 || EntityGender.GetFromPIDAndRatio(candidate, source.PersonalInfo.Gender) != source.Gender) continue;
                target.PID = candidate; break;
            }
        }
        if (source.IsShiny && !target.IsShiny)
        {
            bool found = false;
            for (uint delta = 0; delta < 65536 && !found; delta++)
            {
                uint low = ((source.PID & 65535) + delta) & 65535;
                for (uint xor = 0; xor < 8; xor++)
                {
                    uint candidate = low | ((low ^ source.TID16 ^ source.SID16 ^ xor) << 16);
                    if (candidate % 25 != source.PID % 25 || (candidate & 1) != (source.PID & 1) || EntityGender.GetFromPIDAndRatio(candidate, source.PersonalInfo.Gender) != source.Gender) continue;
                    target.PID = candidate; found = true; break;
                }
            }
            if (!found) throw new InvalidOperationException("Unable to preserve forced shininess in a portable file.");
        }
        target.ResetPartyStats(); target.RefreshChecksum(); return target;
    }
}
