using PKHeX.Core;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PKHeX.Everywhere.RomHacks.Quetzal;
public static class QuetzalExporter
{
public static string Export(string inspection)
{
using var input = JsonDocument.Parse(inspection);
using var stream = typeof(QuetzalExporter).Assembly.GetManifestResourceStream("quetzal.9v1.json") ?? throw new InvalidOperationException("Quetzal source tables unavailable.");
using var tables = JsonDocument.Parse(stream);
var root = input.RootElement;var data = tables.RootElement;var species = data.GetProperty("species");var moves = data.GetProperty("moves");var items = data.GetProperty("items");var abilities = data.GetProperty("abilities");
string Key(string? s) => Regex.Replace((s ?? "").Normalize(System.Text.NormalizationForm.FormD), "[^a-zA-Z0-9]", "").ToUpperInvariant();
var moveNames = GameInfo.Strings.movelist.Select((v,i)=>(Name:Key(v),Id:i)).Where(x=>x.Name.Length>0).DistinctBy(x=>x.Name).ToDictionary(x=>x.Name,x=>x.Id);
var itemNames = GameInfo.Strings.itemlist.Select((v,i)=>(Name:Key(v),Id:i)).Where(x=>x.Name.Length>0).DistinctBy(x=>x.Name).ToDictionary(x=>x.Name,x=>x.Id);
var abilityNames = Enum.GetValues<Ability>().Distinct().ToDictionary(x=>Key(x.ToString()),x=>(int)x);
var ballNames = Enum.GetValues<Ball>().Distinct().ToDictionary(x=>Key(x.ToString()),x=>(byte)x);
string TrainerName() => StringConverter3.GetString(Convert.FromHexString(root.GetProperty("trainerNameRaw").GetString()!),false);
int N(JsonElement e,string key) => e.GetProperty(key).GetInt32();
int MapName(JsonElement table,int id,Dictionary<string,int> map,string kind){if(id==0)return 0;if(id<0||id>=table.GetArrayLength())throw new InvalidOperationException($"Unknown {kind} #{id}");string name=table[id].GetString()??"";if(!map.TryGetValue(Key(name),out int value))throw new InvalidOperationException($"Unsupported {kind}: {name} (#{id})");return value;}
(byte Value, bool Approximate) MapBall(int id){string[] names={"Strange","Poke","Great","Ultra","Master","Premier","Heal","Net","Nest","Dive","Dusk","Timer","Quick","Repeat","Luxury","Level","Lure","Moon","Friend","Love","Fast","Heavy","Dream","Safari","Sport","","Beast","Cherish"};return id>=0&&id<names.Length&&ballNames.TryGetValue(Key(names[id]),out var ball)?(ball,false):((byte)Ball.Great,true);}
byte Form(int native,ushort national){
 if(native<=898)return 0;
 var row=species[native];var stats=row.GetProperty("stats").EnumerateArray().Select(x=>x.GetInt32()).ToArray();var types=row.GetProperty("types").EnumerateArray().Select(x=>x.GetInt32()>=10?x.GetInt32()-1:x.GetInt32()).ToArray();
 var matches=new List<byte>();var baseInfo=PersonalTable.SV[national];
 for(byte f=0;f<Math.Max(1,(int)baseInfo.FormCount);f++){
  var pi=PersonalTable.SV.GetFormEntry(national,f);
  if(pi.HP==stats[0]&&pi.ATK==stats[1]&&pi.DEF==stats[2]&&pi.SPE==stats[3]&&pi.SPA==stats[4]&&pi.SPD==stats[5]&&pi.Type1==types[0]&&pi.Type2==types[1])matches.Add(f);
 }
 if(matches.Count>1){
  var nativeAbilities=row.GetProperty("abilities").EnumerateArray().Select(x=>x.GetInt32()).Where(x=>x!=0).Select(x=>MapName(abilities,x,abilityNames,"ability")).ToHashSet();
  matches=matches.Where(f=>{var pi=PersonalTable.SV.GetFormEntry(national,f);return nativeAbilities.SetEquals(new[]{pi.Ability1,pi.Ability2,pi.AbilityH}.Where(a=>a!=0));}).ToList();
 }
 if(matches.Count!=1)throw new InvalidOperationException($"Species #{native}: form cannot be resolved uniquely ({matches.Count} matches)");return matches[0];
}
ushort Location(PK9 mon){
 var chain=new[]{new EvoCriteria{Species=mon.Species,Form=mon.Form,LevelMin=1,LevelMax=100}};
 var candidates=EncounterGenerator9.Instance.GetPossible(mon,chain,GameVersion.SL,EncounterTypeGroup.Slot).OfType<EncounterSlot9>().Where(x=>x.Form==mon.Form).OrderBy(x=>x.Location<130?0:1).ThenBy(x=>x.LevelMin).ThenBy(x=>x.Location);
 return candidates.FirstOrDefault()?.Location??0;
}
var report=new List<Result>();
foreach(var (record,party) in root.GetProperty("party").EnumerateArray().Select(e=>(e,true)).Concat(root.GetProperty("pc").EnumerateArray().Select(e=>(e,false)))){
 string position=party?$"Party {N(record,"slot"):D2}":$"Box {N(record,"box"):D2} Slot {N(record,"slot"):D2}";
 try{
  var raw=Convert.FromHexString(record.GetProperty("raw").GetString()!);int native=N(record,"nativeSpecies");var sp=species[native];int nat=N(sp,"national");if(nat is <1 or >1025)throw new InvalidOperationException($"Custom species #{native} has no standard PK9 equivalent");
  ushort dex=(ushort)nat;byte form=Form(native,dex);int[] rawMoves=record.GetProperty("moves").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
  var mapped=rawMoves.Select(x=>(ushort)MapName(moves,x,moveNames,"move")).ToArray();
  int slot=party?(int)(BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(0x54))>>28)&3:N(record,"abilitySlot");
  if(slot>2)throw new InvalidOperationException("Unsupported native ability slot");
  int abilityNative=sp.GetProperty("abilities")[slot].GetInt32();if(abilityNative==0)abilityNative=sp.GetProperty("abilities")[0].GetInt32();
  int ability=MapName(abilities,abilityNative,abilityNames,"ability");
  int item=party?BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(0x2a)):N(record,"nativeItem");
  int ball=party?raw[0x32]:N(record,"nativeBall");
  var mappedBall=MapBall(ball);
  uint nativePid=party?BinaryPrimitives.ReadUInt32LittleEndian(raw):BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(raw));
  uint trainer=party?BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4)):root.GetProperty("trainerId").GetUInt32();
  int nature=party?(int)((nativePid&255)%25):N(record,"nature");bool shiny=party?(raw[0x13]&8)!=0:record.GetProperty("shiny").GetBoolean();
  string nickname=StringConverter3.GetString(raw.AsSpan(party?8:21,10),false);string ot=party?StringConverter3.GetString(raw.AsSpan(20,7),false):TrainerName();
  bool otGenderKnown=trainer==root.GetProperty("trainerId").GetUInt32();
  var mon=new PK9{Species=dex,Form=form,Version=GameVersion.SL,Language=2,Nickname=nickname,OriginalTrainerName=ot,ID32=trainer,OriginalTrainerGender=otGenderKnown?(byte)N(root,"trainerGender"):(byte)0,PID=nativePid,EncryptionConstant=nativePid,Nature=(Nature)nature,StatAlignment=(Nature)nature,Ability=ability,AbilityNumber=1<<slot,HeldItem=MapName(items,item,itemNames,"item"),Ball=mappedBall.Value};
  mon.OriginalTrainerFriendship=party?raw[0x31]:mon.PersonalInfo.BaseFriendship;
  mon.IsNicknamed=!nickname.Equals(SpeciesName.GetSpeciesNameGeneration(dex,2,9),StringComparison.OrdinalIgnoreCase);
  uint low=mon.PID&65535;uint xor=(mon.PID>>16)^low^mon.TID16^mon.SID16;
  if(shiny!=(xor<16))mon.PID=shiny?low|((low^(uint)mon.TID16^(uint)mon.SID16)<<16):mon.PID^0x10000000;
  int[] ev=party?raw.AsSpan(0x40,6).ToArray().Select(x=>(int)x).ToArray():record.GetProperty("evs").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
  uint packed=party?BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(0x50)):0;int[] iv=party?Enumerable.Range(0,6).Select(i=>(int)(packed>>(i*5))&31).ToArray():record.GetProperty("ivs").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
  mon.Moves=mapped;for(int i=0;i<6;i++){mon.SetIV(i,iv[i]);mon.SetEV(i,ev[i]);}
  if(party){mon.Move1_PP=raw[0x3c];mon.Move2_PP=raw[0x3d];mon.Move3_PP=raw[0x3e];mon.Move4_PP=raw[0x3f];mon.Move1_PPUps=raw[0x30]&3;mon.Move2_PPUps=(raw[0x30]>>2)&3;mon.Move3_PPUps=(raw[0x30]>>4)&3;mon.Move4_PPUps=(raw[0x30]>>6)&3;}
  else{mon.Move1_PP=MoveInfo.GetPP(EntityContext.Gen9,mapped[0]);mon.Move2_PP=MoveInfo.GetPP(EntityContext.Gen9,mapped[1]);mon.Move3_PP=MoveInfo.GetPP(EntityContext.Gen9,mapped[2]);mon.Move4_PP=MoveInfo.GetPP(EntityContext.Gen9,mapped[3]);}
  int level=party?N(record,"level"):LevelFromExp(N(record,"experienceApproximate"));mon.CurrentLevel=(byte)level;mon.ObedienceLevel=(byte)level;mon.MetLevel=(byte)level;mon.MetLocation=Location(mon);mon.Gender=Gender(mon,sp,party,nativePid,record);
  mon.ResetPartyStats();mon.RefreshChecksum();byte[] output=new byte[mon.SIZE_STORED];mon.WriteDecryptedDataStored(output);string filename=position+" - "+string.Concat(nickname.Where(char.IsLetterOrDigit))+".pk9";
  bool needsFormatHint=EntityFormat.GetFromBytes(output.ToArray(),EntityContext.Gen9) is not PK9;
  report.Add(new Result(position,nickname,filename,Convert.ToBase64String(output),null,native,dex,form,mon.MetLocation==0?null:GameInfo.GetLocationName(false,mon.MetLocation,mon.Format,mon.Generation,mon.Version),ball,mappedBall.Approximate,!party,!otGenderKnown,!party,mon.PID!=nativePid,!party,needsFormatHint,record));
 }catch(Exception e){report.Add(new Result(position,"",null,null,e.Message,0,0,0,null,null,false,!party,false,!party,false,!party,false,record));}
}
using var outputStream = new MemoryStream();
using(var writer=new Utf8JsonWriter(outputStream)){
 writer.WriteStartArray();foreach(var r in report){writer.WriteStartObject();writer.WriteString("position",r.Position);writer.WriteString("nickname",r.Nickname);
 writer.WriteString("file",r.File);writer.WriteString("bytes",r.Bytes);writer.WriteString("error",r.Error);writer.WriteNumber("sourceSpecies",r.SourceSpecies);writer.WriteNumber("national",r.National);writer.WriteNumber("form",r.Form);writer.WriteString("location",r.Location);writer.WriteBoolean("locationApproximate",true);if(r.NativeBall is int nativeBall)writer.WriteNumber("nativeBallId",nativeBall);else writer.WriteNull("nativeBallId");writer.WriteBoolean("ballApproximate",r.BallApproximate);writer.WriteBoolean("trainerApproximate",r.TrainerApproximate);writer.WriteBoolean("trainerGenderApproximate",r.TrainerGenderApproximate);writer.WriteBoolean("pidApproximate",r.PidApproximate);writer.WriteBoolean("pidAdjusted",r.PidAdjusted);writer.WriteBoolean("experienceApproximate",true);writer.WriteBoolean("friendshipApproximate",r.TrainerApproximate);writer.WriteBoolean("metLevelApproximate",true);writer.WriteBoolean("ppApproximate",r.PpApproximate);writer.WriteBoolean("formatHintRequired",r.FormatHintRequired);writer.WritePropertyName("source");r.Source.WriteTo(writer);writer.WriteEndObject();}writer.WriteEndArray();}
return System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
}

static int LevelFromExp(int exp){int result=1;for(int n=2;n<=100;n++){int value=6*n*n*n/5-15*n*n+100*n-140;if(value>exp)break;result=n;}return result;}
static byte Gender(PK9 mon,JsonElement info,bool party,uint pid,JsonElement record){int ratio=info.GetProperty("gender").GetInt32();if(ratio==255)return 2;if(ratio==254)return 1;if(ratio==0)return 0;return party?(byte)((pid&255)<ratio?1:0):(byte)record.GetProperty("femaleBit").GetInt32();}

private sealed record Result(string Position,string Nickname,string? File,string? Bytes,string? Error,int SourceSpecies,int National,int Form,string? Location,int? NativeBall,bool BallApproximate,bool TrainerApproximate,bool TrainerGenderApproximate,bool PidApproximate,bool PidAdjusted,bool PpApproximate,bool FormatHintRequired,JsonElement Source);
}
