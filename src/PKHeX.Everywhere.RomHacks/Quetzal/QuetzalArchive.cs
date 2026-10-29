using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace PKHeX.Everywhere.RomHacks.Quetzal;

public static class QuetzalArchive
{
    public static byte[] Create(string results)
    {
        using var data = JsonDocument.Parse(results);
        using var archive = new MemoryStream();
        using var metadata = new MemoryStream();
        using (var zip = new ZipArchive(archive, ZipArchiveMode.Create, true))
        {
            using (var writer = new Utf8JsonWriter(metadata, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartArray();
                foreach (var entry in data.RootElement.EnumerateArray())
                {
                    if (entry.GetProperty("file").ValueKind == JsonValueKind.String && entry.GetProperty("bytes").ValueKind == JsonValueKind.String)
                        Put(zip, entry.GetProperty("file").GetString()!, Convert.FromBase64String(entry.GetProperty("bytes").GetString()!));
                    writer.WriteStartObject();
                    foreach (var property in entry.EnumerateObject())
                        if (property.Name != "bytes") property.WriteTo(writer);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            Put(zip, "Metadata.json", metadata.ToArray());
            Put(zip, "READ ME.txt", Encoding.UTF8.GetBytes("Quetzal PK9 exports. Unsupported Pokémon are omitted from PK9 files and listed in Metadata.json with an error. Radiant Pokémon become standard shinies; their custom palettes cannot be retained. Original records and approximation flags are in the metadata. Box trainer identities and PIDs, encounter locations and other fields may be approximated. These exports do not claim a legal retail encounter.\n"));
        }
        return archive.ToArray();
    }

    private static void Put(ZipArchive zip, string name, byte[] data)
    {
        using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open();
        entry.Write(data);
    }
}
