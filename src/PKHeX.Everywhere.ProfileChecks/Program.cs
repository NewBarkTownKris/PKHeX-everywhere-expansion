using PKHeX.Core;
using PKHeX.Facade;
using PKHeX.Everywhere.RomHacks.Expansion;
using System.Text.Json;
using System.Security.Cryptography;

if(args.Contains("--quetzal-private")){QuetzalChecks.Private("../quetzal-work/sample-inspection.json",args.Contains("--write-quetzal-private"));return;}
if(args.Contains("--state-sample")){
 var samplePath=args[Array.IndexOf(args,"--state-sample")+1];var snapshot=MgbaState.Read(File.ReadAllBytes(samplePath));
 Console.WriteLine($"{snapshot.Game.Profile} trainer={snapshot.Trainer} records={snapshot.Members.Count}");
 foreach(var x in snapshot.Members)Console.WriteLine($"{x.Position}: {x.Pokemon.Nickname} species={x.Pokemon.SpeciesIndex} national={x.Pokemon.Species}");
 return;
}
if(args.Contains("--state-fixtures")){
 foreach(var (name,id,box) in new[]{("vanilla","pokeemerald-expansion-1-17-1","Box 14 Slot 30"),("hns","heart-and-soul-2.0.6","Box 14 Slot 30"),("soulgold","soulgold-1.2.1","Box 21 Slot 30")}){
  byte[] input=File.ReadAllBytes($"../state-support-work/fixtures/{name}/live-synthetic.state");var state=MgbaState.Read(input);
  Require(state.Game.Profile==id&&state.Members.Count==2&&state.Members[0].Position=="Party 01"&&state.Members[1].Position==box,"state fixture positions "+id);
  foreach(var record in state.Members)Require(record.Pokemon.Species==82&&record.Pokemon.Nickname=="CHECK"&&record.Pokemon.ID32==0x11223344&&record.Pokemon.Valid,"state fixture identity "+id);
  using var result=JsonDocument.Parse(StateExport.Inspect(input));foreach(var mon in result.RootElement.GetProperty("pokemon").EnumerateArray())Require(mon.GetProperty("error").ValueKind==JsonValueKind.Null,"state fixture portable export "+id);
  var stateEdited=input.ToArray();int nameOffset=0x21000+(int)(state.Game.Party-0x02000000)+8;stateEdited[nameOffset]=0xBB;
  Require(MgbaState.Read(stateEdited).Members[0].Pokemon.Nickname!=state.Members[0].Pokemon.Nickname,"reads live RAM, not saved flash");
  var damaged=input.ToArray();int recordOffset=nameOffset-8;var stateProfile=SourceProfile.Find(id);
  if(stateProfile.Compact){int bit=stateProfile.BitFields["outer.reservedOuter"][0];damaged[recordOffset+bit/8]|=(byte)(1<<(bit%8));}else damaged[recordOffset+32]^=1;
  bool rejected=false;try{MgbaState.Read(damaged);}catch(InvalidDataException){rejected=true;}Require(rejected,"reject invalid Pokémon record");
  var unknown=input.ToArray();unknown[8]^=0x80;rejected=false;try{MgbaState.Read(unknown);}catch(InvalidDataException){rejected=true;}Require(rejected,"reject unknown ROM build");
  Console.WriteLine("PASS state fixture: "+id+" player, last box, live edits, corruption and ROM identity");
 }
 return;
}
if(args.Contains("--state-private")){
 var state=MgbaState.Read(File.ReadAllBytes("../pokemon-export-work/completed-save.state"));
 var stateSave=new NativeSave(SourceProfile.Find("rocket-hoenn"),File.ReadAllBytes("../Pokemon Emerald - Joule.sav"));
 var expected=Enumerable.Range(0,stateSave.PartyCount).Select(stateSave.GetPartySlotAtIndex).Concat(stateSave.BoxData.Where(x=>x.Species!=0)).Cast<NativePokemon>().ToArray();
 Require(state.Trainer=="Joule"&&state.Members.Count==expected.Length,"state live party and boxes");
 for(int i=0;i<expected.Length;i++){
  var actual=state.Members[i].Pokemon;var wanted=expected[i];
  Require(actual.PID==wanted.PID&&actual.ID32==wanted.ID32&&actual.SpeciesIndex==wanted.SpeciesIndex&&actual.Nickname==wanted.Nickname,"state live identity "+i);
 }
 var pngState=MgbaState.Read(File.ReadAllBytes("../state-support-work/fixtures/rocket-png.state"));
 Require(pngState.Members.Count==state.Members.Count&&pngState.Members[0].Pokemon.PID==state.Members[0].Pokemon.PID,"PNG state agrees with raw state");
 var broken=File.ReadAllBytes("../state-support-work/fixtures/rocket-png.state");broken[^8]^=1;bool rejected=false;try{MgbaState.Read(broken);}catch(InvalidDataException){rejected=true;}Require(rejected,"damaged PNG rejected");
 string exported=StateExport.Inspect(File.ReadAllBytes("../pokemon-export-work/completed-save.state"));using var result=JsonDocument.Parse(exported);
 Require(result.RootElement.GetProperty("pokemon").GetArrayLength()==expected.Length,"state export count");
 foreach(var record in result.RootElement.GetProperty("pokemon").EnumerateArray())
  Require(record.GetProperty("error").ValueKind==JsonValueKind.Null,"state portable export: "+record.GetProperty("nickname")+" "+record.GetProperty("error"));
 using(var zip=new System.IO.Compression.ZipArchive(new MemoryStream(StateExport.Archive(exported)),System.IO.Compression.ZipArchiveMode.Read)){
  Require(zip.Entries.Count(x=>x.FullName.EndsWith(".expmon"))==expected.Length,"native state export count");
  Require(zip.GetEntry("Metadata.json")!=null,"state archive metadata");
 }
 Console.WriteLine($"PASS live mGBA state: {state.Members.Count} Pokémon match Joule save identities");return;
}
if(args.Contains("--write-state-fixtures")){
 var dir="../state-support-work/fixtures";Directory.CreateDirectory(dir);
 foreach(var p in SourceProfile.All){
  var fixture=SyntheticSave.Create(p);File.WriteAllBytes(Path.Combine(dir,p.Id+"-synthetic.sav"),fixture);
  var native=new NativeSave(p,fixture);var party=(NativePokemon)native.GetPartySlotAtIndex(0);var box=(NativePokemon)native.GetBoxSlotAtIndex(native.BoxCount*30-1);
  var partyBytes=new byte[party.SIZE_PARTY];party.WriteEncryptedDataParty(partyBytes);var boxBytes=new byte[box.SIZE_STORED];box.WriteEncryptedDataStored(boxBytes);
  File.WriteAllBytes(Path.Combine(dir,p.Id+"-party.bin"),partyBytes);File.WriteAllBytes(Path.Combine(dir,p.Id+"-box.bin"),boxBytes);
 }
 return;
}
if(args.Contains("--synthetic-only")){RunSyntheticChecks();LocationChecks.Run();QuetzalChecks.Synthetic();StateChecks.Run();VbaChecks.Run();GbStateChecks.Run();PolishedSyntheticChecks.Run();HackRecoveryChecks.Run();return;}
var path=Path.GetFullPath(args.FirstOrDefault(x=>!x.StartsWith("--"))??"../Pokemon Emerald - Joule.sav");
var original=File.ReadAllBytes(path);var hash=SHA256.HashData(original);
Console.WriteLine("Loaded profiles: "+string.Join(", ",SourceProfile.All.Select(x=>x.Id)));
var profile=SourceProfile.Find("rocket-hoenn");
var save=new NativeSave(profile,original);
Require(save.PartyCount==6,"party count");Require(save.OT=="Joule","trainer");
var metadata=JsonDocument.Parse(File.ReadAllText("../Joule Pokemon Exports/Complete Original Metadata.json")).RootElement;
var mons=Enumerable.Range(0,save.PartyCount).Select(save.GetPartySlotAtIndex).Concat(save.BoxData.Where(p=>p.Species!=0)).Cast<NativePokemon>().ToArray();
Require(mons.Length==9,"nine occupied records");
int n=0;
foreach(var m in mons){
 var expected=metadata[n++];var d=expected.GetProperty("native_values");
 int N(string k)=>d.GetProperty("MON_DATA_"+k).GetInt32();
 Require(m.ChecksumValid&&m.Valid,m.Nickname+" native checksum");
 Require(m.Nickname==expected.GetProperty("nickname").GetString(),"nickname");
 Require(m.OriginalTrainerName==expected.GetProperty("original_trainer").GetString(),"OT");
 Require(m.Species==expected.GetProperty("national_species").GetInt32(),"species");
 Require(m.PID==expected.GetProperty("PID").GetUInt32(),"PID");Require(m.ID32==expected.GetProperty("ID32").GetUInt32(),"ID");
 Require(m.CurrentLevel==expected.GetProperty("level").GetInt32(),"level");Require(m.EXP==d.GetProperty("MON_DATA_EXP").GetUInt32(),"EXP");
 Require(m.IsShiny==(N("IS_SHINY")!=0),"shininess");Require(m.Ability==expected.GetProperty("ability").GetInt32(),"ability");
 Require(m.HeldItemIndex==N("HELD_ITEM"),"native held item");Require(m.MetLocation==N("MET_LOCATION"),"met location");
 Require(m.MetLevel==N("MET_LEVEL")&&(int)m.Version==N("MET_GAME"),"origin");Require(m.CurrentFriendship==N("FRIENDSHIP"),"friendship");
 Require((int)m.Nature==N("HIDDEN_NATURE"),"nature");
 for(int i=0;i<6;i++){var key=new[]{"HP","ATK","DEF","SPEED","SPATK","SPDEF"}[i];Require(m.GetIV(i)==N(key+"_IV"),"IV "+key);Require(m.GetEV(i)==N(key+"_EV"),"EV "+key);}
 for(int i=0;i<4;i++){Require(m.GetMove(i)==N("MOVE"+(i+1)),"move");Require(new[]{m.Move1_PP,m.Move2_PP,m.Move3_PP,m.Move4_PP}[i]==N("PP"+(i+1)),"PP");}
 Console.WriteLine("PASS native extraction comparison: "+m.Nickname);
}
var unchanged=save.Write().ToArray();Require(original.AsSpan().SequenceEqual(unchanged),"direct no-op save round-trip");
Console.WriteLine("PASS byte-identical direct save round-trip");
var format=new NativeFormat(profile);var game=Game.LoadFrom(original.ToArray(),"private-test.sav",format);
var facade=game.ToByteArray();Require(original.AsSpan().SequenceEqual(facade),"facade no-op save round-trip");
Console.WriteLine("PASS byte-identical editor/facade round-trip");
var edited=new NativeSave(profile,original);var first=(NativePokemon)edited.GetPartySlotAtIndex(0);first.Nickname="TEST-M4GN1";edited.SetPartySlotAtIndex(first,0);
var bytes=edited.Write().ToArray();var reload=new NativeSave(profile,bytes);Require(reload.GetPartySlotAtIndex(0).Nickname=="TEST-M4GN1","edited nickname persisted");
for(int i=1;i<6;i++)Require(reload.GetPartySlotAtIndex(i).Data.SequenceEqual(save.GetPartySlotAtIndex(i).Data),"other party member unchanged");
for(int i=0;i<save.BoxCount*save.BoxSlotCount;i++)Require(reload.GetBoxSlotAtIndex(i).Data.SequenceEqual(save.GetBoxSlotAtIndex(i).Data),"box record unchanged");
Require(reload.ChecksumsValid,"edited save checksums");
Console.WriteLine("PASS edit/reopen with all unrelated Pokémon unchanged");
RunSyntheticChecks();
LocationChecks.Run();
foreach(var m in mons){
 var standard=m.ToPortable();var output=new byte[standard.SIZE_STORED];standard.WriteDecryptedDataStored(output);
 var reopened=EntityFormat.GetFromBytes(output,standard.Context)!;
 Require(reopened.ChecksumValid&&reopened.Species==m.Species&&reopened.ID32==m.ID32&&reopened.Nickname==m.Nickname&&reopened.IsShiny==m.IsShiny,"portable identity");
 Require(reopened.Moves.AsSpan().SequenceEqual(m.Moves),"portable moves");
 var damaged=m.Clone();damaged.Data[48]^=1;Require(!damaged.ChecksumValid,"negative native checksum check");
}
var mappedMarks=mons[0].Clone();mappedMarks.Set("outer.markings",5);var marked=(IAppliedMarkings4)mappedMarks.ToPortable();
Require(marked.MarkingCircle&&marked.MarkingTriangle&&!marked.MarkingSquare,"Gen3 marking order converted");
var customJson=File.ReadAllText("profiles/rocket-hoenn.json").Replace("\"rocket-hoenn\"","\"private-check-profile\"");
var customProfile=SourceProfile.Parse(customJson);Require(new NativeSave(customProfile,original).Write().Span.SequenceEqual(original),"imported source profile no-op");
var foreign=new NativePokemon();bool refused=false;try{new NativeSave(profile,original).SetPartySlotAtIndex(foreign,0);}catch(InvalidOperationException){refused=true;}Require(refused,"unreviewed reflection imports refused");
Console.WriteLine("PASS all nine portable exports and native corruption negative controls");
var shiny=mons.Single(m=>m.Nickname=="Magenta").Clone();var pid=shiny.PID;
shiny.SetShinyState(false);Require(!shiny.IsShiny&&shiny.PID==pid,"native shiny off without PID change");
shiny.SetShinyState(true);Require(shiny.IsShiny&&shiny.PID==pid,"native shiny on without PID change");
var levels=new NativeSave(profile,original);var levelMon=(NativePokemon)levels.GetPartySlotAtIndex(0);levelMon.CurrentLevel=67;levelMon.IV_HP=31;
levels.SetPartySlotAtIndex(levelMon,0);var levelReload=new NativeSave(profile,levels.Write().ToArray()).GetPartySlotAtIndex(0);
Require(levelReload.CurrentLevel==67&&levelReload.Stat_Level==67&&levelReload.IV_HP==31&&levelReload.Stat_HPMax>0,"level, IV and party stat update");
Console.WriteLine("PASS native shiny toggles preserve PID; level/IV edit rebuilds party stats");
if(args.Contains("--write-test-save")){Directory.CreateDirectory(".local");File.WriteAllBytes(".local/qa-edited.sav",bytes);}
using(var zip=new System.IO.Compression.ZipArchive(new MemoryStream(NativeCollectionArchive.Export(game)),System.IO.Compression.ZipArchiveMode.Read)){
 Require(zip.Entries.Count(e=>e.FullName.EndsWith(".pk6"))==9,"archive has 9 PK6 files");
 Require(zip.Entries.Count(e=>e.FullName.EndsWith(".expmon"))==9,"archive has 9 native records");
 using var json=JsonDocument.Parse(zip.GetEntry("Metadata.json")!.Open());
 Require(json.RootElement.GetProperty("pokemon").GetArrayLength()==9,"archive metadata includes all nine");
 Require(json.RootElement.GetProperty("pokemon")[0].GetProperty("metLocationName").GetString()=="CHERRYGROVE CITY","archive custom location");
 Require(zip.Entries.Count(e=>e.FullName.EndsWith(".pk4"))==1,"archive includes M4GN1 exact-location PK4");
 Require(json.RootElement.GetProperty("pokemon")[0].GetProperty("portableLocationName").GetString()=="Cherrygrove City","archive mapped city");
 Require(json.RootElement.GetProperty("pokemon")[0].GetProperty("modernAlternativeLocationName").GetString()=="the Johto region","archive regional PK6 alternative");
}
Console.WriteLine("PASS collection ZIP: 9 primary files, modern alternative, 9 native records, complete metadata");
var exact=mons[0].ToPortable();var modern=NativePortable.Export(mons[0],preferModern:true);
Require(exact is PK4&&exact.Version==GameVersion.SS&&exact.MetLocation==127&&NativeLocations.DisplayLocation(exact)=="Cherrygrove City","M4GN1 exact HGSS export");
Require(modern is PK6&&modern.Version==GameVersion.SS&&NativeLocations.DisplayLocation(modern)=="the Johto region","M4GN1 regional modern export");
foreach(var p in new[]{exact,modern})Require(p.PID==mons[0].PID&&p.Nature==mons[0].Nature&&p.Ability==mons[0].Ability&&p.MetLevel==40&&p.CurrentLevel==66,"M4GN1 identity retained");
foreach(var m in mons.Skip(1))Require(m.ToPortable().Version==GameVersion.OR&&NativeLocations.DisplayLocation(m.ToPortable()).Length>0,"Hoenn ORAS origin and location");
if(args.Contains("--write-location-exports")){
 string output=".local/location-exports";Directory.CreateDirectory(output);
 foreach(var p in new[]{exact,modern}){var data=new byte[p.SIZE_STORED];p.WriteDecryptedDataStored(data);File.WriteAllBytes(Path.Combine(output,"M4GN1 - "+NativeLocations.DisplayLocation(p)+"."+p.Extension),data);}
 File.WriteAllBytes(Path.Combine(output,"Joule Pokemon - Mapped Origins.zip"),NativeCollectionArchive.Export(game));
 Console.WriteLine("Wrote corrected M4GN1 files and full collection to "+output);
}
Require(hash.AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))),"original save untouched");
Console.WriteLine("PASS source save unchanged");
static void Require(bool pass,string what){if(!pass)throw new Exception("FAIL: "+what);}

