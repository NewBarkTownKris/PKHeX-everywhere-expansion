using System.Buffers.Binary;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Historic;
using PKHeX.Everywhere.RomHacks.Expansion;

static class GbStateChecks
{
    public static void Run()
    {
        byte[] raw = new byte[0x11800]; BinaryPrimitives.WriteUInt32LittleEndian(raw, 0x00400003);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(4), 0x878032D3);
        var mon = new PK1 { Species = 25, Nickname = "TEST", OriginalTrainerName = "CHECK", TID16 = 1234, Move1 = 84 };
        var party = raw.AsSpan(0x5563, 0x194); party[0] = 1; party[1] = mon.SpeciesInternal; party[2] = 0xFF;
        mon.Data.CopyTo(party.Slice(8, 44)); mon.OriginalTrainerTrash.CopyTo(party.Slice(272, 11)); mon.NicknameTrash.CopyTo(party.Slice(338, 11));
        string result = MgbaShinRed.Inspect(raw) ?? throw new Exception("GB state not detected");
        using var json = JsonDocument.Parse(result); var list = json.RootElement.GetProperty("pokemon");
        if (list.GetArrayLength() != 1 || list[0].GetProperty("species").GetInt32() != 25 || list[0].GetProperty("originalTrainer").GetString() != "CHECK") throw new Exception("GB party mismatch");
        var file = Convert.FromBase64String(list[0].GetProperty("bytes").GetString()!);
        var decoded = EntityFormat.GetFromBytes(file, EntityContext.Gen1) ?? throw new Exception("bad PK1");
        if (decoded.Species != 25 || decoded.TID16 != 1234 || decoded.Nickname != "TEST") throw new Exception("GB PK1 mismatch");
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(StateExport.Archive(result)));
        if (archive.GetEntry("READ ME.txt") is null || !archive.Entries.Any(x => x.FullName.EndsWith(".pk1"))) throw new Exception("GB archive missing records");
        raw[4] ^= 1;
        try { MgbaShinRed.Inspect(raw); throw new Exception("unknown ROM accepted"); } catch (InvalidDataException) { }
        raw[4] ^= 1; party[0] = 7;
        try { MgbaShinRed.Inspect(raw); throw new Exception("invalid party accepted"); } catch (InvalidDataException) { }
        if (MgbaShinRed.Inspect(new byte[0x61000]) is not null) throw new Exception("GBA mistaken for GB");
        var gbcSave = new SAV2(LanguageID.English, GameVersion.GD);
        var gbcMon = new PK2 { Species = 25, Nickname = "TEST", OriginalTrainerName = "CHECK", TID16 = 1234, Move1 = 84, HeldItem = 173 };
        gbcSave.PartyData = [gbcMon]; gbcSave.SetBoxSlotAtIndex(gbcMon,1,2); gbcSave.Data[0x2D6D] = 0xFF;
        byte[] gbData = gbcSave.Write().ToArray()[..0x8000];
        var gbcState = new byte[0x11800]; BinaryPrimitives.WriteUInt32LittleEndian(gbcState,0x00400003); BinaryPrimitives.WriteUInt32LittleEndian(gbcState.AsSpan(4),0x6C8DD2E8);
        gbData.AsSpan(0x288A,428).CopyTo(gbcState.AsSpan(0x5E22));
        using (var live = JsonDocument.Parse(MgbaShinRed.Inspect(gbcState) ?? throw new Exception("Cobalt state not recognized")))
            if(live.RootElement.GetProperty("pokemon").GetArrayLength()!=1 || !live.RootElement.GetProperty("pokemon")[0].GetProperty("heldItemDropped").GetBoolean()) throw new Exception("Cobalt live party mismatch");
        var eggState = gbcState.ToArray(); eggState[0x5E23] = 0xFD;
        using (var eggs = JsonDocument.Parse(MgbaShinRed.Inspect(eggState) ?? throw new Exception("Cobalt egg state not recognized")))
            if (eggs.RootElement.GetProperty("skippedUnmapped").GetInt32()!=1 || eggs.RootElement.GetProperty("pokemon").GetArrayLength()!=0) throw new Exception("Cobalt egg not skipped");
        using (var cobalt = JsonDocument.Parse(GbClassic.Inspect(gbData,"cobalt-blue"))) {
            var entries=cobalt.RootElement.GetProperty("pokemon");
            if (entries.GetArrayLength()!=2 || !entries[0].GetProperty("heldItemDropped").GetBoolean() || entries[1].GetProperty("position").GetString()!="Box 02 Slot 01") throw new Exception("Cobalt party, box or held item mismatch");
            var converted=EntityFormat.GetFromBytes(entries[0].GetProperty("bytes").GetBytesFromBase64(),EntityContext.Gen2) ?? throw new Exception("bad PK2");
            if(converted.Species!=25 || converted.HeldItem!=0 || converted.TID16!=1234) throw new Exception("Cobalt PK2 mismatch");
        }
        gbcMon.Move1=15; gbcSave.PartyData=[gbcMon];
        using (var skipped = JsonDocument.Parse(GbClassic.Inspect(gbcSave.Write().ToArray()[..0x8000],"cobalt-blue")))
            if (skipped.RootElement.GetProperty("skippedUnmapped").GetInt32()<1) throw new Exception("unmapped custom move accepted");
        Console.WriteLine("PASS Shin Red mGBA party and Cobalt Blue save exports, item handling, PC positions and invalid inputs");
    }
}
