using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Historic;

static class VbaChecks
{
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("VBA check: " + reason); }
    static byte[] Gzip(byte[] data) { using var s = new MemoryStream(); using (var z = new GZipStream(s, CompressionLevel.Fastest, true)) z.Write(data); return s.ToArray(); }
    public static void Run()
    {
        byte[] raw = new byte[740244]; BinaryPrimitives.WriteInt32LittleEndian(raw, 9);
        Encoding.ASCII.GetBytes("POKEMON FIREBPRE").CopyTo(raw, 4);
        const int ewram = 0x1E5 + 0x8400, count = ewram + 0x24029, party = ewram + 0x24284;
        raw[count] = 2;
        var mon = new PK3 { Species = 25, Nickname = "TEST", OriginalTrainerName = "TESTER", Stat_Level = 5, Ball = 4, Move1 = 33, PID = 0x00080000, ID32 = 0x00000000, Version = GameVersion.FR };
        mon.RefreshChecksum(); mon.WriteEncryptedDataParty(raw.AsSpan(party, 100));
        mon.SpeciesInternal = 600; mon.RefreshChecksum(); mon.WriteEncryptedDataParty(raw.AsSpan(party + 100, 100));
        string result = VbaParty.Inspect(Gzip(raw), "my-ass"); using var json = JsonDocument.Parse(result);
        Require(json.RootElement.GetProperty("pokemon").GetArrayLength() == 1 && json.RootElement.GetProperty("skippedUnmapped").GetInt32() == 1, "unmapped species skipped");
        var entry = json.RootElement.GetProperty("pokemon")[0];
        Require(entry.GetProperty("error").ValueKind == JsonValueKind.Null, "portable conversion");
        var portable = new PK6(entry.GetProperty("bytes").GetBytesFromBase64());
        Require(portable.ChecksumValid && portable.Species == 25 && portable.Nickname == "TEST" && portable.OriginalTrainerName == "TESTER" && portable.ID32 == 0 && !portable.IsShiny && entry.GetProperty("pidAdjusted").GetBoolean() && portable.Move1 == 33 && portable.MetLocation == 0, "portable identity and unknown location");
        using (var zip = new ZipArchive(new MemoryStream(VbaParty.Archive(result)), ZipArchiveMode.Read))
            Require(zip.GetEntry("Native/Party 01.bin") != null && zip.GetEntry("Portable/Party 01.pk6") != null && zip.GetEntry("Metadata.json") != null && zip.Entries.Count == 4, "archive contents");
        var originalFirst = raw.AsSpan(party, 100).ToArray();
        raw.AsSpan(party + 100, 100).CopyTo(raw.AsSpan(party, 100)); originalFirst.CopyTo(raw.AsSpan(party + 100, 100));
        string shifted = VbaParty.Inspect(Gzip(raw), "my-ass");
        using (var zip = new ZipArchive(new MemoryStream(VbaParty.Archive(shifted)), ZipArchiveMode.Read))
            Require(zip.GetEntry("Native/Party 02.bin") != null && zip.GetEntry("Portable/Party 02.pk6") != null && zip.GetEntry("Native/Party 01.bin") == null, "preserve source slot when a custom species is skipped");
        originalFirst.CopyTo(raw.AsSpan(party,100));
        var badBall = new PK3(raw.AsSpan(party,100).ToArray()) { Ball = 1 }; badBall.RefreshChecksum(); badBall.WriteEncryptedDataParty(raw.AsSpan(party,100));
        using (var unknownBall = JsonDocument.Parse(VbaParty.Inspect(Gzip(raw), "my-ass")))
            Require(unknownBall.RootElement.GetProperty("pokemon")[0].GetProperty("bytes").ValueKind == JsonValueKind.Null, "unverified ball rejected");
        raw[count] = 7;
        try { VbaParty.Inspect(Gzip(raw), "my-ass"); throw new Exception("invalid count accepted"); } catch (InvalidOperationException) { }
        try { VbaParty.Inspect(Gzip(raw), "rijon"); throw new Exception("wrong version accepted"); } catch (InvalidOperationException) { }
        try { VbaParty.Inspect([0, 1, 2], "my-ass"); throw new Exception("bad gzip accepted"); } catch (InvalidOperationException) { }
        var saved = new byte[0x20000]; Array.Fill(saved, (byte)0xff);
        for (int i=0;i<14;i++) {
            var sector=saved.AsSpan(i*0x1000,0x1000); sector[..0xf80].Clear();
            BinaryPrimitives.WriteUInt16LittleEndian(sector[0xff4..],(ushort)i);
            BinaryPrimitives.WriteUInt32LittleEndian(sector[0xff8..],0x08012025);
            BinaryPrimitives.WriteUInt32LittleEndian(sector[0xffc..],4);
        }
        var save = new SAV3FRLG(saved); var pk = new PK3(originalFirst); save.OT="TESTER";
        save.PartyData = [pk]; save.SetBoxSlotAtIndex(pk,1,2); save.WriteBothSaveSlots(saved);
        var withFooter = new byte[0x20010]; saved.CopyTo(withFooter,0);
        string batteryJson = VbaParty.InspectBattery(withFooter,"my-ass"); using var battery = JsonDocument.Parse(batteryJson);
        Require(battery.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "battery party and box count");
        Require(battery.RootElement.GetProperty("pokemon")[1].GetProperty("position").GetString() == "Box 02 Slot 03", "battery box location");
        using (var zip = new ZipArchive(new MemoryStream(VbaParty.Archive(batteryJson)), ZipArchiveMode.Read))
            Require(zip.GetEntry("Portable/Box 02 Slot 03.pk6") != null && zip.GetEntry("Native/Box 02 Slot 03.bin") != null, "battery box archive");
        using (var gary = JsonDocument.Parse(VbaParty.InspectBattery(withFooter,"gary-edition")))
            Require(gary.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "Gary Edition battery party and synthetic PC");
        using (var lightning = JsonDocument.Parse(VbaParty.InspectBattery(withFooter,"lightning-yellow")))
            Require(lightning.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "Lightning Yellow battery party and synthetic PC");
        using (var shiny = JsonDocument.Parse(VbaParty.InspectBattery(withFooter,"shiny-gold")))
            Require(shiny.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "Shiny Gold battery party and synthetic PC");
        using (var thunder = JsonDocument.Parse(VbaParty.InspectBattery(withFooter,"thunder-emblem")))
            Require(thunder.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "Thunder Emblem battery party and synthetic PC");
        using (var rijon = JsonDocument.Parse(VbaParty.InspectBattery(withFooter,"rijon"))) Require(rijon.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Rijon synthetic battery party/PC");
        // Classic VBA 9 uses live EWRAM PC storage as well as the party.
        byte[] classic = new byte[740244]; BinaryPrimitives.WriteInt32LittleEndian(classic,9); Encoding.ASCII.GetBytes("POKEMON FIREBPRE").CopyTo(classic,4);
        BinaryPrimitives.WriteUInt32LittleEndian(classic.AsSpan(0x1e5+0x5010),0x02010000); classic[ewram+0x24029]=1;
        var cmon=new PK3 {Species=83,Nickname="Farfetch'd",OriginalTrainerName="TESTER",Ball=4,Move1=185,Stat_Level=5,PID=0x12345678,Version=GameVersion.FR}; cmon.RefreshChecksum(); cmon.WriteEncryptedDataParty(classic.AsSpan(ewram+0x24284,100)); cmon.WriteEncryptedDataStored(classic.AsSpan(ewram+0x10000+4+32*80,80));
        using(var lc=JsonDocument.Parse(ClassicRecovered.InspectVba(Gzip(classic),"liquid-crystal"))){
            Require(lc.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Liquid Crystal live party and PC"); var lcMon=lc.RootElement.GetProperty("pokemon")[0];
            var portableLc=new PK6(lcMon.GetProperty("bytes").GetBytesFromBase64());Require(portableLc.Species==83&&portableLc.Move1==185,"Farfetch'd and Faint Attack remapping");
            Require(lcMon.GetProperty("nativeBytes").GetBytesFromBase64().SequenceEqual(classic.AsSpan(ewram+0x24284,100).ToArray()),"native state bytes preserved");
        }
        BinaryPrimitives.WriteUInt32LittleEndian(classic.AsSpan(0x1e5+0x5010),0x0203fff0);
        try{ClassicRecovered.InspectVba(Gzip(classic),"liquid-crystal");throw new Exception("invalid PC pointer accepted");}catch(InvalidDataException){}
        byte[] scattered=Enumerable.Repeat((byte)0xff,0x10000).ToArray();for(int i=0;i<14;i++)saved.AsSpan(i*0x1000,0x1000).CopyTo(scattered.AsSpan(((i+1)%16)*0x1000));
        using(var dr=JsonDocument.Parse(ClassicRecovered.InspectBattery(scattered,"dark-rising"))) Require(dr.RootElement.GetProperty("pokemon").GetArrayLength()==2,"scattered flash sector recovery");
        scattered.AsSpan(0x1000,0x1000).Fill(0xff);using(var partial=JsonDocument.Parse(ClassicRecovered.InspectBattery(scattered,"liquid-crystal"))) Require(partial.RootElement.GetProperty("pokemon").GetArrayLength()==2 && partial.RootElement.GetProperty("sourceType").GetString()!.StartsWith("Partial"),"partial Liquid Crystal recovers party and PC");try{ClassicRecovered.InspectBattery(scattered,"dark-rising");throw new Exception("incomplete flash accepted");}catch(InvalidOperationException){}
        var emeraldSave = new SAV3E(saved.ToArray()); emeraldSave.OT="TESTER"; emeraldSave.PartyData=[pk]; emeraldSave.SetBoxSlotAtIndex(pk,1,2); var emeraldBytes=saved.ToArray(); emeraldSave.WriteBothSaveSlots(emeraldBytes);
        using(var ocean=JsonDocument.Parse(VbaParty.InspectBattery(emeraldBytes,"liquid-ocean-beta-1")))
            Require(ocean.RootElement.GetProperty("pokemon").GetArrayLength()==2 && ocean.RootElement.GetProperty("pokemon")[1].GetProperty("position").GetString()=="Box 02 Slot 03","Liquid Ocean Emerald battery synthetic party/PC");
        using(var shards=JsonDocument.Parse(VbaParty.InspectBattery(emeraldBytes,"crystal-shards-beta-1"))) Require(shards.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Crystal Shards battery synthetic party/PC");
        foreach(var profile in new[]{"leaf-green-usa","rijon-2009","genesis-beta-3","jasper-demo","liquid-crystal-beta-1","orange-generation","shining-opal-beta-2-13","twilight"}) {
            using var synthetic=JsonDocument.Parse(VbaParty.InspectBattery(withFooter,profile));Require(synthetic.RootElement.GetProperty("pokemon").GetArrayLength()==2,$"{profile} synthetic battery party/PC");
        }
        var rubySave=new SAV3RS(saved.ToArray());rubySave.OT="TESTER";rubySave.PartyData=[pk];rubySave.SetBoxSlotAtIndex(pk,1,2);var rubyBytes=saved.ToArray();rubySave.WriteBothSaveSlots(rubyBytes);
        foreach(var profile in new[]{"omega-beta-1","pokemon-naranja","ruby-destiny-rescue-beta-3-2","naranja-beta-2","ambar-beta-1","ambar-pabeta1","ruby-destiny-reign","crono-beta-1"}){using var ruby=JsonDocument.Parse(VbaParty.InspectBattery(rubyBytes,profile));Require(ruby.RootElement.GetProperty("pokemon").GetArrayLength()==2,$"{profile} Ruby battery synthetic party/PC");}
        var testEevee=new PK3(originalFirst.ToArray()){Species=133,Move1=33,Move2=39,Move3=270};testEevee.RefreshChecksum();rubySave.PartyData=[testEevee];rubySave.SetBoxSlotAtIndex(testEevee,1,2);var topazBytes=saved.ToArray();rubySave.WriteBothSaveSlots(topazBytes);
        using(var topaz=JsonDocument.Parse(VbaParty.InspectBattery(topazBytes,"topaz"))) Require(topaz.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Topaz Ruby battery real Eevee party/PC");
        rubySave.SetBoxSlotAtIndex(pk,1,2);
        var fakeAmbar=new PK3(originalFirst.ToArray()){SpeciesInternal=280};fakeAmbar.RefreshChecksum();rubySave.PartyData=[fakeAmbar];var fakeBytes=saved.ToArray();rubySave.WriteBothSaveSlots(fakeBytes);
        using(var fake=JsonDocument.Parse(VbaParty.InspectBattery(fakeBytes,"ambar-beta-1"))) Require(fake.RootElement.GetProperty("skippedUnmapped").GetInt32()==1 && fake.RootElement.GetProperty("pokemon").GetArrayLength()==1,"Ámbar custom starter is skipped while real PC remains");
        var testCynda=new PK3(originalFirst.ToArray()){Species=155,Move1=33,Move2=43,Move3=0};testCynda.RefreshChecksum();var marbleSave=new SAV3FRLG(saved.ToArray());marbleSave.OT="TESTER";marbleSave.PartyData=[testCynda];marbleSave.SetBoxSlotAtIndex(testCynda,1,2);var marbleBytes=saved.ToArray();marbleSave.WriteBothSaveSlots(marbleBytes);
        using(var marble=JsonDocument.Parse(VbaParty.InspectBattery(marbleBytes,"marble-beta-2-1")))Require(marble.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Marble synthetic real party/PC");
        var fakeMarble=new PK3(originalFirst.ToArray()){SpeciesInternal=4};fakeMarble.RefreshChecksum();marbleSave.PartyData=[fakeMarble];var fakeMarbleBytes=saved.ToArray();marbleSave.WriteBothSaveSlots(fakeMarbleBytes);using(var marble=JsonDocument.Parse(VbaParty.InspectBattery(fakeMarbleBytes,"marble-beta-2-1")))Require(marble.RootElement.GetProperty("skippedUnmapped").GetInt32()==1 && marble.RootElement.GetProperty("pokemon").GetArrayLength()==1,"Marble custom starter skipped, real PC kept");
        byte[] mgba = new byte[0x61000]; BinaryPrimitives.WriteUInt32LittleEndian(mgba,0x01000007);
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x1DED7E51);
        Span<byte> Ram(uint address,int size) => mgba.AsSpan((int)(address >= 0x03000000 ? 0x19000 + address - 0x03000000 : 0x21000 + address - 0x02000000),size);
        BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x0300500C,4),0x02001000);
        BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x03005010,4),0x02010000);
        StringConverter3.SetString(Ram(0x02001000,7),"TESTER",7,false,StringConverterOption.ClearFF);
        Ram(0x02024029,1)[0]=1; originalFirst.CopyTo(Ram(0x02024284,100));
        originalFirst.AsSpan(0,80).CopyTo(Ram(0x02010000 + 4 + (uint)(32*80),80));
        string mgbaJson=VbaParty.InspectMgba(mgba) ?? throw new Exception("mGBA profile missing"); using var decodedMgba=JsonDocument.Parse(mgbaJson);
        Require(decodedMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2 && decodedMgba.RootElement.GetProperty("pokemon")[1].GetProperty("position").GetString()=="Box 02 Slot 03", "mGBA party and PC");
        foreach(uint crc in new uint[]{0x178CEAA6,0xAE6BE484,0x9165F8A3,0xFFA7854B,0xF7C447B1,0xC9B11249,0x935ECB8B,0x1C12FD39}) {
            BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),crc);using var seededMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Seeded game CRC missing"));Require(seededMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"seeded revision synthetic mGBA party/PC");
        }
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x532F54BC);StringConverter3.SetString(Ram(0x02024EA4,7),"TESTER",7,false,StringConverterOption.ClearFF);Ram(0x03004350,1)[0]=1;originalFirst.CopyTo(Ram(0x03004360,100));originalFirst.AsSpan(0,80).CopyTo(Ram(0x020300A0+4+(uint)(32*80),80));
        foreach(uint crc in new uint[]{0x532F54BC,0x84C3A0D6,0x7D43AEE4,0xE4C181BF,0x4EA40939,0x52FF506E,0x575C5ECD,0xCF4F1957}){BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),crc);using var rubyMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Ruby mGBA profile missing"));Require(rubyMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Ruby mGBA synthetic party/PC");}
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x1B921419);testEevee.WriteEncryptedDataParty(Ram(0x03004360,100));testEevee.WriteEncryptedDataStored(Ram(0x020300A0+4+(uint)(32*80),80));
        using(var topazMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Topaz mGBA profile missing"))) Require(topazMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Topaz Ruby mGBA real Eevee party/PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x2BED9BD8);testCynda.WriteEncryptedDataParty(Ram(0x02024284,100));testCynda.WriteEncryptedDataStored(Ram(0x02010000+4+(uint)(32*80),80));
        using(var marbleMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Marble mGBA profile missing")))Require(marbleMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Marble synthetic mGBA real party/PC");
        originalFirst.CopyTo(Ram(0x02024284,100));originalFirst.AsSpan(0,80).CopyTo(Ram(0x02010000+4+(uint)(32*80),80));
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x13548284);
        using(var classicMgba=JsonDocument.Parse(ClassicRecovered.InspectMgba(mgba) ?? throw new Exception("Dark Rising mGBA profile missing")))
            Require(classicMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2 && classicMgba.RootElement.GetProperty("pokemon")[1].GetProperty("position").GetString()=="Box 02 Slot 03","Dark Rising mGBA synthetic party/PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x96D04AD1);
        using(var crystalMgba=JsonDocument.Parse(ClassicRecovered.InspectMgba(mgba) ?? throw new Exception("Liquid Crystal mGBA profile missing")))
            Require(crystalMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Liquid Crystal mGBA synthetic party/PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0xB82CE606); BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x03005D90,4),0x02001000);BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x03005D94,4),0x02010000);Ram(0x020244E9,1)[0]=1;originalFirst.CopyTo(Ram(0x020244EC,100));
        using(var oceanMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Liquid Ocean mGBA profile missing"))) Require(oceanMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2 && oceanMgba.RootElement.GetProperty("pokemon")[1].GetProperty("position").GetString()=="Box 02 Slot 03","Liquid Ocean mGBA synthetic party/PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0xA3938FD1);
        using(var shardsMgba=JsonDocument.Parse(VbaParty.InspectMgba(mgba)??throw new Exception("Crystal Shards mGBA profile missing"))) Require(shardsMgba.RootElement.GetProperty("pokemon").GetArrayLength()==2,"Crystal Shards mGBA synthetic party/PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x6A2B8EE2);
        using (var thunder = JsonDocument.Parse(VbaParty.InspectMgba(mgba) ?? throw new Exception("Thunder profile missing")))
            Require(thunder.RootElement.GetProperty("pokemon").GetArrayLength() == 2, "Thunder Emblem mGBA synthetic PC");
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x61EF78C2);
        var johto = new PK3(originalFirst) { MetLocation = 143 }; johto.RefreshChecksum(); johto.WriteEncryptedDataParty(Ram(0x02024284,100));
        using (var shiny = JsonDocument.Parse(VbaParty.InspectMgba(mgba) ?? throw new Exception("Shiny Gold profile missing"))) {
            var johtoEntry = shiny.RootElement.GetProperty("pokemon")[0]; var portableJohto = new PK6(johtoEntry.GetProperty("bytes").GetBytesFromBase64());
            Require(portableJohto.Version == GameVersion.HG && PKHeX.Everywhere.RomHacks.Expansion.NativeLocations.DisplayLocation(portableJohto) == "the Johto region", "verified New Bark location uses regional Johto mapping");
        }
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x82F643BF);
        var kanto = new PK3(originalFirst) { MetLocation = 88 }; kanto.RefreshChecksum(); kanto.WriteEncryptedDataParty(Ram(0x02024284,100));
        using (var lightning = JsonDocument.Parse(VbaParty.InspectMgba(mgba) ?? throw new Exception("Lightning Yellow profile missing"))) {
            var kantoEntry = lightning.RootElement.GetProperty("pokemon")[0]; var portableKanto = new PK6(kantoEntry.GetProperty("bytes").GetBytesFromBase64());
            Require(portableKanto.Version == GameVersion.HG && PKHeX.Everywhere.RomHacks.Expansion.NativeLocations.DisplayLocation(portableKanto) == "the Kanto region", "verified Pallet location uses regional Kanto mapping");
        }
        BinaryPrimitives.WriteUInt32LittleEndian(mgba.AsSpan(8),0x912918D8);
        using (var gary = JsonDocument.Parse(VbaParty.InspectMgba(mgba) ?? throw new Exception("Gary Edition profile missing"))) {
            var garyEntry = gary.RootElement.GetProperty("pokemon")[0]; var portableGary = new PK6(garyEntry.GetProperty("bytes").GetBytesFromBase64());
            Require(portableGary.Version == GameVersion.HG && PKHeX.Everywhere.RomHacks.Expansion.NativeLocations.DisplayLocation(portableGary) == "the Kanto region", "Gary verified Pallet location uses regional Kanto mapping");
        }
        BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x03005010,4),0x0203fff0);
        try { VbaParty.InspectMgba(mgba); throw new Exception("invalid mGBA storage pointer accepted"); } catch (InvalidDataException) { }
        Console.WriteLine("PASS VBA, battery and mGBA readers, PK6, native archives, box position, unmatched species and invalid inputs");
    }
}