static void RunSyntheticChecks(){
foreach(var p in SourceProfile.All){
 var fixture=SyntheticSave.Create(p);
 // Heart & Soul stores challenge and randomizer settings in the 116-byte
 // SaveBlock3 portion of each sector, outside the ordinary sector checksum.
 if(p.Id=="heart-and-soul-2.0.6")
  for(int sector=0;sector<28;sector++) for(int i=0;i<116;i++) fixture[sector*4096+3968+i]=(byte)(sector*37+i*13);
 var loaded=new NativeSave(p,fixture);
 Require(loaded.BoxCount==p.At("TOTAL_BOXES_COUNT"),"profile box count");Require(loaded.GetPartySlotAtIndex(0).Species==82,"profile party decoded");
 Require(loaded.GetBoxSlotAtIndex(loaded.BoxCount*30-1).Species==82,"last box/last slot decoded");
 Require(fixture.AsSpan().SequenceEqual(loaded.Write().Span),"synthetic profile no-op "+p.Id);
 var mon=(NativePokemon)loaded.GetBoxSlotAtIndex(loaded.BoxCount*30-1);mon.Nickname="LAST-BOX";mon.IV_HP=17;loaded.SetBoxSlotAtIndex(mon,loaded.BoxCount*30-1);
 var editedBytes=loaded.Write().ToArray();
 if(p.Id=="heart-and-soul-2.0.6")
  for(int sector=0;sector<28;sector++) Require(fixture.AsSpan(sector*4096+3968,116).SequenceEqual(editedBytes.AsSpan(sector*4096+3968,116)),"HnS randomizer settings preserved in sector "+sector);
 var updated=new NativeSave(p,editedBytes);var last=updated.GetBoxSlotAtIndex(loaded.BoxCount*30-1);
 Require(last.Nickname=="LAST-BOX"&&last.IV_HP==17,"last box edit/reload "+p.Id);
 Require(updated.GetPartySlotAtIndex(0).Nickname=="CHECK","party retained during box edit");
 Console.WriteLine("PASS synthetic no-op and last-box edit: "+p.Id+" ("+loaded.BoxCount+" boxes)");
}
}
