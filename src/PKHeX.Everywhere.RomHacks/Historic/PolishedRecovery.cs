using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
namespace PKHeX.Everywhere.RomHacks.Historic;

// Polished Crystal 3.2.3. Data layout from the public release's SRAM and WRAM symbols.
public static class PolishedRecovery
{
    private sealed record Entry(string Position,byte[] Body,byte[] OT,byte[] Nick,byte[] Native,bool Party);
    public static string InspectBattery(byte[] data)
    {
        if(data.Length is not (0x8000 or 0x802c or 0x8030))throw new InvalidDataException("Expected a 32 KiB Polished Crystal save, optionally with an RTC footer.");
        bool primary=Valid(data,0x2007,0x2008,0x2b83,0x2d0d,0x2d0f),backup=Valid(data,0x1207,0x1208,0x1d83,0x1f0d,0x1f0f);
        if(!primary&&!backup)throw new InvalidDataException("No valid Polished Crystal save checksum and markers.");
        int party=primary?0x285e:0x1a5e,boxes=primary?0x30e4:0x3378;
        var records=ReadParty(data.AsSpan(party,0x21c));
        for(int box=0;box<20;box++)for(int slot=0;slot<20;slot++){
            int t=boxes+box*33,index=data[t+slot];if(index==0)continue;if(index>207)throw new InvalidDataException("Invalid box database index.");
            int side=(data[t+20+slot/8]>>(slot%8))&1,n=index-1;
            int o=n<167?(side==0?0x4000:0x6000)+49*n:n<195?(side==0?0:0x0bf1)+49*(n-167):(side==0?0x360c:0x3858)+49*(n-195);
            var raw=data.AsSpan(o,49).ToArray();if(!ValidBox(raw))throw new InvalidDataException($"Box {box+1:D2} Slot {slot+1:D2} checksum failed.");
            var nick=DecodeStored(raw.AsSpan(32,10));var ot=DecodeStored(raw.AsSpan(42,7));records.Add(new($"Box {box+1:D2} Slot {slot+1:D2}",raw[..32],ot,nick,raw,false));
        }
        return Export(records,"Game Boy battery save",primary?"primary":"backup");
    }
    private static bool Valid(byte[] b,int marker1,int start,int end,int check,int marker2)
    {
        if(b[marker1]!=97||b[marker2]!=127)return false;int sum=0;for(int i=start;i<end;i++)sum=(sum+b[i])&65535;return sum==BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(check,2));
    }
    private static bool ValidBox(byte[] b)
    {
        int sum=127;for(int i=0;i<32;i++)sum+=b[i]*(i+1);for(int i=0;i<17;i++)sum+=(b[32+i]&127)*(34+i);sum&=65535;int embedded=0;for(int i=0;i<16;i++)embedded=(embedded<<1)|(b[32+i]>>7);return embedded==sum&&(b[48]&128)==0;
    }
    private static byte[] DecodeStored(ReadOnlySpan<byte> b){var o=new byte[b.Length];for(int i=0;i<b.Length;i++){int x=b[i]&127;o[i]=x switch{0x7a=>0x7f,0x7b=>0x53,0x7c=>0,_=>(byte)(x|0x80)};}return o;}
    private static List<Entry> ReadParty(ReadOnlySpan<byte> p)
    {
        int count=p[0];if(count>6)throw new InvalidDataException("Invalid party count.");var entries=new List<Entry>();for(int i=0;i<count;i++){byte[] body=p.Slice(8+i*48,48).ToArray(),ot=p.Slice(0x128+i*11,8).ToArray(),nick=p.Slice(0x16a+i*11,11).ToArray();entries.Add(new($"Party {i+1:D2}",body,ot,nick,body.Concat(ot).Concat(nick).ToArray(),true));}return entries;
    }
    public static string InspectLiveCore(byte[] core)
    {
        if(core.Length<0x11800)throw new InvalidDataException("Incomplete Game Boy state.");return Export(ReadParty(core.AsSpan(0x60ce,0x21c)),"mGBA Game Boy party","live RAM");
    }
    private static string Export(List<Entry> records,string source,string slot)
    {
        using var f=typeof(PolishedRecovery).Assembly.GetManifestResourceStream("historic.gb-polished-crystal-3-2-3.json")??throw new InvalidOperationException("Polished Crystal mapping missing.");using var j=JsonDocument.Parse(f);
        var species=j.RootElement.GetProperty("species").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetInt32(),x=>x.GetProperty("national").GetUInt16());var moves=j.RootElement.GetProperty("moves").EnumerateArray().ToDictionary(x=>x.GetProperty("id").GetInt32(),x=>x.GetProperty("national").GetUInt16());var chars=j.RootElement.GetProperty("charmap").EnumerateObject().ToDictionary(x=>int.Parse(x.Name),x=>x.Value.GetString()!);
        string Decode(byte[] raw){var sb=new StringBuilder();foreach(byte b in raw){if(b==0x53)break;if(!chars.TryGetValue(b,out var c)||c.Contains('<'))throw new InvalidOperationException($"Unsupported name character {b:X2}.");sb.Append(c);}return sb.ToString();}
        using var output=new MemoryStream();using(var w=new Utf8JsonWriter(output)){
            w.WriteStartObject();w.WriteString("profile","Polished Crystal 3.2.3");w.WriteString("sourceType",source);w.WriteString("saveSlot",slot);w.WriteString("warning","Read-only 3.2.3 recovery. Only mapped real base species are exported; eggs and unverified forms are skipped. Trainer secret ID and PID are approximated because they are not stored; DVs become IVs, boxed PP is estimated, held items are removed and balls become Poké Balls. Original location is retained in metadata but not mapped. Source shiny and nature are preserved where possible. The faithful and normal builds may differ in mechanics.");w.WriteStartArray("pokemon");int skipped=0;
            foreach(var entry in records){var b=entry.Body;int native=b[0]|((b[21]&0x20)!=0?256:0);int form=b[21]&31;if((b[21]&0x40)!=0||form!=1||!species.TryGetValue(native,out ushort nat)){skipped++;continue;}
                w.WriteStartObject();w.WriteString("position",entry.Position);w.WriteNumber("nativeSpecies",native);w.WriteNumber("nationalSpecies",nat);w.WriteNumber("nativeItem",b[1]);w.WriteNumber("nativeLocation",b[entry.Party?30:27]);w.WriteBoolean("nativeShiny",(b[20]&128)!=0);w.WriteString("nativeFile",entry.Position+".bin");w.WriteBase64String("nativeBytes",entry.Native);
                try{
                    string ot=Decode(entry.OT),nick=Decode(entry.Nick);if(string.IsNullOrWhiteSpace(ot)||string.IsNullOrWhiteSpace(nick))throw new InvalidOperationException("Missing trainer or nickname.");
                    var mapped=new ushort[4];for(int i=0;i<4;i++){int id=b[2+i];if(id!=0&&!moves.TryGetValue(id,out mapped[i]))throw new InvalidOperationException($"Unmapped move {id}.");}
                    bool old=nat<=721&&mapped.All(x=>x<=621)&&PersonalTable.XY.IsPresentInGame(nat,0);PKM q=old?new PK6():new PK9();if(!old&&!PersonalTable.SV.IsPresentInGame(nat,0))throw new InvalidOperationException("No verified portable personal data for this species.");var pi=old?(PersonalInfo)PersonalTable.XY.GetFormEntry(nat,0):PersonalTable.SV.GetFormEntry(nat,0);
                    byte nature=(byte)(b[20]&31);if(nature>24)throw new InvalidOperationException("Unsupported nature value.");ushort tid=BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(6,2));uint low=(uint)(tid^native^(b[17]<<8)^b[18]);uint high=low^tid;bool shiny=(b[20]&128)!=0;if(!shiny)high^=0x100;uint pid=(high<<16)|low;
                    q.Species=nat;q.Form=0;q.TID16=tid;q.SID16=0;q.PID=pid;q.EncryptionConstant=pid;q.Language=2;q.OriginalTrainerName=ot;q.OriginalTrainerGender=0;q.Nickname=nick;q.IsNicknamed=!string.Equals(nick,SpeciesName.GetSpeciesNameGeneration(nat,2,old?(byte)6:(byte)9),StringComparison.OrdinalIgnoreCase);q.EXP=(uint)((b[8]<<16)|(b[9]<<8)|b[10]);q.Nature=(Nature)nature;q.StatAlignment=(Nature)nature;q.Gender=pi.Gender==255?(byte)2:(byte)((b[21]&128)!=0?1:0);q.Moves=mapped;q.HeldItem=0;q.Ball=4;q.Version=GameVersion.C;q.MetLocation=0;q.MetLevel=(byte)(b[entry.Party?29:26]&0x7f);q.OriginalTrainerFriendship=b[entry.Party?26:23];
                    int ability=(b[20]>>5)&3;q.RefreshAbility(ability is >=1 and <=3?ability-1:0);
                    int[] dvs={b[17]>>4,b[17]&15,b[18]>>4,b[18]&15,b[19]>>4,b[19]&15};for(int i=0;i<6;i++){q.SetIV(i,dvs[i]*2+1);q.SetEV(i,b[11+i]);}
                    if(entry.Party){q.Move1_PP=b[22]&63;q.Move2_PP=b[23]&63;q.Move3_PP=b[24]&63;q.Move4_PP=b[25]&63;q.Move1_PPUps=b[22]>>6;q.Move2_PPUps=b[23]>>6;q.Move3_PPUps=b[24]>>6;q.Move4_PPUps=b[25]>>6;}else{q.Move1_PPUps=b[22]&3;q.Move2_PPUps=(b[22]>>2)&3;q.Move3_PPUps=(b[22]>>4)&3;q.Move4_PPUps=(b[22]>>6)&3;q.HealPP();}
                    int level=b[entry.Party?31:28];if(level is <1 or >100)throw new InvalidOperationException("Invalid level.");if(q.CurrentLevel!=level)q.EXP=Experience.GetEXP((byte)level,pi.EXPGrowth);if(q.IsShiny!=shiny)throw new InvalidOperationException("Could not preserve shiny state.");q.ResetPartyStats();q.RefreshChecksum();byte[] bytes=new byte[q.SIZE_STORED];q.WriteDecryptedDataStored(bytes);
                    w.WriteString("nickname",nick);w.WriteString("originalTrainer",ot);w.WriteNumber("trainerId",tid);w.WriteNumber("storedLevel",level);w.WriteString("file",entry.Position+(old?".pk6":".pk9"));w.WriteBase64String("bytes",bytes);w.WriteNull("error");
                }catch(Exception e)when(e is InvalidOperationException or ArgumentException or IndexOutOfRangeException){w.WriteString("nickname","");w.WriteNull("file");w.WriteNull("bytes");w.WriteString("error",e.Message);}w.WriteEndObject();
            }
            w.WriteEndArray();w.WriteNumber("skippedUnmapped",skipped);w.WriteEndObject();
        }return Encoding.UTF8.GetString(output.ToArray());
    }
}
