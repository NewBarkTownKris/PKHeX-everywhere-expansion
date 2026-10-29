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
                        writer.WriteString("portableOriginName", portable.Version.ToString()); writer.WriteString("portableRegion", NativeLocations.Resolve(mon).Region); writer.WriteString("portableLocationName", NativeLocations.DisplayLocation(portable));
                        writer.WriteString("locationMapping", portable is PK4 ? "exact HG/SS location" : portable.MetLocation >= 30000 ? "regional fallback" : portable.MetLocation != 0 ? "exact OR/AS location" : "unmapped; exact native location retained in metadata");
                        writer.WriteBoolean("originChanged", portable.Version != mon.Version); writer.WriteBoolean("pidChanged", portable.PID != mon.PID);
                        if (portable is PK4)
                        {
                            var modern = NativePortable.Export(mon, preferModern: true); var modernBytes = new byte[modern.SIZE_STORED]; modern.WriteDecryptedDataStored(modernBytes);
                            string modernPath = "Modern Regional Alternatives/" + stem + "." + modern.Extension; Put(zip, modernPath, modernBytes);
                            writer.WriteString("modernAlternativePath", modernPath); writer.WriteString("modernAlternativeLocationName", NativeLocations.DisplayLocation(modern));
                        }
                    }
                    catch (InvalidOperationException e) { writer.WriteString("portableError", e.Message); }
                    writer.WriteStartObject("fields"); foreach (var field in save.Profile.BitFields.Keys) writer.WriteNumber(field, mon.Read(field)); writer.WriteEndObject(); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            Put(zip, "Metadata.json", metadata.ToArray());
            Put(zip, "READ ME.txt", Encoding.UTF8.GetBytes("Portable Pokemon remaps exported origins to preserve meaningful locations: Johto/Kanto uses HG/SS and exact city/route names in PK4 when all data fits; newer Pokémon use PK6/PK9 with the Kanto or Johto region. Hoenn uses Omega Ruby origins with exact OR/AS locations in PK6/PK9, or the Hoenn region for unmatched areas. Modern Regional Alternatives provides a newer-format copy of each PK4 export. Metadata.json records original and exported origins, location names/IDs and any PID change needed to preserve nature, ability slot or shininess. These are ROM-hack conversions, not a claim of retail encounter legality. Native Expansion Records retain the matching game's original records; the native save's origins and locations are unchanged.\n"));
        }
        return stream.ToArray();
    }
    private static void Put(ZipArchive zip, string name, byte[] bytes) { using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open(); entry.Write(bytes); }
}
