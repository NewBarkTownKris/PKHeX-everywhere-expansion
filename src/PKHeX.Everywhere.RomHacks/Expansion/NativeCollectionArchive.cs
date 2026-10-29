using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Facade;
namespace PKHeX.Everywhere.RomHacks.Expansion;

public static class NativeCollectionArchive
{
    public static byte[] Export(Game game)
    {
        _ = game.ToByteArray();
        if (game.SaveFile is not NativeSave save) throw new InvalidOperationException("Open a source-profile save first.");
        using var stream = new MemoryStream(); using var metadata = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            using (var writer = new Utf8JsonWriter(metadata, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject(); writer.WriteString("profile", save.Profile.Id); writer.WriteStartArray("pokemon");
                var members = Enumerable.Range(0, save.PartyCount).Select(i => (Position: $"Party {i + 1:D2}", Mon: (NativePokemon)save.GetPartySlotAtIndex(i)))
                    .Concat(Enumerable.Range(0, save.BoxCount * 30).Select(i => (Position: $"Box {i / 30 + 1:D2} Slot {i % 30 + 1:D2}", Mon: (NativePokemon)save.GetBoxSlotAtIndex(i)))).Where(x => x.Mon.SpeciesIndex != 0);
                foreach (var (position, mon) in members)
                {
                    string stem = position + " - " + string.Concat(mon.Nickname.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
                    var native = new byte[mon.SIZE_STORED]; mon.WriteEncryptedDataStored(native); Put(zip, "Native Expansion Records/" + stem + ".expmon", native);
                    writer.WriteStartObject(); writer.WriteString("position", position); writer.WriteString("nickname", mon.Nickname); writer.WriteNumber("species", mon.Species); writer.WriteNumber("nativeSpecies", mon.SpeciesIndex); writer.WriteNumber("pid", mon.PID); writer.WriteNumber("otId", mon.ID32); writer.WriteString("ot", mon.OriginalTrainerName); writer.WriteNumber("metLocationId", mon.MetLocation); writer.WriteString("metLocationName", save.Profile.Locations.GetValueOrDefault(mon.MetLocation.ToString())); writer.WriteNumber("originGame", (int)mon.Version); writer.WriteNumber("metLevel", mon.MetLevel); writer.WriteNumber("level", mon.CurrentLevel); writer.WriteBoolean("shiny", mon.IsShiny);
                    try
                    {
                        var portable = mon.ToPortable(); var bytes = new byte[portable.SIZE_STORED]; portable.WriteDecryptedDataStored(bytes);
                        var path = "Portable Pokemon/" + stem + "." + portable.Extension; Put(zip, path, bytes);
                        writer.WriteString("portablePath", path); writer.WriteNumber("portablePid", portable.PID); writer.WriteNumber("portableLocation", portable.MetLocation); writer.WriteNumber("portableOrigin", (int)portable.Version);
                    }
                    catch (InvalidOperationException e) { writer.WriteString("portableError", e.Message); }
                    writer.WriteStartObject("fields"); foreach (var field in save.Profile.BitFields.Keys) writer.WriteNumber(field, mon.Read(field)); writer.WriteEndObject(); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            Put(zip, "Metadata.json", metadata.ToArray());
            Put(zip, "READ ME.txt", Encoding.UTF8.GetBytes("Portable Pokemon contains standard PK6 or PK9 files. Native Expansion Records and Metadata.json preserve the editor's native records, original PID, location IDs/names and all packed fields. Custom Johto/Kanto locations have no direct PK6 met-location equivalent. Forced shininess may require a different portable PID without changing the original trainer. The save itself was not converted to HG/SS. Native records require the matching source profile and are not standard PK3 files.\n"));
        }
        return stream.ToArray();
    }
    private static void Put(ZipArchive zip, string name, byte[] bytes) { using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open(); entry.Write(bytes); }
}
