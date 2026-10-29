using System.Text;
using System.Text.Json;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Historic;

// Explicit source selection is required for saves: Gen 2 headers cannot identify a hack.
public static class GbClassic
{
    public static string Inspect(byte[] input, string profile)
    {
        if (profile is not ("cobalt-blue" or "citrine")) throw new InvalidOperationException("Unsupported Game Boy source.");
        if (input.Length is not (0x8000 or 0x802C or 0x8030)) throw new InvalidDataException("Expected a 32 KiB Game Boy save, optionally with a supported-size RTC footer.");
        if (SaveUtil.GetSaveFile(input.AsSpan(0, 0x8000).ToArray()) is not SAV2 save || save.Version == GameVersion.C || save.Japanese || save.Korean || !save.ChecksumsValid)
            throw new InvalidDataException("Invalid or unrecognized Generation 2 Gold/Silver save checksum.");
        var records = new List<(string Position, PKM Mon)>();
        for(int i=0;i<save.PartyCount;i++) records.Add(($"Party {i+1:D2}",save.GetPartySlotAtIndex(i)));
        for(int box=0;box<save.BoxCount;box++) for(int slot=0;slot<save.BoxSlotCount;slot++) records.Add(($"Box {box+1:D2} Slot {slot+1:D2}",save.GetBoxSlotAtIndex(box*save.BoxSlotCount+slot)));
        return Export(records, false, profile);
    }

    // Exact ROM CRC and live party address are checked by the mGBA container reader.
    public static string InspectLiveParty(ReadOnlySpan<byte> party, string profile = "cobalt-blue")
    {
        if (party.Length != 428 || party[0] > 6 || party[1+party[0]] != 0xFF) throw new InvalidDataException("Invalid live Game Boy party list.");
        var records = new List<(string Position, PKM Mon)>(); int initialSkipped = 0;
        for(int i=0;i<party[0];i++) {
            if (party[i+1] == 0xFD) { initialSkipped++; continue; }
            var mon = new PK2(party.Slice(8+i*48,48),party.Slice(296+i*11,11),party.Slice(362+i*11,11));
            if(mon.Species == 0 || mon.Species != party[i+1]) throw new InvalidDataException($"Invalid live party slot {i+1}.");
            records.Add(($"Party {i+1:D2}",mon));
        }
        return Export(records, true, profile, initialSkipped);
    }

    private static string Export(List<(string Position, PKM Mon)> records, bool state, string profile, int initialSkipped = 0)
    {
        using var mappingStream = typeof(GbClassic).Assembly.GetManifestResourceStream($"historic.gb-{profile}.json") ?? throw new InvalidOperationException("Game mapping unavailable.");
        using var mapping = JsonDocument.Parse(mappingStream);
        var species = mapping.RootElement.GetProperty("species").EnumerateArray().Select(x=>x.GetProperty("id").GetUInt16()).ToHashSet();
        var moves = mapping.RootElement.GetProperty("moves").EnumerateArray().Select(x=>x.GetProperty("id").GetInt32()).ToHashSet();
        using var output = new MemoryStream(); using (var w = new Utf8JsonWriter(output)) {
            w.WriteStartObject(); w.WriteString("profile", profile == "citrine" ? "Pokémon Citrine, supplied revision" : "Pokémon Cobalt Blue, supplied revision"); w.WriteString("sourceType", state ? "mGBA Game Boy party" : "Game Boy battery save");
            if(state) { w.WriteString("trainer", "Live party"); w.WriteString("romCrc", profile == "citrine" ? "A90AC0A3" : "6C8DD2E8"); }
            w.WriteString("warning", (state ? "Live party only; PC is not included. " : "Choose the exact game revision: the save cannot identify it. ") + "Species and moves are matched by names and IDs, not verified mechanics. " + (profile == "citrine" ? "Citrine was tested with one seeded Pokémon in a game-created save; real PC contents remain unverified. " : "") + " Held items are removed from the portable PK2 and preserved in the native record; eggs and unknown moves are skipped. Gen 2 DVs determine shiny; original capture locations and balls may not be stored.");
            w.WriteStartArray("pokemon"); int skipped = initialSkipped;
            foreach(var (position,mon) in records) {
                if (mon.Species == 0) continue;
                if (mon is not PK2 || !species.Contains(mon.Species) || mon.IsEgg || mon.Moves.Any(x=>x!=0 && !moves.Contains(x))) { skipped++; continue; }
                var raw = new byte[mon.SIZE_STORED]; mon.WriteDecryptedDataStored(raw);
                var portable = (PK2)mon.Clone(); portable.HeldItem = 0;
                var pk2 = new byte[portable.SIZE_STORED]; portable.WriteDecryptedDataStored(pk2);
                w.WriteStartObject(); w.WriteString("position",position); w.WriteString("nickname",mon.Nickname);
                w.WriteNumber("nativeSpecies",mon.Species); w.WriteNumber("storedLevel",mon.CurrentLevel);
                w.WriteString("originalTrainer",mon.OriginalTrainerName); w.WriteNumber("trainerId",mon.TID16); w.WriteBoolean("shinyAssumingStandardDVs",mon.IsShiny);
                w.WriteString("nativeFile",position+".pk2"); w.WriteBase64String("nativeBytes",raw);
                w.WriteNumber("nativeHeldItem", mon.HeldItem); w.WriteBoolean("heldItemDropped", mon.HeldItem != 0);
                w.WriteString("file",position+".pk2"); w.WriteBase64String("bytes",pk2); w.WriteNull("portableLocation"); w.WriteNull("error"); w.WriteEndObject();
            }
            w.WriteEndArray(); w.WriteNumber("skippedUnmapped",skipped); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
