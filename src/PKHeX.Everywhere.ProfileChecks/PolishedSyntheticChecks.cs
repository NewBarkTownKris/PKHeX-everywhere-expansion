using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
using PKHeX.Everywhere.RomHacks.Historic;

static class PolishedSyntheticChecks
{
    public static void Run()
    {
        var save=new byte[0x8000];save[0x2007]=97;save[0x2d0f]=127;const int start=0x285e;save[start]=2;
        for(int i=0;i<2;i++){
            int off=start+8+i*48;save[off]=(byte)(i==0?25:0x22);save[off+2]=33;save[off+6]=0x04;save[off+7]=0xd2;save[off+10]=125;save[off+17]=save[off+18]=save[off+19]=0xff;save[off+20]=(byte)(i==0?0x20:0xa0);save[off+21]=(byte)(i==0?1:0x21);save[off+22]=35;save[off+26]=70;save[off+31]=5;
            new byte[]{0x82,0x87,0x84,0x82,0x8a,0x53}.CopyTo(save,start+0x128+i*11);new byte[]{0x93,0x84,0x92,0x93,0x53}.CopyTo(save,start+0x16a+i*11);
        }
        static void SetChecksum(byte[] b){ushort v=(ushort)b.AsSpan(0x2008,0x2b83-0x2008).ToArray().Sum(x=>(int)x);BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(0x2d0d),v);}
        SetChecksum(save);string json=PolishedRecovery.InspectBattery(save);using(var doc=JsonDocument.Parse(json)){var mons=doc.RootElement.GetProperty("pokemon");if(mons.GetArrayLength()!=2)throw new Exception("Polished party count");var a=new PK6(mons[0].GetProperty("bytes").GetBytesFromBase64());var b=new PK9(mons[1].GetProperty("bytes").GetBytesFromBase64());if(!a.ChecksumValid||a.Species!=25||a.Move1!=33||a.OriginalTrainerName!="CHECK"||!b.ChecksumValid||b.Species!=980||!b.IsShiny)throw new Exception("Polished portable identity");}
        using(var zip=new ZipArchive(new MemoryStream(StateExport.Archive(json))))if(zip.GetEntry("Portable/Party 02.pk9") is null)throw new Exception("Polished PK9 archive");
        var state=new byte[0x11800];BinaryPrimitives.WriteUInt32LittleEndian(state,0x00400003);BinaryPrimitives.WriteUInt32LittleEndian(state.AsSpan(4),0xF98367E4);save.AsSpan(start,0x21c).CopyTo(state.AsSpan(0x60ce));using(var live=JsonDocument.Parse(MgbaShinRed.Inspect(state)??throw new Exception("Polished state route")))if(live.RootElement.GetProperty("pokemon").GetArrayLength()!=2)throw new Exception("Polished live party");
        var form=save.ToArray();form[start+8+21]=2;SetChecksum(form);using(var doc=JsonDocument.Parse(PolishedRecovery.InspectBattery(form)))if(doc.RootElement.GetProperty("skippedUnmapped").GetInt32()!=1)throw new Exception("Polished unknown form accepted");
        save[0x2008]^=1;try{PolishedRecovery.InspectBattery(save);throw new Exception("Polished bad checksum accepted");}catch(InvalidDataException){}
        Console.WriteLine("PASS Polished Crystal synthetic PK6/PK9, shiny, state, archive, unsupported form and checksum checks");
    }
}
