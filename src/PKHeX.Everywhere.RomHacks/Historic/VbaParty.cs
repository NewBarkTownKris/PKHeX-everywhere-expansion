using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using System.Buffers.Binary;
using PKHeX.Everywhere.RomHacks.Expansion;

namespace PKHeX.Everywhere.RomHacks.Historic;

/// <summary>Conservative, read-only recovery for explicitly chosen historic VBA game builds.</summary>
public static class VbaParty
{
    private sealed record Profile(string Name, string Header, int Version, int Count, int Party);
    // Offsets are in EWRAM, independently checked against the accompanying old states.
    private static readonly Dictionary<string, Profile> Profiles = new(StringComparer.Ordinal) {
        ["rijon-2009"] = new("Rijon Adventures 2009 Beta", "RIJON ADVENTBPRE", 9, 0x24029, 0x24284),
        ["rijon"] = new("Rijon Adventures", "POKEMON FIREBPRE", 8, 0x24029, 0x24284),
        ["gary-edition"] = new("Pokémon Gary Edition Beta 1", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["lightning-yellow"] = new("Pokémon Lightning Yellow Demo", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["shiny-gold"] = new("Pokémon Shiny Gold Beta 5 X", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["thunder-emblem"] = new("Pokémon Thunder Emblem Beta 1", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["my-ass"] = new("Pokémon My Ass", "POKEMON FIREBPRE", 9, 0x24029, 0x24284),
        ["crono-beta-1"] = new("Pokémon Crono Beta 1", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["leaf-green-usa"] = new("Pokémon LeafGreen USA (supplied revision)", "POKEMON LEAFBPGE", 0, 0x24029, 0x24284),
        ["liquid-ocean-alpha-1-2"] = new("Pokémon Liquid Ocean Alpha 1.2", "POKEMON LIQUID69", 0, 0x24029, 0x24284),
        ["old-white-1-2"] = new("Pokémon Old White 1.2", "OLD WHITE   AAAA", 0, 0x24029, 0x24284),
        ["pokemon-heroes"] = new("Pokémon Heroes", "POKEMON RUBYWAHH", 0, 0x4350, 0x4360),
        ["snakewood-fixed"] = new("Pokémon Snakewood Fixed", "SNAKEWOOD   AXVE", 0, 0x4350, 0x4360),
        ["resolute-1-17"] = new("Pokémon Resolute 1.17 Fixed", "POKEMON EMERBPEE", 0, 0x244E9, 0x244EC),
        ["pokemon-isthmis"] = new("Pokémon Isthmis", "POKEMON EMERBPEE", 0, 0x244E9, 0x244EC),
        ["shattered-dreams"] = new("Pokémon Shattered Dreams", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["marble-beta-2-1"] = new("Pokémon Marble Beta 2.1", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["ruby-destiny-reign"] = new("Pokémon Ruby Destiny: Reign of Legends", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["topaz"] = new("Pokémon Topaz", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["ambar-beta-1"] = new("Pokémon Ambar Beta 1", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["ambar-pabeta1"] = new("Pokémon Ambar pabeta1", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["naranja-beta-2"] = new("Pokémon Naranja Beta 2", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["pokemon-naranja"] = new("Pokémon Naranja, supplied revision", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["ruby-destiny-rescue-beta-3-2"] = new("Pokémon Ruby Destiny Rescue Rangers Beta 3.2", "POKEMON RUBYAVXE", 0, 0x4350, 0x4360),
        ["omega-beta-1"] = new("Pokémon Omega Beta 1", "POKEMON RUBYAXVE", 0, 0x4350, 0x4360),
        ["genesis-beta-3"] = new("Pokémon Genesis Fixed Beta 3", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["jasper-demo"] = new("Pokémon Jasper Demo", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["liquid-crystal-beta-1"] = new("Pokémon Liquid Crystal Beta 1", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["orange-generation"] = new("Pokémon Orange Generation", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["shining-opal-beta-2-13"] = new("Pokémon Shining Opal Beta 2.13", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["twilight"] = new("Pokémon Twilight", "POKEMON FIREBPRE", 0, 0x24029, 0x24284),
        ["crystal-shards-beta-1"] = new("Pokémon Crystal Shards Beta 1", "POKEMON EMERBPEE", 0, 0x244E9, 0x244EC),
        ["liquid-ocean-beta-1"] = new("Liquid Ocean Beta 1", "POKEMON EMERBPEE", 8, 0x244E9, 0x244EC),
    };
    private static bool SeededProfile(string id) => id is "rijon" or "jasper-demo" or "liquid-crystal-beta-1" or "orange-generation" or "shining-opal-beta-2-13" or "twilight" or "genesis-beta-3" or "rijon-2009" or "omega-beta-1" or "pokemon-naranja" or "ruby-destiny-rescue-beta-3-2" or "naranja-beta-2" or "ambar-beta-1" or "ambar-pabeta1" or "topaz" or "ruby-destiny-reign" or "liquid-ocean-alpha-1-2" or "old-white-1-2" or "pokemon-heroes" or "snakewood-fixed" or "resolute-1-17" or "pokemon-isthmis" or "shattered-dreams" or "marble-beta-2-1" or "leaf-green-usa" or "crono-beta-1";
    private const int MaxCompressed = 8_000_000;
    private const int MaxDecompressed = 750_000;

    public static string Inspect(byte[] input, string profileId)
    {
        if (!Profiles.TryGetValue(profileId, out var profile) || profile.Version == 0) throw new InvalidOperationException("Choose a supported VBA game revision.");
        if (input.Length is < 16 or > MaxCompressed || input[0] != 0x1f || input[1] != 0x8b)
            throw new InvalidOperationException("Expected a gzip-compressed VBA save state within the size limit.");
        using var source = new MemoryStream(input, false);
        using var gz = new GZipStream(source, CompressionMode.Decompress);
        using var decoded = new MemoryStream();
        var chunk = new byte[8192];
        for (int n; (n = gz.Read(chunk, 0, chunk.Length)) > 0; ) {
            if (decoded.Length + n > MaxDecompressed) throw new InvalidOperationException("The expanded state exceeds the safe size limit.");
            decoded.Write(chunk, 0, n);
        }
        byte[] data = decoded.ToArray();
        int expectedLength = profile.Version == 8 ? 740238 : 740244;
        if (data.Length != expectedLength || BitConverter.ToInt32(data, 0) != profile.Version || Encoding.ASCII.GetString(data, 4, 16) != profile.Header)
            throw new InvalidOperationException("The state version or game header does not match the selected revision.");
        int iwram = profile.Version == 8 ? 0x1DF : 0x1E5;
        int ewram = iwram + 0x8000 + 0x400; // IWRAM, then palette RAM, then work RAM.
        using var mappingStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}.json")
            ?? throw new InvalidOperationException("Real-species mapping for this game is unavailable.");
        using var mapping = JsonDocument.Parse(mappingStream);
        var realSpecies = mapping.RootElement.GetProperty("species").EnumerateArray()
            .ToDictionary(x => x.GetProperty("id").GetUInt16(), x => x.GetProperty("national").GetUInt16());
        using var fieldsStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}-fields.json")
            ?? throw new InvalidOperationException("Move and item mapping for this game is unavailable.");
        using var fields = JsonDocument.Parse(fieldsStream);
        int count = data[ewram + profile.Count];
        if (count > 6) throw new InvalidOperationException("The party count is invalid for this layout.");
        int skipped = 0;
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream)) {
            w.WriteStartObject(); w.WriteString("profile", profile.Name); w.WriteNumber("vbaVersion", profile.Version);
            w.WriteString("warning", "The ROM header cannot uniquely identify a hack or revision. Select the matching game. Only species whose ROM names and original internal IDs match official species are shown; names alone do not establish forms or mechanics. Portable PK6 files use name-based mappings and standard ability/shiny assumptions, with an unknown met location; the raw record retains source details. Review all approximations.");
            w.WriteStartArray("pokemon");
            for (int i=0;i<count;i++) {
                byte[] raw = data.AsSpan(ewram + profile.Party + i * 100,100).ToArray();
                var mon = new PK3(raw.ToArray());
                if (!mon.ChecksumValid || !mon.FlagHasSpecies || mon.FlagIsBadEgg || mon.SpeciesInternal == 0)
                    throw new InvalidOperationException($"Party slot {i+1} is invalid, corrupt, or uses an unsupported record layout; no records were exported.");
                // A matching official name and original internal ID are necessary but not proof
                // of unmodified forms or game mechanics. Never export unmapped/fake species.
                if (!realSpecies.TryGetValue(mon.SpeciesInternal, out var national)) { skipped++; continue; }
                WritePokemon(w, $"Party {i+1:D2}", mon, raw, fields.RootElement, profileId);
            }
            w.WriteEndArray(); w.WriteNumber("skippedUnmapped", skipped); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WritePokemon(Utf8JsonWriter w, string position, PK3 mon, byte[] raw, JsonElement fields, string? profileId = null)
    {
        w.WriteStartObject(); w.WriteString("position", position); w.WriteString("nickname", mon.Nickname);
        w.WriteNumber("nativeSpecies", mon.SpeciesInternal); w.WriteNumber("nationalSpecies", mon.Species);
        w.WriteNumber("storedLevel", raw.Length == 100 ? mon.Stat_Level : mon.CurrentLevel);
        w.WriteBoolean("levelApproximate", raw.Length != 100);
        w.WriteString("originalTrainer", mon.OriginalTrainerName); w.WriteNumber("trainerId", mon.TID16); w.WriteNumber("secretId", mon.SID16);
        w.WriteNumber("nativePid", mon.PID); w.WriteNumber("nativeMetLocation", mon.MetLocation); w.WriteNumber("nativeMetLevel", mon.MetLevel);
        w.WriteString("nativeOrigin", mon.Version.ToString()); w.WriteNumber("nativeBall", mon.Ball); w.WriteNumber("nativeHeldItem", mon.HeldItem);
        w.WriteBoolean("shinyAssumingStandardPid", mon.IsShiny);
        w.WriteString("nativeFile", position + ".bin"); w.WriteBase64String("nativeBytes", raw);
        try {
            var portable = VbaPortable.Convert(mon, fields, profileId);
            byte[] encoded = new byte[portable.SIZE_STORED]; portable.WriteDecryptedDataStored(encoded);
            w.WriteString("file", position + ".pk6"); w.WriteBase64String("bytes", encoded);
            w.WriteBoolean("pidAdjusted", portable.PID != mon.PID); w.WriteString("portableLocation", NativeLocations.DisplayLocation(portable)); w.WriteNull("error");
        } catch (Exception e) when (e is InvalidOperationException or ArgumentException or IndexOutOfRangeException) {
            w.WriteNull("file"); w.WriteNull("bytes"); w.WriteBoolean("pidAdjusted", false); w.WriteString("error", e.Message);
        }
        w.WriteEndObject();
    }

    // Supported only for the exact revision with a game-created save fixture.
    public static string InspectBattery(byte[] input, string profileId)
    {
        if (profileId is not ("rijon" or "my-ass" or "thunder-emblem" or "shiny-gold" or "lightning-yellow" or "gary-edition" or "liquid-ocean-beta-1" or "crystal-shards-beta-1" or "jasper-demo" or "liquid-crystal-beta-1" or "orange-generation" or "shining-opal-beta-2-13" or "twilight" or "genesis-beta-3" or "rijon-2009" or "omega-beta-1" or "pokemon-naranja" or "ruby-destiny-rescue-beta-3-2" or "naranja-beta-2" or "ambar-beta-1" or "ambar-pabeta1" or "topaz" or "ruby-destiny-reign" or "liquid-ocean-alpha-1-2" or "old-white-1-2" or "pokemon-heroes" or "snakewood-fixed" or "resolute-1-17" or "pokemon-isthmis" or "shattered-dreams" or "marble-beta-2-1" or "leaf-green-usa" or "crono-beta-1")) throw new InvalidOperationException("This game does not yet have verified battery-save support.");
        using var speciesStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}.json")
            ?? throw new InvalidOperationException("Real-species mapping unavailable.");
        using var fieldsStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}-fields.json")
            ?? throw new InvalidOperationException("Move and item mapping unavailable.");
        using var species = JsonDocument.Parse(speciesStream); using var fields = JsonDocument.Parse(fieldsStream);
        var allowed = species.RootElement.GetProperty("species").EnumerateArray().Select(x => x.GetProperty("id").GetUInt16()).ToHashSet();
        var save = profileId is "omega-beta-1" or "pokemon-naranja" or "ruby-destiny-rescue-beta-3-2" or "naranja-beta-2" or "ambar-beta-1" or "ambar-pabeta1" or "topaz" or "ruby-destiny-reign" or "crono-beta-1" or "pokemon-heroes" or "snakewood-fixed" ? HistoricGen3.InspectRubySave(input) : profileId is "liquid-ocean-beta-1" or "crystal-shards-beta-1" or "resolute-1-17" or "pokemon-isthmis" ? HistoricGen3.InspectEmeraldSave(input) : HistoricGen3.InspectFireRedSave(input);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream)) {
            w.WriteStartObject(); w.WriteString("profile", Profiles[profileId].Name); w.WriteString("sourceType", "GBA battery save");
            w.WriteString("warning", "Select the matching ROM revision; the save header is shared. PK6 uses name-based mappings and standard ability and PID-derived shiny assumptions. Met locations are unknown unless explicitly mapped; Shiny Gold New Bark Town and Lightning Yellow and Gary Edition Pallet Town use regional approximations. Box levels use standard growth data and can differ in a hack." + (profileId == "rijon" ? " One supplied VBA party was also checked against a battery file reconstructed from live state memory and an mGBA fixture. No game-created battery save was available; real PC contents remain unverified." : SeededProfile(profileId) ? " This revision was checked with one cross-game seeded Pokémon that the game displayed and re-saved; other party records and real PC contents remain unverified." : ""));
            w.WriteStartArray("pokemon"); int skipped=0;
            foreach (var record in save.Records) {
                if (!record.ChecksumValid) throw new InvalidOperationException($"{record.Position} has an invalid checksum; no records were exported.");
                var mon = new PK3(record.NativeRecord.ToArray());
                if (!allowed.Contains(mon.SpeciesInternal)) { skipped++; continue; }
                WritePokemon(w, record.Position, mon, record.NativeRecord, fields.RootElement, profileId);
            }
            w.WriteEndArray(); w.WriteNumber("skippedUnmapped", skipped); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // Exact ROM CRC and RAM addresses are validated against a fresh mGBA game-created save.
    // Other builds need their own independent profile, not a matching shared game header.
    public static string? InspectMgba(byte[] input)
    {
        byte[] state = MgbaState.ExtractCore(input);
        if (BinaryPrimitives.ReadUInt32LittleEndian(state) != 0x01000007)
            throw new InvalidDataException("Unsupported mGBA state version.");
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(8));
        string? profileId = crc switch { 0x1DED7E51 => "my-ass", 0x6A2B8EE2 => "thunder-emblem", 0x61EF78C2 => "shiny-gold", 0x82F643BF => "lightning-yellow", 0x912918D8 => "gary-edition", 0xB82CE606 => "liquid-ocean-beta-1", 0xA3938FD1 => "crystal-shards-beta-1", 0x178CEAA6 => "jasper-demo", 0xAE6BE484 => "liquid-crystal-beta-1", 0x9165F8A3 => "orange-generation", 0xFFA7854B => "shining-opal-beta-2-13", 0xF7C447B1 => "twilight", 0xC9B11249 => "genesis-beta-3", 0xF57EC245 => "rijon", 0x935ECB8B => "rijon-2009", 0x532F54BC => "omega-beta-1", 0x84C3A0D6 => "pokemon-naranja", 0x7D43AEE4 => "ruby-destiny-rescue-beta-3-2", 0xE4C181BF => "naranja-beta-2", 0x4EA40939 => "ambar-beta-1", 0x52FF506E => "ambar-pabeta1", 0x1B921419 => "topaz", 0x575C5ECD => "ruby-destiny-reign", 0x7AB26D21 => "liquid-ocean-alpha-1-2", 0x090974A4 => "old-white-1-2", 0x87365785 => "pokemon-heroes", 0x07001014 => "snakewood-fixed", 0x7F3B87B4 => "resolute-1-17", 0x80237E35 => "pokemon-isthmis", 0x60A63534 => "shattered-dreams", 0x2BED9BD8 => "marble-beta-2-1", 0x1C12FD39 => "leaf-green-usa", 0xCF4F1957 => "crono-beta-1", _ => null };
        if (profileId is null) return null;
        ReadOnlySpan<byte> Ram(uint address, int length)
        {
            if (length < 0) throw new InvalidDataException("Invalid RAM length.");
            long offset = address switch {
                >= 0x02000000 and < 0x02040000 => 0x21000L + address - 0x02000000,
                >= 0x03000000 and < 0x03008000 => 0x19000L + address - 0x03000000,
                _ => throw new InvalidDataException("A game memory pointer is outside GBA RAM.")
            };
            long end = address < 0x03000000 ? 0x61000L : 0x21000L;
            if (offset + length > end) throw new InvalidDataException("A game memory pointer crosses the end of RAM.");
            return state.AsSpan((int)offset, length);
        }
        uint Pointer(uint address) => BinaryPrimitives.ReadUInt32LittleEndian(Ram(address,4));
        bool ruby = profileId is "omega-beta-1" or "pokemon-naranja" or "ruby-destiny-rescue-beta-3-2" or "naranja-beta-2" or "ambar-beta-1" or "ambar-pabeta1" or "topaz" or "ruby-destiny-reign" or "crono-beta-1" or "pokemon-heroes" or "snakewood-fixed";
        bool emerald = profileId is "liquid-ocean-beta-1" or "crystal-shards-beta-1" or "resolute-1-17" or "pokemon-isthmis";
        string trainer = StringConverter3.GetString(Ram(ruby ? 0x02024EA4u : Pointer(emerald ? 0x03005D90u : 0x0300500Cu),7),false);
        if (string.IsNullOrWhiteSpace(trainer)) throw new InvalidDataException("No initialized trainer in this state.");
        int count = Ram(ruby ? 0x03004350u : emerald ? 0x020244E9u : 0x02024029u,1)[0]; if (count > 6) throw new InvalidDataException("Invalid live party count.");
        var storage = Ram(ruby ? 0x020300A0u : Pointer(emerald ? 0x03005D94u : 0x03005010u), 4 + 14 * 30 * 80);
        if (BinaryPrimitives.ReadUInt32LittleEndian(storage) >= 14) throw new InvalidDataException("Invalid PC box index.");
        using var speciesStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}.json")
            ?? throw new InvalidOperationException("Real-species mapping unavailable.");
        using var fieldsStream = typeof(VbaParty).Assembly.GetManifestResourceStream($"historic.vba-{profileId}-fields.json")
            ?? throw new InvalidOperationException("Move and item mapping unavailable.");
        using var species = JsonDocument.Parse(speciesStream); using var fields = JsonDocument.Parse(fieldsStream);
        var allowed = species.RootElement.GetProperty("species").EnumerateArray().Select(x => x.GetProperty("id").GetUInt16()).ToHashSet();
        using var stream = new MemoryStream(); using (var w = new Utf8JsonWriter(stream)) {
            w.WriteStartObject(); w.WriteString("profile", Profiles[profileId].Name); w.WriteString("sourceType", "mGBA historic state");
            w.WriteString("trainer",trainer); w.WriteString("romCrc", crc.ToString("X8"));
            w.WriteString("warning", "PK6 uses source-name mappings and standard ability and PID-derived shiny assumptions. Met locations are unknown unless explicitly mapped; Shiny Gold New Bark Town and Lightning Yellow and Gary Edition Pallet Town use regional approximations. Box level is based on standard growth data." + (profileId == "rijon" ? " One supplied VBA party was also checked against a battery file reconstructed from live state memory and an mGBA fixture. No game-created battery save was available; real PC contents remain unverified." : SeededProfile(profileId) ? " This revision was checked with one cross-game seeded Pokémon that the game displayed and re-saved; other party records and real PC contents remain unverified." : ""));
            w.WriteStartArray("pokemon"); int skipped = 0;
            void Add(string position, ReadOnlySpan<byte> raw, bool party) {
                if ((raw[0x13] & 2) == 0) {
                    if (party) throw new InvalidDataException($"{position} is empty despite the party count.");
                    return;
                }
                var mon = new PK3(raw.ToArray());
                if (!mon.ChecksumValid || mon.FlagIsBadEgg || mon.SpeciesInternal == 0)
                    throw new InvalidDataException($"{position} has an invalid native Pokémon record.");
                if (!allowed.Contains(mon.SpeciesInternal)) { skipped++; return; }
                WritePokemon(w,position,mon,raw.ToArray(),fields.RootElement,profileId);
            }
            for (int i=0;i<count;i++) Add($"Party {i+1:D2}",Ram((ruby ? 0x03004360u : emerald ? 0x020244ECu : 0x02024284u)+(uint)(i*100),100),true);
            for (int box=0;box<14;box++) for (int slot=0;slot<30;slot++)
                Add($"Box {box+1:D2} Slot {slot+1:D2}",storage.Slice(4+(box*30+slot)*80,80),false);
            w.WriteEndArray(); w.WriteNumber("skippedUnmapped",skipped); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static byte[] Archive(string json)
    {
        using var root = JsonDocument.Parse(json); using var output = new MemoryStream(); using var metadata = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true)) {
            using (var w = new Utf8JsonWriter(metadata, new JsonWriterOptions { Indented = true })) {
                w.WriteStartObject();
                foreach (var p in root.RootElement.EnumerateObject()) {
                    if (p.Name != "pokemon") { p.WriteTo(w); continue; }
                    w.WriteStartArray("pokemon"); var usedPositions = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var mon in p.Value.EnumerateArray()) {
                        string position = mon.GetProperty("position").GetString() ?? "";
                        bool party = System.Text.RegularExpressions.Regex.IsMatch(position, @"^Party 0[1-6]$");
                        bool box = System.Text.RegularExpressions.Regex.IsMatch(position, @"^Box (0[1-9]|1[0-4]) Slot (0[1-9]|[12][0-9]|30)$");
                        if ((!party && !box) || !usedPositions.Add(position))
                            throw new InvalidOperationException("Invalid or duplicate Pokémon position.");
                        var raw = Convert.FromBase64String(mon.GetProperty("nativeBytes").GetString()!);
                        if (raw.Length != (party ? 100 : 80)) throw new InvalidOperationException("Invalid native record length.");
                        using (var dst = zip.CreateEntry($"Native/{position}.bin").Open()) dst.Write(raw);
                        if (mon.GetProperty("bytes").ValueKind == JsonValueKind.String) {
                            byte[] portable = Convert.FromBase64String(mon.GetProperty("bytes").GetString()!);
                            string extension = portable.Length switch { 232 => "pk6", 328 => "pk9", _ => throw new InvalidOperationException("Invalid portable Pokémon record length.") };
                            using var dst = zip.CreateEntry($"Portable/{position}.{extension}").Open(); dst.Write(portable);
                        }
                        w.WriteStartObject(); foreach (var field in mon.EnumerateObject()) if (field.Name is not ("nativeBytes" or "bytes")) field.WriteTo(w); w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                w.WriteEndObject();
            }
            using (var dst=zip.CreateEntry("Metadata.json").Open()) dst.Write(metadata.ToArray());
            using (var dst=zip.CreateEntry("READ ME.txt").Open()) dst.Write(Encoding.UTF8.GetBytes("Native records preserve source bytes. Portable files are approximations: see the warning and original fields in Metadata.json. Game headers are shared, so choose the correct ROM revision. No retail encounter legality is implied.\n"));
        }
        return output.ToArray();
    }
}
