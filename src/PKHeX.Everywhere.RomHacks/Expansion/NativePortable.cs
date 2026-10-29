using PKHeX.Core;
namespace PKHeX.Everywhere.RomHacks.Expansion;

public static class NativePortable
{
    public static PKM Export(NativePokemon source, GameVersion? origin = null, bool preferModern = false)
    {
        var mapping = NativeLocations.Resolve(source);
        PKM target = !preferModern && !source.HasRandomizedAbility && (mapping.Region is "johto" or "kanto") && mapping.ExactLocation != 0 && Fits(source, new PK4()) ? new PK4() : new PK6();
        if (!Fits(source, target)) target = new PK9();
        if (!Fits(source, target)) throw new InvalidOperationException("A species, form, move, ability, item or name has no supported standard Pokémon file equivalent.");
        target.Species = source.Species; target.Form = source.Form; target.EncryptionConstant = source.PID; target.PID = source.PID; target.ID32 = source.ID32;
        target.Language = source.Language; target.OriginalTrainerName = source.OriginalTrainerName; target.OriginalTrainerGender = source.OriginalTrainerGender;
        target.Nickname = source.Nickname; target.IsNicknamed = source.IsNicknamed; target.EXP = source.EXP; target.Nature = source.Nature; target.StatAlignment = source.Nature; target.Gender = source.Gender;
        target.Ability = source.Ability; target.AbilityNumber = source.AbilityNumber; target.HeldItem = source.HeldItem;
        target.OriginalTrainerFriendship = source.OriginalTrainerFriendship; target.Version = origin ?? mapping.Origin; target.MetLevel = source.MetLevel;
        target.FatefulEncounter = source.FatefulEncounter; target.IsEgg = source.IsEgg; target.PokerusDays = source.PokerusDays; target.PokerusStrain = source.PokerusStrain;
        target.Moves = source.Moves; for (int i = 0; i < 6; i++) { target.SetIV(i, source.GetIV(i)); target.SetEV(i, source.GetEV(i)); }
        target.Move1_PP = source.Move1_PP; target.Move2_PP = source.Move2_PP; target.Move3_PP = source.Move3_PP; target.Move4_PP = source.Move4_PP;
        target.Move1_PPUps = source.Move1_PPUps; target.Move2_PPUps = source.Move2_PPUps; target.Move3_PPUps = source.Move3_PPUps; target.Move4_PPUps = source.Move4_PPUps;
        target.MetLocation = NativeLocations.PortableLocation(mapping, target);
        target.Ball = source.Ball;
        if (target is PK4 hgss && hgss.Ball != source.Ball) hgss.BallHGSS = source.Ball;
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
        if (target is G4PKM old)
        {
            old.RibbonWinning=source.Read("winningRibbon")!=0;old.RibbonVictory=source.Read("victoryRibbon")!=0;
            old.RibbonG3Cool=source.Read("coolRibbon")>=1;old.RibbonG3CoolSuper=source.Read("coolRibbon")>=2;old.RibbonG3CoolHyper=source.Read("coolRibbon")>=3;old.RibbonG3CoolMaster=source.Read("coolRibbon")>=4;
            old.RibbonG3Beauty=source.Read("beautyRibbon")>=1;old.RibbonG3BeautySuper=source.Read("beautyRibbon")>=2;old.RibbonG3BeautyHyper=source.Read("beautyRibbon")>=3;old.RibbonG3BeautyMaster=source.Read("beautyRibbon")>=4;
            old.RibbonG3Cute=source.Read("cuteRibbon")>=1;old.RibbonG3CuteSuper=source.Read("cuteRibbon")>=2;old.RibbonG3CuteHyper=source.Read("cuteRibbon")>=3;old.RibbonG3CuteMaster=source.Read("cuteRibbon")>=4;
            old.RibbonG3Smart=source.Read("smartRibbon")>=1;old.RibbonG3SmartSuper=source.Read("smartRibbon")>=2;old.RibbonG3SmartHyper=source.Read("smartRibbon")>=3;old.RibbonG3SmartMaster=source.Read("smartRibbon")>=4;
            old.RibbonG3Tough=source.Read("toughRibbon")>=1;old.RibbonG3ToughSuper=source.Read("toughRibbon")>=2;old.RibbonG3ToughHyper=source.Read("toughRibbon")>=3;old.RibbonG3ToughMaster=source.Read("toughRibbon")>=4;
            old.PID = Gen4Pid(source);
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

    private static bool Fits(NativePokemon source, PKM target)
    {
        if (source.Species > target.MaxSpeciesID || source.Ability > target.MaxAbilityID || source.HeldItem > target.MaxItemID || source.Ball > target.MaxBallID || source.Moves.Any(m => m > target.MaxMoveID) || source.Nickname.Length > target.MaxStringLengthNickname || source.OriginalTrainerName.Length > target.MaxStringLengthTrainer) return false;
        return target is not PK4 || source.AbilityNumber != 4 && PersonalTable.HGSS.IsPresentInGame(source.Species, source.Form);
    }

    private static uint Gen4Pid(NativePokemon source)
    {
        bool Matches(uint pid) => pid % 25 == (uint)source.Nature && (1 << (int)(pid & 1)) == source.AbilityNumber && EntityGender.GetFromPIDAndRatio(pid, source.PersonalInfo.Gender) == source.Gender && (((pid & 65535) ^ (pid >> 16) ^ source.TID16 ^ source.SID16) < 8) == source.IsShiny;
        if (Matches(source.PID)) return source.PID;
        if (!source.IsShiny)
        {
            for (uint delta = 1; delta <= 65536; delta++) if (Matches(unchecked(source.PID + delta))) return unchecked(source.PID + delta);
        }
        else
        {
            for (uint delta = 0; delta < 65536; delta++)
            {
                uint low = ((source.PID & 65535) + delta) & 65535;
                for (uint xor = 0; xor < 8; xor++)
                {
                    uint pid = low | ((low ^ source.TID16 ^ source.SID16 ^ xor) << 16);
                    if (Matches(pid)) return pid;
                }
            }
        }
        throw new InvalidOperationException("Unable to preserve nature, ability slot and shininess in PK4.");
    }
}
