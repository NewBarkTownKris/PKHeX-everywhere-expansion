using System.Buffers.Binary;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Historic;
using PKHeX.Everywhere.RomHacks.Expansion;

static class HackRecoveryChecks
{
    public static void Run()
    {
        var mon=new PK3{Species=25,Nickname="TEST",OriginalTrainerName="TESTER",TID16=1234,Stat_Level=5,Move1=33,Ball=4,Version=GameVersion.E};mon.RefreshChecksum();
        byte[] MakeSave(int partyCount,int partyStart){var bytes=new byte[0x20000];Array.Fill(bytes,(byte)0xff);for(int i=0;i<14;i++){var sec=bytes.AsSpan(i*0x1000,0x1000);sec[..0xf80].Clear();BinaryPrimitives.WriteUInt16LittleEndian(sec[0xff4..],(ushort)i);BinaryPrimitives.WriteUInt32LittleEndian(sec[0xff8..],0x08012025);BinaryPrimitives.WriteUInt32LittleEndian(sec[0xffc..],1);}bytes[0x1000+partyCount]=1;mon.WriteEncryptedDataParty(bytes.AsSpan(0x1000+partyStart,100));mon.WriteEncryptedDataStored(bytes.AsSpan(0x5000+4+2*80,80));for(int i=0;i<14;i++){var sec=bytes.AsSpan(i*0x1000,0x1000);BinaryPrimitives.WriteUInt16LittleEndian(sec[0xff6..],Checksums.CheckSum32(sec[..0xf80]));}return bytes;}
        void Check(string json){using var doc=JsonDocument.Parse(json);var arr=doc.RootElement.GetProperty("pokemon");if(arr.GetArrayLength()!=2||arr[1].GetProperty("position").GetString()!="Box 01 Slot 03")throw new Exception("party/box extraction failed");foreach(var x in arr.EnumerateArray()){var p=new PK6(x.GetProperty("bytes").GetBytesFromBase64());if(!p.ChecksumValid||p.Species!=25||p.Move1!=33||p.TID16!=1234)throw new Exception("portable identity failed");}}
        Check(RechargedRecovery.InspectBattery(MakeSave(0x3b,0x44)));Check(LazarusRecovery.InspectBattery(MakeSave(0x234,0x238)));
        var state=new byte[0x61000];BinaryPrimitives.WriteUInt32LittleEndian(state,0x01000007);Span<byte> Ram(uint a,int n)=>state.AsSpan((int)(a>=0x03000000?0x19000+a-0x03000000:0x21000+a-0x02000000),n);
        void Setup(uint crc,uint count,uint party,uint storagePtr){Array.Clear(state,0x19000,state.Length-0x19000);BinaryPrimitives.WriteUInt32LittleEndian(state.AsSpan(8),crc);BinaryPrimitives.WriteUInt32LittleEndian(Ram(storagePtr,4),0x02010000);Ram(count,1)[0]=1;mon.WriteEncryptedDataParty(Ram(party,100));mon.WriteEncryptedDataStored(Ram(0x02010000+4+2*80,80));}
        Setup(0xDEF0F69E,0x02038559,0x0203855C,0x03005AD8);Check(RechargedRecovery.InspectMgba(state)??throw new Exception("Recharged state not routed"));BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x03005AD8,4),0x04000000);try{RechargedRecovery.InspectMgba(state);throw new Exception("Recharged bad pointer accepted");}catch(InvalidDataException){}
        Setup(0x558AE42F,0x0201B95D,0x0201B960,0x0300366C);Check(LazarusRecovery.InspectMgba(state)??throw new Exception("Lazarus state not routed"));BinaryPrimitives.WriteUInt32LittleEndian(Ram(0x0300366C,4),0x04000000);try{LazarusRecovery.InspectMgba(state);throw new Exception("Lazarus bad pointer accepted");}catch(InvalidDataException){}
        var profile=SourceProfile.Find("pokeemerald-expansion-1-17-1");var synthetic=SyntheticSave.Create(profile);using(var doc=JsonDocument.Parse(PetalburgRecovery.InspectBattery(synthetic))){if(doc.RootElement.GetProperty("pokemon").GetArrayLength()!=2)throw new Exception("Petalburg synthetic party/box");}
        Console.WriteLine("PASS Recharged Yellow, Lazarus and Petalburg synthetic recovery and invalid pointer checks");
    }
}
