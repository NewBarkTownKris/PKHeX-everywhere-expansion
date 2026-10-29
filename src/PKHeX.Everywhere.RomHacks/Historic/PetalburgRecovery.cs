using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
namespace PKHeX.Everywhere.RomHacks.Historic;

// The supplied revision shares the expansion 1.17.1 record layout, but its ROM
// defines its own species/move tables. This reader is explicitly read-only.
public static class PetalburgRecovery
{
    public static string InspectBattery(byte[] bytes)
    {
        var save=new NativeSave(SourceProfile.Find("pokeemerald-expansion-1-17-1"),bytes);
        var members=Enumerable.Range(0,save.PartyCount).Select(i=>(Pos:$"Party {i+1:D2}",Mon:(NativePokemon)save.GetPartySlotAtIndex(i),Party:true)).Concat(Enumerable.Range(0,save.BoxCount*30).Select(i=>(Pos:$"Box {i/30+1:D2} Slot {i%30+1:D2}",Mon:(NativePokemon)save.GetBoxSlotAtIndex(i),Party:false))).Where(x=>x.Mon.SpeciesIndex!=0);
        return Export(members,"Petalburg Dynasty battery save");
    }
    public static string? InspectMgba(byte[] input)
    {
        byte[] state=MgbaState.ExtractCore(input);if(BinaryPrimitives.ReadUInt32LittleEndian(state)!=0x01000007)throw new InvalidDataException("Unsupported mGBA state version.");if(BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(8))!=0xA360B05C)return null;
        ReadOnlySpan<byte> Ram(uint addr,int len){long start=addr switch{>=0x02000000 and <0x02040000=>0x21000L+addr-0x02000000,>=0x03000000 and <0x03008000=>0x19000L+addr-0x03000000,_=>throw new InvalidDataException("Invalid RAM pointer.")};long end=addr<0x03000000?0x61000:0x21000;if(len<0||start+len>end)throw new InvalidDataException("RAM pointer out of bounds.");return state.AsSpan((int)start,len);}
        int count=Ram(0x02036DDC,1)[0];if(count>6)throw new InvalidDataException("Invalid party count.");uint ptr=BinaryPrimitives.ReadUInt32LittleEndian(Ram(0x03005464,4));var storage=Ram(ptr,4+14*30*80);if(BinaryPrimitives.ReadUInt32LittleEndian(storage)>=14)throw new InvalidDataException("Invalid PC box index.");var profile=SourceProfile.Find("pokeemerald-expansion-1-17-1");var members=new List<(string Pos,NativePokemon Mon,bool Party)>();
        void Add(string pos,byte[] raw,bool party){if((raw[0x13]&2)==0){if(party)throw new InvalidDataException("Empty party slot.");return;}PokeCrypto.Decrypt3(raw);members.Add((pos,new NativePokemon(profile,raw),party));}
        for(int i=0;i<count;i++)Add($"Party {i+1:D2}",Ram(0x02036DE8u+(uint)(i*100),100).ToArray(),true);for(int b=0;b<14;b++)for(int s=0;s<30;s++)Add($"Box {b+1:D2} Slot {s+1:D2}",storage.Slice(4+(b*30+s)*80,80).ToArray(),false);return Export(members,"mGBA historic state");
    }
    private static string Export(IEnumerable<(string Pos,NativePokemon Mon,bool Party)> members,string source)
    {
        using var stream=typeof(PetalburgRecovery).Assembly.GetManifestResourceStream("historic.vba-petalburg-1-10-candidates.json") ?? throw new InvalidOperationException("Petalburg mappings missing.");
        using var json=JsonDocument.Parse(stream);var species=json.RootElement.GetProperty("species").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetUInt16(),x=>x.GetProperty("national").GetUInt16());var moves=json.RootElement.GetProperty("moves").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetUInt32(),x=>x.GetProperty("national").GetUInt16());
        using var output=new MemoryStream();using(var w=new Utf8JsonWriter(output)){
            w.WriteStartObject();w.WriteString("profile","Pokémon Petalburg Dynasty 1.10.0");w.WriteString("sourceType",source);w.WriteString("warning","Experimental, explicitly selected revision. Only base species confirmed by source name and ID are exported; custom species, alternate forms and eggs are skipped. Moves are verified against source names. Portable files use standard ability and gender assumptions, remove held items, replace balls with Poké Balls, and leave the original location unknown. The original native records are retained.");w.WriteStartArray("pokemon");int skipped=0;
            foreach(var (pos,mon,party) in members){if(!mon.Valid)throw new InvalidDataException($"{pos} has an invalid checksum.");if(mon.IsEgg||!species.TryGetValue(mon.SpeciesIndex,out var nat)||nat!=mon.Species||mon.Form!=0){skipped++;continue;}
                byte[] raw=new byte[party?100:80];if(party)mon.WriteEncryptedDataParty(raw);else mon.WriteEncryptedDataStored(raw);
                w.WriteStartObject();w.WriteString("position",pos);w.WriteString("nickname",mon.Nickname);w.WriteNumber("nativeSpecies",mon.SpeciesIndex);w.WriteNumber("nationalSpecies",nat);w.WriteNumber("storedLevel",party?mon.Stat_Level:mon.CurrentLevel);w.WriteNumber("trainerId",mon.TID16);w.WriteString("originalTrainer",mon.OriginalTrainerName);w.WriteNumber("nativeHeldItem",mon.HeldItemIndex);w.WriteNumber("nativeBall",mon.Read("pokeball"));w.WriteNumber("nativeMetLocation",mon.MetLocation);w.WriteString("nativeFile",pos+".bin");w.WriteBase64String("nativeBytes",raw);
                try{
                    for(int i=1;i<=4;i++){uint id=mon.Read("move"+i);if(id!=0&&(!moves.TryGetValue(id,out ushort mapped)||mon.GetMove(i-1)!=mapped))throw new InvalidOperationException($"Unmapped or mismatched move {id}.");}
                    var copy=mon.Clone();copy.HeldItemIndex=0;copy.Ball=4;PKM portable=NativePortable.Export(copy,preferModern:true);portable.HeldItem=0;portable.Ball=4;portable.MetLocation=0;portable.Version=GameVersion.E;portable.RefreshAbility(copy.AbilityNumber switch{2=>1,4=>2,_=>0});portable.Gender=EntityGender.GetFromPIDAndRatio(copy.PID,portable.PersonalInfo.Gender);portable.ResetPartyStats();portable.RefreshChecksum();byte[] data=new byte[portable.SIZE_STORED];portable.WriteDecryptedDataStored(data);if(!portable.ChecksumValid||portable.Species!=nat||portable.ID32!=mon.ID32)throw new InvalidOperationException("Portable validation failed.");w.WriteString("file",pos+"."+portable.Extension);w.WriteBase64String("bytes",data);w.WriteNull("error");
                }catch(Exception e)when(e is InvalidOperationException or ArgumentException or IndexOutOfRangeException){w.WriteNull("file");w.WriteNull("bytes");w.WriteString("error",e.Message);}w.WriteEndObject();
            }w.WriteEndArray();w.WriteNumber("skippedUnmapped",skipped);w.WriteEndObject();
        }return Encoding.UTF8.GetString(output.ToArray());
    }
}
