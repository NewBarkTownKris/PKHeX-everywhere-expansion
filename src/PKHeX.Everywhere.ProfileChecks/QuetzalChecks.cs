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
        Console.WriteLine("PASS Quetzal synthetic party converts to PK9");
    }

    public static void Private(string file, bool writeExports = false)
    {
        string original=File.ReadAllText(file);using var source=JsonDocument.Parse(original);
        using var result=JsonDocument.Parse(QuetzalExporter.Export(original));
        if(result.RootElement.GetArrayLength()!=54)throw new Exception("Expected 54 Quetzal records.");
        int boxed=0,hints=0;
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
            if(writeExports)File.WriteAllBytes(Path.Combine(output,entry.GetProperty("file").GetString()!),Convert.FromBase64String(entry.GetProperty("bytes").GetString()!));
        }
        if(boxed!=49||hints!=4)throw new Exception("Unexpected Quetzal box or format-warning count.");
        Console.WriteLine("PASS 54 Quetzal PK9 records, 49 labelled trainer approximations and four format warnings");
        if(writeExports){using var stream=new MemoryStream();using(var writer=new Utf8JsonWriter(stream,new JsonWriterOptions{Indented=true})){
            writer.WriteStartArray();foreach(var entry in result.RootElement.EnumerateArray()){writer.WriteStartObject();foreach(var prop in entry.EnumerateObject()){if(prop.Name=="bytes")continue;prop.WriteTo(writer);}writer.WriteEndObject();}writer.WriteEndArray();}
            File.WriteAllBytes(Path.Combine(output,"metadata.json"),stream.ToArray());}
    }
}
