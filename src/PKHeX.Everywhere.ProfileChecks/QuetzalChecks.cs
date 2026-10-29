using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Quetzal;
using System.Buffers.Binary;
using System.Text.Json;

static class QuetzalChecks
{
    public static void Synthetic()
    {
        byte[] raw=new byte[104];StringConverter3.SetString(raw.AsSpan(8,10),"CHARMANDER",10,false,StringConverterOption.ClearFF);
        StringConverter3.SetString(raw.AsSpan(20,7),"TESTER",7,false,StringConverterOption.ClearFF);
        BinaryPrimitives.WriteUInt32LittleEndian(raw,123);BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(4),0x11223344);
        BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(40),4);BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(52),33);raw[0x32]=1;raw[0x3c]=35;raw[0x58]=5;
        var fixture=new {trainerNameRaw=Convert.ToHexString(raw.AsSpan(20,7)),trainerGender=0,trainerId=0x11223344,party=new[]{new{slot=1,nativeSpecies=4,level=5,moves=new[]{33,0,0,0},raw=Convert.ToHexString(raw)}},pc=Array.Empty<object>()};
        using var result=JsonDocument.Parse(QuetzalExporter.Export(JsonSerializer.Serialize(fixture)));var entry=result.RootElement[0];
        if(entry.GetProperty("error").ValueKind!=JsonValueKind.Null)throw new Exception("Quetzal conversion failed: "+entry.GetProperty("error"));
        var pk=new PK9(Convert.FromBase64String(entry.GetProperty("bytes").GetString()!));
        if(!pk.ChecksumValid||pk.Species!=4||pk.Move1!=33||pk.Version!=GameVersion.SL||pk.OriginalTrainerName!="TESTER")throw new Exception("Quetzal PK9 conversion mismatch.");
        foreach(var (native,expected,approximate) in new[]{(1,Ball.Poke,false),(2,Ball.Great,false),(25,Ball.Great,true),(63,Ball.Great,true),(0,Ball.Strange,false)}){
            raw[0x32]=(byte)native;
            var checkFixture=new {trainerNameRaw=Convert.ToHexString(raw.AsSpan(20,7)),trainerGender=0,trainerId=0x11223344,party=new[]{new{slot=1,nativeSpecies=4,level=5,moves=new[]{33,0,0,0},raw=Convert.ToHexString(raw)}},pc=Array.Empty<object>()};
            using var check=JsonDocument.Parse(QuetzalExporter.Export(JsonSerializer.Serialize(checkFixture)));var converted=check.RootElement[0];
            var mon=new PK9(Convert.FromBase64String(converted.GetProperty("bytes").GetString()!));
            if(mon.Ball!=(byte)expected||converted.GetProperty("ballApproximate").GetBoolean()!=approximate||converted.GetProperty("nativeBallId").GetInt32()!=native)throw new Exception("Quetzal ball mapping mismatch for "+native);
        }
        foreach(var (shiny,radiant) in new[]{(false,false),(true,false),(false,true),(true,true)}){
            raw[0x13]=(byte)((shiny?8:0)|(radiant?16:0));
            byte[] boxRaw=new byte[31];boxRaw[5]=(byte)((shiny?0x10:0)|(radiant?0x20:0));
            StringConverter3.SetString(boxRaw.AsSpan(21,10),"CHARMANDER",10,false,StringConverterOption.ClearFF);
            var cases=new {trainerNameRaw=Convert.ToHexString(raw.AsSpan(20,7)),trainerGender=0,trainerId=0x11223344,
                party=new[]{new{slot=1,nativeSpecies=4,level=5,moves=new[]{33,0,0,0},raw=Convert.ToHexString(raw)}},
                pc=new[]{new{box=1,slot=1,nativeSpecies=4,nativeItem=0,experienceApproximate=100,nativeBall=1,shiny,moves=new[]{33,0,0,0},evs=new int[6],ivs=new int[6],femaleBit=0,nature=0,abilitySlot=0,raw=Convert.ToHexString(boxRaw)}}};
            using var check=JsonDocument.Parse(QuetzalExporter.Export(JsonSerializer.Serialize(cases)));
            foreach(var converted in check.RootElement.EnumerateArray()){
                if(converted.GetProperty("error").ValueKind!=JsonValueKind.Null)throw new Exception("Radiant fixture failed: "+converted.GetProperty("error"));
                var mon=new PK9(Convert.FromBase64String(converted.GetProperty("bytes").GetString()!));
                if(!mon.ChecksumValid||mon.IsShiny!=(shiny||radiant)||converted.GetProperty("radiantConvertedToShiny").GetBoolean()!=radiant)throw new Exception("Quetzal radiant conversion mismatch.");
            }
        }
        Console.WriteLine("PASS Quetzal synthetic party converts to PK9");
    }

    public static void Private(string file, bool writeExports = false)
    {
        string original=File.ReadAllText(file);using var source=JsonDocument.Parse(original);
        using var result=JsonDocument.Parse(QuetzalExporter.Export(original));
        if(result.RootElement.GetArrayLength()!=54)throw new Exception("Expected 54 Quetzal records.");
        int boxed=0,hints=0,approximatedBalls=0,radiants=0;
        string output="../quetzal-work/final-output";
        if(writeExports)Directory.CreateDirectory(output);
        foreach(var entry in result.RootElement.EnumerateArray()){
            if(entry.GetProperty("error").ValueKind!=JsonValueKind.Null)throw new Exception("Quetzal conversion error: "+entry.GetProperty("error"));
            var mon=new PK9(Convert.FromBase64String(entry.GetProperty("bytes").GetString()!));
            if(!mon.ChecksumValid||mon.Species!=entry.GetProperty("national").GetInt32()||mon.Form!=entry.GetProperty("form").GetInt32())throw new Exception("Quetzal exported PK9 mismatch.");
            bool box=entry.GetProperty("position").GetString()!.StartsWith("Box ",StringComparison.Ordinal);
            if(entry.GetProperty("trainerApproximate").GetBoolean()!=box)throw new Exception("Quetzal trainer approximation was not labelled.");
            if(box){boxed++;if(mon.ID32!=source.RootElement.GetProperty("trainerId").GetUInt32())throw new Exception("Box trainer approximation mismatch.");}
            if(entry.GetProperty("formatHintRequired").GetBoolean())hints++;
            if(entry.GetProperty("ballApproximate").GetBoolean()){approximatedBalls++;if(mon.Ball!=(byte)Ball.Great)throw new Exception("Custom ball was not mapped to a Great Ball.");}
            if(entry.GetProperty("radiantConvertedToShiny").GetBoolean()){radiants++;if(!mon.IsShiny)throw new Exception("Radiant Pokémon did not export as shiny.");}
            if(entry.GetProperty("position").GetString()=="Box 02 Slot 13"&&(!entry.GetProperty("radiantConvertedToShiny").GetBoolean()||!mon.IsShiny))throw new Exception("Yamato was not exported as shiny.");
            if(writeExports)File.WriteAllBytes(Path.Combine(output,entry.GetProperty("file").GetString()!),Convert.FromBase64String(entry.GetProperty("bytes").GetString()!));
        }
        if(boxed!=49||hints!=4||approximatedBalls!=9||radiants!=2)throw new Exception("Unexpected Quetzal box, ball, Radiant or format-warning count.");
        Console.WriteLine("PASS 54 Quetzal PK9 records, nine Great Ball substitutions, 49 labelled trainer approximations and four format warnings");
        if(writeExports){using var stream=new MemoryStream();using(var writer=new Utf8JsonWriter(stream,new JsonWriterOptions{Indented=true})){
            writer.WriteStartArray();foreach(var entry in result.RootElement.EnumerateArray()){writer.WriteStartObject();foreach(var prop in entry.EnumerateObject()){if(prop.Name=="bytes")continue;prop.WriteTo(writer);}writer.WriteEndObject();}writer.WriteEndArray();}
            File.WriteAllBytes(Path.Combine(output,"metadata.json"),stream.ToArray());}
    }
}
