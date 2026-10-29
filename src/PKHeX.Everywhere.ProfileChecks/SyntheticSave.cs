using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Expansion;
using static System.Buffers.Binary.BinaryPrimitives;

static class SyntheticSave
{
    public static byte[] Create(SourceProfile p)
    {
        var data=new byte[131088];Array.Fill(data,(byte)0xA5);
        for(int i=0;i<14;i++)Array.Clear(data,i*4096,4096);
        var block1=new byte[p.At("sizeof.SaveBlock1")];var block2=new byte[p.At("sizeof.SaveBlock2")];var storage=new byte[p.At("sizeof.PokemonStorage")];
        StringConverter3.SetString(block2.AsSpan(0,7),"TESTER",7,false,StringConverterOption.ClearFF);
        WriteUInt32LittleEndian(block2.AsSpan(p.At("SaveBlock2.playerTrainerId")),0x11223344);
        block1[p.At("SaveBlock1.playerPartyCount")]=1;
        if(p.Layout.ContainsKey("SAVE_VERSION")){WriteUInt16LittleEndian(block1.AsSpan(p.At("SaveBlock1.saveVersion")),(ushort)p.At("SAVE_VERSION"));WriteUInt32LittleEndian(block1.AsSpan(p.At("SaveBlock1.saveVersionMagic")),unchecked((uint)p.At("SAVE_VERSION_MAGIC")));}
        var mon=new NativePokemon(p){Language=2,ID32=0x11223344,OriginalTrainerName="TESTER",Nickname="CHECK",PID=0x87654321,CurrentLevel=40};
        mon.SpeciesIndex=(ushort)p.Symbols["SPECIES"]["SPECIES_MAGNETON"];mon.CurrentLevel=40;mon.MetLevel=40;mon.Version=GameVersion.E;
        var place=p.Region=="hoenn"?"Rustboro City":"Cherrygrove City";
        mon.MetLocation=ushort.Parse(p.Locations.Single(x=>x.Value.Equals(place,StringComparison.OrdinalIgnoreCase)).Key);
        mon.Move1=85;mon.Move1_PP=15;mon.IV_HP=31;mon.IV_SPA=30;mon.ResetPartyStats();mon.RefreshChecksum();
        var party=new byte[mon.SIZE_PARTY];mon.WriteEncryptedDataParty(party);party.CopyTo(block1,p.At("SaveBlock1.playerParty"));
        var box=new byte[mon.SIZE_STORED];mon.WriteEncryptedDataStored(box);int final=p.At("PokemonStorage.boxes")+(p.At("TOTAL_BOXES_COUNT")*30-1)*mon.SIZE_STORED;box.CopyTo(storage,final);
        storage[p.At("PokemonStorage.currentBox")]=(byte)(p.At("TOTAL_BOXES_COUNT")-1);
        if(p.Compact)
        {
            const uint magic=0x31325842;WriteUInt32LittleEndian(storage,magic);WriteUInt16LittleEndian(storage.AsSpan(4),1);WriteUInt16LittleEndian(storage.AsSpan(6),(ushort)(storage.Length-16));
            ushort check=Checksums.CheckSum32(storage.AsSpan(16));WriteUInt16LittleEndian(storage.AsSpan(8),check);WriteUInt16LittleEndian(storage.AsSpan(10),(ushort)~check);WriteUInt32LittleEndian(storage.AsSpan(12),~magic);
            storage.AsSpan(39680,1944).CopyTo(block1.AsSpan(p.At("SaveBlock1.pokemonStorageExtensionTail")));
            Array.Clear(data,28*4096,4096);Array.Clear(data,30*4096,4096);
            storage.AsSpan(35712,3968).CopyTo(data.AsSpan(30*4096));
            var aux=data.AsSpan(28*4096,3968);WriteUInt32LittleEndian(aux,magic);WriteUInt16LittleEndian(aux[4..],1);WriteUInt16LittleEndian(aux[6..],3420);storage.AsSpan(41624,1832).CopyTo(aux[12..]);
            check=Checksums.CheckSum32(aux.Slice(12,3420));WriteUInt16LittleEndian(aux[8..],check);WriteUInt16LittleEndian(aux[10..],(ushort)~check);
        }
        PutBlock(data,block2,0);PutBlock(data,block1,1);PutBlock(data,storage.AsSpan(0,Math.Min(storage.Length,35712)).ToArray(),5);
        for(int i=0;i<14;i++)Finish(data,p,i,i,p.ChecksumLengths[i]);
        if(p.Compact){Finish(data,p,28,28,3968);Finish(data,p,30,30,3968);}
        return data;
    }
    static void PutBlock(byte[] data,byte[] block,int first){for(int pos=0;pos<block.Length;pos+=3968)block.AsSpan(pos,Math.Min(3968,block.Length-pos)).CopyTo(data.AsSpan((first+pos/3968)*4096));}
    static void Finish(byte[] data,SourceProfile p,int sector,int id,int size){var span=data.AsSpan(sector*4096,4096);WriteUInt16LittleEndian(span[4084..],(ushort)id);WriteUInt16LittleEndian(span[4086..],Checksums.CheckSum32(span[..size]));WriteUInt32LittleEndian(span[4088..],unchecked((uint)p.At(p.Compact?"SECTOR_SIGNATURE_BX21":"SECTOR_SIGNATURE")));WriteUInt32LittleEndian(span[4092..],9);}
}
