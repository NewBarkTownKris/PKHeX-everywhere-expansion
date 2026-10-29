using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;

static class StateChecks
{
    public static void Run()
    {
        foreach (string id in new[] { "rocket-hoenn", "pokeemerald-expansion-1-17-1", "heart-and-soul-2.0.6", "soulgold-1.2.1" })
        {
            var profile = SourceProfile.Find(id); var build = MgbaState.Builds.First(x => x.Profile == id);
            var source = new NativeSave(profile, SyntheticSave.Create(profile));
            var party = (NativePokemon)source.GetPartySlotAtIndex(0);
            var box = (NativePokemon)source.GetBoxSlotAtIndex(source.BoxCount * 30 - 1);
            byte[] state = new byte[0x61000]; BinaryPrimitives.WriteUInt32LittleEndian(state, 0x01000007);
            BinaryPrimitives.WriteUInt32LittleEndian(state.AsSpan(8), build.RomCrc);
            Span<byte> Ram(uint address, int size) => state.AsSpan((int)((address >= 0x03000000 ? 0x19000 + address - 0x03000000 : 0x21000 + address - 0x02000000)), size);
            BinaryPrimitives.WriteUInt32LittleEndian(Ram(build.TrainerPtr, 4), 0x02001000);
            BinaryPrimitives.WriteUInt32LittleEndian(Ram(build.StoragePtr, 4), 0x02010000);
            StringConverter3.SetString(Ram(0x02001000, 7), "TESTER", 7, false, StringConverterOption.ClearFF);
            if (id == "heart-and-soul-2.0.6")
            {
                BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x02001000 + (uint)profile.At("SaveBlock2.playerTrainerId"), 4), 0x11223344);
                Ram(0x0200921C + 20, 1)[0] = 0x80;
            }
            Ram(build.Count, 1)[0] = 1;
            party.WriteEncryptedDataParty(Ram(build.Party, profile.At("sizeof.Pokemon")));
            uint boxAddress = 0x02010000 + (uint)(profile.At("PokemonStorage.boxes") + (profile.At("TOTAL_BOXES_COUNT") * 30 - 1) * profile.At("sizeof.BoxPokemon"));
            box.WriteEncryptedDataStored(Ram(boxAddress, profile.At("sizeof.BoxPokemon")));
            var result = MgbaState.Read(state);
            Check(result.Trainer == "TESTER" && result.Members.Count == 2 && result.Members.All(x => x.Pokemon.Species == 82), "live records " + id);
            if (id == "heart-and-soul-2.0.6") Check(result.Members.All(x => x.Pokemon.Ability == (int)Ability.Electromorphosis), "HnS live trainer seed and randomizer setting");
            using var json = JsonDocument.Parse(StateExport.Inspect(state));
            Check(json.RootElement.GetProperty("pokemon").GetArrayLength() == 2 && json.RootElement.GetProperty("pokemon")[0].GetProperty("error").ValueKind == JsonValueKind.Null, "portable exports " + id);
            if (id == "heart-and-soul-2.0.6")
            {
                var exported = json.RootElement.GetProperty("pokemon")[0];
                Check(exported.GetProperty("file").GetString()!.EndsWith(".pk9"), "HnS modern format for randomized ability");
                var pk = new PK9(exported.GetProperty("bytes").GetBytesFromBase64());
                Check(pk.Ability == (int)Ability.Electromorphosis, "HnS state export ability");
            }
            using var zip = new ZipArchive(new MemoryStream(StateExport.Archive(json.RootElement.GetRawText())), ZipArchiveMode.Read);
            Check(zip.Entries.Count(e => e.FullName.EndsWith(".expmon")) == 2 && zip.GetEntry("Metadata.json") != null, "archive " + id);
            state[8] ^= 1;
            bool rejected = false; try { MgbaState.Read(state); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "unrecognized ROM " + id);
        }
        Console.WriteLine("PASS mGBA state adapters, live party/PC, portable archives and unknown-ROM rejection");
    }
    private static void Check(bool ok, string text) { if (!ok) throw new Exception("mGBA state check failed: " + text); }
}
