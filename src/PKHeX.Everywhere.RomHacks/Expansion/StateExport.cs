using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Expansion;

public static class StateExport
{
    public static string Inspect(byte[] bytes)
    {
        var state = MgbaState.Read(bytes);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject(); w.WriteString("profile", state.Game.Profile); w.WriteString("trainer", state.Trainer);
            w.WriteString("romCrc", state.Game.RomCrc.ToString("X8")); w.WriteStartArray("pokemon");
            foreach (var entry in state.Members)
            {
                var mon = entry.Pokemon;
                string stem = entry.Position + " - " + string.Concat(mon.Nickname.Where(char.IsLetterOrDigit));
                w.WriteStartObject(); w.WriteString("position", entry.Position); w.WriteString("nickname", mon.Nickname);
                w.WriteString("nativeFile", stem + ".expmon"); w.WriteBase64String("nativeBytes", entry.Original.AsSpan(0, mon.SIZE_STORED));
                w.WriteNumber("nativeSpecies", mon.SpeciesIndex); w.WriteNumber("species", mon.Species); w.WriteNumber("form", mon.Form);
                w.WriteString("originalTrainer", mon.OriginalTrainerName); w.WriteNumber("trainerId", mon.ID32); w.WriteNumber("pid", mon.PID);
                w.WriteNumber("nativeMetLocation", mon.MetLocation); w.WriteString("nativeLocation", mon.Profile.Locations.GetValueOrDefault(mon.MetLocation.ToString()));
                w.WriteString("nativeOrigin", mon.Version.ToString()); w.WriteBoolean("shiny", mon.IsShiny);
                try
                {
                    if (mon.Species == 0 || Enumerable.Range(1, 4).Any(i => mon.Read("move" + i) != 0 && mon.GetMove(i - 1) == 0) ||
                        mon.HeldItemIndex != 0 && mon.HeldItem == 0 || mon.Read("pokeball") != 0 && mon.Ball == 0)
                        throw new InvalidOperationException("A species, move, item or ball has no known standard mapping.");
                    var portable = mon.ToPortable(); byte[] data = new byte[portable.SIZE_STORED]; portable.WriteDecryptedDataStored(data);
                    w.WriteString("file", stem + "." + portable.Extension); w.WriteBase64String("bytes", data);
                    w.WriteString("portableLocation", NativeLocations.DisplayLocation(portable));
                    w.WriteBoolean("pidAdjusted", portable.PID != mon.PID); w.WriteNull("error");
                }
                catch (Exception e) when (e is InvalidOperationException or ArgumentException or IndexOutOfRangeException)
                {
                    w.WriteNull("file"); w.WriteNull("bytes"); w.WriteNull("portableLocation"); w.WriteBoolean("pidAdjusted", false); w.WriteString("error", e.Message);
                }
                w.WriteEndObject();
            }
            w.WriteEndArray(); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static byte[] Archive(string json)
    {
        using var root = JsonDocument.Parse(json); using var output = new MemoryStream(); using var metadata = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            using (var w = new Utf8JsonWriter(metadata, new JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                foreach (var property in root.RootElement.EnumerateObject())
                {
                    if (property.Name != "pokemon") { property.WriteTo(w); continue; }
                    w.WriteStartArray("pokemon");
                    foreach (var mon in property.Value.EnumerateArray())
                    {
                        Put(zip, "Native/" + mon.GetProperty("nativeFile").GetString(), Convert.FromBase64String(mon.GetProperty("nativeBytes").GetString()!));
                        if (mon.GetProperty("file").ValueKind == JsonValueKind.String)
                            Put(zip, "Portable/" + mon.GetProperty("file").GetString(), Convert.FromBase64String(mon.GetProperty("bytes").GetString()!));
                        w.WriteStartObject(); foreach (var p in mon.EnumerateObject()) if (p.Name is not ("nativeBytes" or "bytes")) p.WriteTo(w); w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                w.WriteEndObject();
            }
            Put(zip, "Metadata.json", metadata.ToArray());
            bool gb = root.RootElement.TryGetProperty("sourceType", out var source) && source.GetString() is "mGBA Game Boy party" or "Game Boy battery save";
            Put(zip, "READ ME.txt", Encoding.UTF8.GetBytes(gb ? "Read-only Game Boy Pokémon export. See Metadata.json for source, slot positions, skipped records and limitations. Game Boy generations may not record capture locations or balls; no location is invented.\n" : "Read-only export from the live party and PC in an mGBA state. Portable files use the project's origin and location mapping and may change the origin or PID. Native records and metadata retain the source data. Pokémon with unsupported mappings have a native record and an error, but no portable file. This is not a claim of retail encounter legality.\n"));
        }
        return output.ToArray();
    }

    private static void Put(ZipArchive zip, string name, byte[] data)
    {
        using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open(); entry.Write(data);
    }
}
