using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
namespace PKHeX.Everywhere.RomHacks.Historic;

// Read-only, revision-specific recovery. Unknown species/forms and fields are not guessed.
public static class LazarusRecovery
{
    private const uint RomCrc = 0x558AE42F;
    public static string InspectBattery(byte[] data) => Export(HistoricGen3.InspectEmeraldSave(data).Records, "Lazarus battery save");
    public static string? InspectMgba(byte[] input)
    {
        byte[] state = MgbaState.ExtractCore(input);
        if (BinaryPrimitives.ReadUInt32LittleEndian(state) != 0x01000007) throw new InvalidDataException("Unsupported mGBA state version.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(8)) != RomCrc) return null;
        ReadOnlySpan<byte> Ram(uint addr,int len) {
            long start = addr switch { >=0x02000000 and <0x02040000 => 0x21000L + addr-0x02000000, >=0x03000000 and <0x03008000 => 0x19000L + addr-0x03000000, _ => throw new InvalidDataException("Invalid RAM pointer.") };
            long end = addr<0x03000000 ? 0x61000 : 0x21000;
            if (len<0 || start+len>end) throw new InvalidDataException("RAM pointer out of bounds.");
            return state.AsSpan((int)start,len);
        }
        int count=Ram(0x0201B95D,1)[0]; if(count>6)throw new InvalidDataException("Invalid party count.");
        uint ptr=BinaryPrimitives.ReadUInt32LittleEndian(Ram(0x0300366C,4));
        var storage=Ram(ptr,4+14*30*80);if(BinaryPrimitives.ReadUInt32LittleEndian(storage)>=14)throw new InvalidDataException("Invalid PC box index.");
        var records=new List<HistoricGen3.Record>();
        void Add(string pos,byte[] bytes,bool party){if((bytes[0x13]&2)==0){if(party)throw new InvalidDataException("Empty party slot.");return;}var p=new PK3(bytes.ToArray());records.Add(new(pos,bytes,p.ChecksumValid,p.SpeciesInternal,p.Nickname,p.OriginalTrainerName,(byte)p.MetLocation));}
        for(int i=0;i<count;i++)Add($"Party {i+1:D2}",Ram(0x0201B960u+(uint)(100*i),100).ToArray(),true);
        for(int b=0;b<14;b++)for(int s=0;s<30;s++)Add($"Box {b+1:D2} Slot {s+1:D2}",storage.Slice(4+(b*30+s)*80,80).ToArray(),false);
        return Export(records,"mGBA historic state");
    }
    private static string Export(IReadOnlyList<HistoricGen3.Record> records,string source)
    {
        using var ss=typeof(LazarusRecovery).Assembly.GetManifestResourceStream("historic.vba-lazarus-2-species.json") ?? throw new InvalidOperationException("Species mapping missing.");
        using var ms=typeof(LazarusRecovery).Assembly.GetManifestResourceStream("historic.vba-lazarus-2-moves.json") ?? throw new InvalidOperationException("Move mapping missing.");
        using var sj=JsonDocument.Parse(ss);using var mj=JsonDocument.Parse(ms);
        var species=sj.RootElement.GetProperty("species").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetUInt16(),x=>x.GetProperty("national").GetUInt16());
        var moves=mj.RootElement.GetProperty("moves").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetUInt16(),x=>x.GetProperty("standardId").GetUInt16());
        using var output=new MemoryStream();using(var w=new Utf8JsonWriter(output)){
            w.WriteStartObject();w.WriteString("profile","Pokémon Lazarus 2.0");w.WriteString("sourceType",source);
            w.WriteString("warning","Experimental read-only recovery for the supplied Lazarus 2.0 revision. Official species candidates and moves are mapped by exact ROM names; unknown species, forms and eggs are skipped. Abilities, gender and shiny use standard assumptions; held items are removed and balls become Poké Balls. Original records retain source data. Levels may be approximated, and locations are unknown.");
            w.WriteStartArray("pokemon");int skipped=0;
            foreach(var r in records){
                if(!r.ChecksumValid)throw new InvalidDataException($"{r.Position} has an invalid Pokémon checksum.");
                var p=new PK3(r.NativeRecord.ToArray());if(p.IsEgg||p.FlagIsBadEgg||!species.TryGetValue(p.SpeciesInternal,out ushort nat)){skipped++;continue;}
                w.WriteStartObject();w.WriteString("position",r.Position);w.WriteString("nickname",p.Nickname);w.WriteNumber("nativeSpecies",p.SpeciesInternal);w.WriteNumber("nationalSpecies",nat);w.WriteNumber("storedLevel",r.NativeRecord.Length==100?p.Stat_Level:p.CurrentLevel);w.WriteString("originalTrainer",p.OriginalTrainerName);w.WriteNumber("trainerId",p.TID16);w.WriteNumber("nativePid",p.PID);w.WriteNumber("nativeMetLocation",p.MetLocation);w.WriteNumber("nativeHeldItem",p.HeldItem);w.WriteNumber("nativeBall",p.Ball);w.WriteString("nativeFile",r.Position+".bin");w.WriteBase64String("nativeBytes",r.NativeRecord);
                try{
                    var mapped=p.Moves.Select(x=>x==0?(ushort)0:moves.TryGetValue(x,out ushort m)?m:throw new InvalidOperationException($"Unmapped move {x}.")).ToArray();
                    bool old=nat<=721 && mapped.All(x=>x<=621) && PersonalTable.XY.IsPresentInGame(nat,0);
                    PKM q=old?new PK6():new PK9();
                    if(!old && !PersonalTable.SV.IsPresentInGame(nat,0))throw new InvalidOperationException("No verified personal data for this species in the portable format.");
                    var pi=old?(PersonalInfo)PersonalTable.XY.GetFormEntry(nat,0):PersonalTable.SV.GetFormEntry(nat,0);
                    q.Species=nat;q.Form=0;q.EncryptionConstant=p.PID;q.PID=p.PID;q.ID32=p.ID32;q.Language=p.Language;q.OriginalTrainerName=p.OriginalTrainerName;q.OriginalTrainerGender=p.OriginalTrainerGender;q.Nickname=p.Nickname;q.IsNicknamed=!string.Equals(p.Nickname,SpeciesName.GetSpeciesNameGeneration(nat,2,old?(byte)6:(byte)9),StringComparison.OrdinalIgnoreCase);q.EXP=p.EXP;q.Nature=p.Nature;q.StatAlignment=p.Nature;q.Gender=EntityGender.GetFromPIDAndRatio(p.PID,pi.Gender);q.AbilityNumber=p.AbilityNumber;q.HeldItem=0;q.OriginalTrainerFriendship=p.OriginalTrainerFriendship;q.Version=GameVersion.E;q.MetLocation=0;q.MetLevel=p.MetLevel;q.Ball=4;q.FatefulEncounter=p.FatefulEncounter;q.PokerusDays=p.PokerusDays;q.PokerusStrain=p.PokerusStrain;q.Moves=mapped;
                    q.Move1_PP=p.Move1_PP;q.Move2_PP=p.Move2_PP;q.Move3_PP=p.Move3_PP;q.Move4_PP=p.Move4_PP;q.Move1_PPUps=p.Move1_PPUps;q.Move2_PPUps=p.Move2_PPUps;q.Move3_PPUps=p.Move3_PPUps;q.Move4_PPUps=p.Move4_PPUps;
                    q.RefreshAbility(p.AbilityNumber==2?1:0);for(int i=0;i<6;i++){q.SetIV(i,p.GetIV(i));q.SetEV(i,p.GetEV(i));}
                    if(r.NativeRecord.Length==100 && p.Stat_Level is >=1 and <=100 && q.CurrentLevel!=p.Stat_Level)q.EXP=Experience.GetEXP(p.Stat_Level,pi.EXPGrowth);
                    if(!p.IsShiny&&q.IsShiny)q.PID^=0x00100000;if(p.IsShiny!=q.IsShiny)throw new InvalidOperationException("Unable to preserve assumed shiny status.");
                    q.ResetPartyStats();q.RefreshChecksum();byte[] bytes=new byte[q.SIZE_STORED];q.WriteDecryptedDataStored(bytes);if(!q.ChecksumValid||q.Species!=nat||q.ID32!=p.ID32)throw new InvalidOperationException("Portable record validation failed.");
                    w.WriteString("file",r.Position+(old?".pk6":".pk9"));w.WriteBase64String("bytes",bytes);w.WriteBoolean("pidAdjusted",q.PID!=p.PID);w.WriteNull("error");
                }catch(Exception e)when(e is InvalidOperationException or ArgumentException or IndexOutOfRangeException){w.WriteNull("file");w.WriteNull("bytes");w.WriteString("error",e.Message);}
                w.WriteEndObject();
            }
            w.WriteEndArray();w.WriteNumber("skippedUnmapped",skipped);w.WriteEndObject();
        }return Encoding.UTF8.GetString(output.ToArray());
    }
}
