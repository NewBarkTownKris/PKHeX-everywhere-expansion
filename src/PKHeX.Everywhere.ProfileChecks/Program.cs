using PKHeX.Core;
using PKHeX.Facade;
using PKHeX.Everywhere.RomHacks.Expansion;
using System.Text.Json;
using System.Security.Cryptography;

if(args.Contains("--synthetic-only")){RunSyntheticChecks();return;}
var path=Path.GetFullPath(args.Length>0?args[0]:"../Pokemon Emerald - Joule.sav");
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
foreach(var m in mons){
 var standard=m.ToPortable();var output=new byte[standard.SIZE_STORED];standard.WriteDecryptedDataStored(output);
 var reopened=EntityFormat.GetFromBytes(output,standard.Context)!;
 Require(reopened.ChecksumValid&&reopened.Species==m.Species&&reopened.ID32==m.ID32&&reopened.Nickname==m.Nickname&&reopened.IsShiny==m.IsShiny,"portable identity");
 Require(reopened.Moves.AsSpan().SequenceEqual(m.Moves),"portable moves");
 var damaged=m.Clone();damaged.Data[48]^=1;Require(!damaged.ChecksumValid,"negative native checksum check");
}
var mappedMarks=mons[0].Clone();mappedMarks.Set("outer.markings",5);var marked=(PK6)mappedMarks.ToPortable();
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
}
Console.WriteLine("PASS collection ZIP: 9 standard files, 9 native records, complete metadata");
Require(hash.AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))),"original save untouched");
Console.WriteLine("PASS source save unchanged");
static void Require(bool pass,string what){if(!pass)throw new Exception("FAIL: "+what);}

static void RunSyntheticChecks(){
foreach(var p in SourceProfile.All){
 var fixture=SyntheticSave.Create(p);var loaded=new NativeSave(p,fixture);
 Require(loaded.BoxCount==p.At("TOTAL_BOXES_COUNT"),"profile box count");Require(loaded.GetPartySlotAtIndex(0).Species==82,"profile party decoded");
 Require(loaded.GetBoxSlotAtIndex(loaded.BoxCount*30-1).Species==82,"last box/last slot decoded");
 Require(fixture.AsSpan().SequenceEqual(loaded.Write().Span),"synthetic profile no-op "+p.Id);
 var mon=(NativePokemon)loaded.GetBoxSlotAtIndex(loaded.BoxCount*30-1);mon.Nickname="LAST-BOX";mon.IV_HP=17;loaded.SetBoxSlotAtIndex(mon,loaded.BoxCount*30-1);
 var updated=new NativeSave(p,loaded.Write().ToArray());var last=updated.GetBoxSlotAtIndex(loaded.BoxCount*30-1);
 Require(last.Nickname=="LAST-BOX"&&last.IV_HP==17,"last box edit/reload "+p.Id);
 Require(updated.GetPartySlotAtIndex(0).Nickname=="CHECK","party retained during box edit");
 Console.WriteLine("PASS synthetic no-op and last-box edit: "+p.Id+" ("+loaded.BoxCount+" boxes)");
}
}
