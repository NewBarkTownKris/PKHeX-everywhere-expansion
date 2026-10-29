using System.Buffers.Binary;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
namespace PKHeX.Everywhere.RomHacks.Historic;

// Pokémon Recharged Yellow 1.9.7: its SaveBlock1 party was moved to +0x44.
public static class RechargedRecovery
{
    public static string InspectBattery(byte[] data)
    {
        var inspected=HistoricGen3.InspectFireRedSave(data); // validates the complete active 14-sector slot
        int first=inspected.SourceSlot*0xE000, sector=-1;
        for(int i=0;i<14;i++)if(BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(first+i*0x1000+0xff4,2))==1){sector=first+i*0x1000;break;}
        if(sector<0)throw new InvalidDataException("Missing party sector.");int count=data[sector+0x3B];if(count>6)throw new InvalidDataException("Invalid party count.");
        var records=new List<HistoricGen3.Record>();
        for(int i=0;i<count;i++){var raw=data.AsSpan(sector+0x44+i*100,100).ToArray();if((raw[0x13]&2)==0)throw new InvalidDataException("Empty party slot.");var p=new PK3(raw.ToArray());records.Add(new($"Party {i+1:D2}",raw,p.ChecksumValid,p.SpeciesInternal,p.Nickname,p.OriginalTrainerName,(byte)p.MetLocation));}
        records.AddRange(inspected.Records.Where(x=>x.Position.StartsWith("Box",StringComparison.Ordinal)));
        return ClassicRecovered.ConvertRecords("recharged-yellow",records,"Recharged Yellow battery save");
    }
    public static string? InspectMgba(byte[] input)
    {
        byte[] state=MgbaState.ExtractCore(input);if(BinaryPrimitives.ReadUInt32LittleEndian(state)!=0x01000007)throw new InvalidDataException("Unsupported mGBA state version.");
        if(BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(8))!=0xDEF0F69E)return null;
        ReadOnlySpan<byte> Ram(uint addr,int len){long start=addr switch{>=0x02000000 and <0x02040000=>0x21000L+addr-0x02000000,>=0x03000000 and <0x03008000=>0x19000L+addr-0x03000000,_=>throw new InvalidDataException("Invalid RAM pointer.")};long end=addr<0x03000000?0x61000:0x21000;if(len<0||start+len>end)throw new InvalidDataException("RAM pointer out of bounds.");return state.AsSpan((int)start,len);}
        int count=Ram(0x02038559,1)[0];if(count>6)throw new InvalidDataException("Invalid party count.");uint ptr=BinaryPrimitives.ReadUInt32LittleEndian(Ram(0x03005AD8,4));var storage=Ram(ptr,4+14*30*80);if(BinaryPrimitives.ReadUInt32LittleEndian(storage)>=14)throw new InvalidDataException("Invalid PC box index.");
        var records=new List<HistoricGen3.Record>();void Add(string pos,byte[] raw,bool party){if((raw[0x13]&2)==0){if(party)throw new InvalidDataException("Empty party slot.");return;}var p=new PK3(raw.ToArray());records.Add(new(pos,raw,p.ChecksumValid,p.SpeciesInternal,p.Nickname,p.OriginalTrainerName,(byte)p.MetLocation));}
        for(int i=0;i<count;i++)Add($"Party {i+1:D2}",Ram(0x0203855Cu+(uint)(i*100),100).ToArray(),true);
        for(int b=0;b<14;b++)for(int s=0;s<30;s++)Add($"Box {b+1:D2} Slot {s+1:D2}",storage.Slice(4+(b*30+s)*80,80).ToArray(),false);
        return ClassicRecovered.ConvertRecords("recharged-yellow",records,"mGBA historic state",romCrc:"DEF0F69E");
    }
}
