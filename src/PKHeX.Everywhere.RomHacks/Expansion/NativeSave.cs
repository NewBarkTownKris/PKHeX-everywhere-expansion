using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Cfru;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Everywhere.RomHacks.Expansion;

public sealed class NativeSave : SaveFile, IBoxDetailName
{
    public SourceProfile Profile { get; }
    private readonly int[] _sectors;
    private readonly int _slot;
    private readonly byte[] _block1;
    private readonly byte[] _storage;
    public NativeSave(SourceProfile profile, byte[] data) : base(data.ToArray())
    {
        Profile = profile;
        if (!TrySlot(profile, data, out _sectors, out _slot)) throw new InvalidDataException("The save does not match this source profile or its sector checksums.");
        _block1 = new byte[profile.At("sizeof.SaveBlock1")]; CopyBlock(_block1, 1, false);
        _storage = new byte[profile.At("sizeof.PokemonStorage")]; CopyStorage(false);
        if (PartyCount > 6 || CurrentBox >= BoxCount) throw new InvalidDataException("Invalid party or storage header.");
        if (profile.Compact && !ValidateCompactStorage()) throw new InvalidDataException("Soulgold storage or auxiliary checksums failed.");
        foreach (var mon in Enumerable.Range(0, PartyCount).Select(GetPartySlotAtIndex).Concat(BoxData).Cast<NativePokemon>())
            if (!mon.Valid) throw new InvalidDataException("A Pokémon checksum failed. No data was changed.");
    }
    internal static bool TrySlot(SourceProfile p, ReadOnlySpan<byte> data, out int[] sectors, out int slot)
    {
        sectors = []; slot = -1; if (!Gen3SaveSize.IsSupported(data.Length)) return false;
        uint newest = 0;
        for (int s = 0; s < 2; s++)
        {
            var candidate = new int[14]; Array.Fill(candidate, -1); uint? counter = null; bool valid = true;
            for (int i = 0; i < 14; i++)
            {
                int offset = (s * 14 + i) * 4096; var sector = data.Slice(offset, 4096);
                int id = ReadUInt16LittleEndian(sector[4084..]); uint signature = ReadUInt32LittleEndian(sector[4088..]); uint c = ReadUInt32LittleEndian(sector[4092..]);
                uint expected = (uint)p.At(p.Compact ? "SECTOR_SIGNATURE_BX21" : "SECTOR_SIGNATURE");
                if (id >= 14 || candidate[id] >= 0 || signature != expected || (counter.HasValue && counter != c) || ReadUInt16LittleEndian(sector[4086..]) != Checksums.CheckSum32(sector[..p.ChecksumLengths[id]])) { valid = false; break; }
                candidate[id] = offset; counter = c;
            }
            if (valid && p.Layout.TryGetValue("SAVE_VERSION", out var version))
            {
                int magic = p.At("SaveBlock1.saveVersionMagic"); int at = candidate[1 + magic / 3968] + magic % 3968;
                valid = ReadUInt16LittleEndian(data[(candidate[1] + p.At("SaveBlock1.saveVersion"))..]) == version && ReadUInt32LittleEndian(data[at..]) == (uint)p.At("SAVE_VERSION_MAGIC");
            }
            if (valid && (slot < 0 || unchecked((int)(counter!.Value - newest)) > 0)) { slot = s; newest = counter!.Value; sectors = candidate; }
        }
        return slot >= 0;
    }
    private Span<byte> Sector(int id) => Data.Slice(_sectors[id], 4096);
    private void CopyBlock(byte[] block, int first, bool toSave)
    {
        for (int pos = 0; pos < block.Length; pos += 3968) { int count = Math.Min(3968, block.Length - pos); var sector = Sector(first + pos / 3968)[..count]; if (toSave) block.AsSpan(pos, count).CopyTo(sector); else sector.CopyTo(block.AsSpan(pos, count)); }
    }
    private void CopyStorage(bool toSave)
    {
        if (!Profile.Compact) { CopyBlock(_storage, 5, toSave); return; }
        for (int pos = 0; pos < 35712; pos += 3968) CopyPart(pos, Sector(5 + pos / 3968)[..3968], toSave);
        CopyPart(35712, Data.Slice((30 + _slot) * 4096, 3968), toSave);
        CopyPart(39680, _block1.AsSpan(Profile.At("SaveBlock1.pokemonStorageExtensionTail"), 1944), toSave);
        CopyPart(41624, Data.Slice((28 + _slot) * 4096 + 12, 1832), toSave);
    }
    private void CopyPart(int pos, Span<byte> target, bool toSave) { if (toSave) _storage.AsSpan(pos, target.Length).CopyTo(target); else target.CopyTo(_storage.AsSpan(pos, target.Length)); }
    private bool ValidateCompactStorage()
    {
        const uint magic = 0x31325842;
        if (ReadUInt32LittleEndian(_storage) != magic || ReadUInt32LittleEndian(_storage.AsSpan(12)) != ~magic || ReadUInt16LittleEndian(_storage.AsSpan(4)) != 1 || ReadUInt16LittleEndian(_storage.AsSpan(6)) != _storage.Length - 16) return false;
        ushort checksum = ReadUInt16LittleEndian(_storage.AsSpan(8)); if (checksum != Checksums.CheckSum32(_storage.AsSpan(16)) || ReadUInt16LittleEndian(_storage.AsSpan(10)) != (ushort)~checksum) return false;
        foreach (int sectorIndex in new[] { 28 + _slot, 30 + _slot })
        {
            var sector = Data.Slice(sectorIndex * 4096, 4096);
            if (ReadUInt16LittleEndian(sector[4084..]) != sectorIndex || ReadUInt32LittleEndian(sector[4088..]) != (uint)Profile.At("SECTOR_SIGNATURE_BX21") || ReadUInt32LittleEndian(sector[4092..]) != ReadUInt32LittleEndian(Sector(0)[4092..]) || ReadUInt16LittleEndian(sector[4086..]) != Checksums.CheckSum32(sector[..3968])) return false;
        }
        var aux = Data.Slice((28 + _slot) * 4096, 3968);
        ushort payload = ReadUInt16LittleEndian(aux[6..]); ushort check = ReadUInt16LittleEndian(aux[8..]);
        return ReadUInt32LittleEndian(aux) == magic && ReadUInt16LittleEndian(aux[4..]) == 1 && payload == 3420 && ReadUInt16LittleEndian(aux[10..]) == (ushort)~check && check == Checksums.CheckSum32(aux.Slice(12, payload));
    }
    protected override Memory<byte> GetFinalData()
    {
        if (Profile.Compact) { ushort check = Checksums.CheckSum32(_storage.AsSpan(16)); WriteUInt16LittleEndian(_storage.AsSpan(8), check); WriteUInt16LittleEndian(_storage.AsSpan(10), (ushort)~check); }
        CopyStorage(true); CopyBlock(_block1, 1, true);
        if (Profile.Compact)
        {
            var aux = Data.Slice((28 + _slot) * 4096, 3968); ushort check = Checksums.CheckSum32(aux.Slice(12, 3420)); WriteUInt16LittleEndian(aux[8..], check); WriteUInt16LittleEndian(aux[10..], (ushort)~check);
            foreach (int index in new[] { 28 + _slot, 30 + _slot }) { var sector = Data.Slice(index * 4096, 4096); WriteUInt16LittleEndian(sector[4086..], Checksums.CheckSum32(sector[..3968])); }
        }
        SetChecksums(); return Data.ToArray();
    }
    protected override void SetChecksums() { for (int id = 0; id < 14; id++) WriteUInt16LittleEndian(Sector(id)[4086..], Checksums.CheckSum32(Sector(id)[..Profile.ChecksumLengths[id]])); }
    public override bool ChecksumsValid => Enumerable.Range(0, 14).All(id => ReadUInt16LittleEndian(Sector(id)[4086..]) == Checksums.CheckSum32(Sector(id)[..Profile.ChecksumLengths[id]]));
    public override string ChecksumInfo => ChecksumsValid ? "Source profile checksums are valid." : "A source profile checksum is invalid.";
    protected override NativeSave CloneInternal() => new(Profile, GetFinalData().ToArray());
    public override string Extension => ".sav";
    protected override string ShortSummary => $"{OT} ({Profile.Name})";
    public override GameVersion Version { get => GameVersion.E; set { } }
    public override byte Generation => 9; public override EntityContext Context => EntityContext.Gen9;
    public override int Language { get => 2; set { } }
    public override IPersonalTable Personal => Profile.Species.Personal;
    public override Type PKMType => typeof(NativePokemon); public override NativePokemon BlankPKM => new(Profile);
    protected override NativePokemon GetPKM(Memory<byte> data) => new(Profile, data);
    protected override void DecryptPKM(Span<byte> data) { if (!Profile.Compact) PokeCrypto.Decrypt3(data); }
    public override int PartyCount { get => _block1[Profile.At("SaveBlock1.playerPartyCount")]; protected set => _block1[Profile.At("SaveBlock1.playerPartyCount")] = (byte)value; }
    protected override Span<byte> PartyBuffer => _block1.AsSpan(Profile.At("SaveBlock1.playerParty"), 6 * SIZE_PARTY);
    protected override Span<byte> BoxBuffer => _storage;
    public override int GetPartyOffset(int slot) => slot * SIZE_PARTY;
    public override int GetBoxOffset(int box) => Profile.At("PokemonStorage.boxes") + box * BoxSlotCount * SIZE_STORED;
    public override int BoxCount => Profile.At("TOTAL_BOXES_COUNT"); public override int BoxSlotCount => Profile.At("IN_BOX_COUNT");
    public override int CurrentBox { get => _storage[Profile.At("PokemonStorage.currentBox")]; set => _storage[Profile.At("PokemonStorage.currentBox")] = (byte)value; }
    public override int SIZE_STORED => Profile.At("sizeof.BoxPokemon"); public override int SIZE_PARTY => Profile.At("sizeof.Pokemon");
    public override bool IsPKMPresent(ReadOnlySpan<byte> data) { var bits = Profile.BitFields["outer.hasSpecies"]; return (data[bits[0] / 8] & (1 << (bits[0] % 8))) != 0; }
    protected override void SetPartyValues(PKM pk, bool isParty) { if (isParty && pk is NativePokemon { StatsDirty: true }) pk.ResetPartyStats(); }
    protected override void SetPKM(PKM pk, bool isParty) { if (pk is not NativePokemon n || n.Profile.Id != Profile.Id || !n.NativeRecord) throw new InvalidOperationException("Cross-profile import needs a conversion preview."); }
    public override int MaxStringLengthTrainer => 7; public override int MaxStringLengthNickname => 12;
    public override ushort MaxMoveID => BlankPKM.MaxMoveID; public override ushort MaxSpeciesID => BlankPKM.MaxSpeciesID; public override int MaxAbilityID => BlankPKM.MaxAbilityID; public override int MaxItemID => BlankPKM.MaxItemID; public override int MaxBallID => BlankPKM.MaxBallID; public override GameVersion MaxGameID => BlankPKM.MaxGameID; public override int MaxEV => 252; public override ReadOnlySpan<ushort> HeldItems => Profile.Items.HeldItems;
    public override string OT { get => GetString(Sector(0).Slice(Profile.At("SaveBlock2.playerName"), 7)); set => SetString(Sector(0).Slice(Profile.At("SaveBlock2.playerName"), 7), value, 7, StringConverterOption.ClearFF); }
    public override byte Gender { get => Sector(0)[Profile.At("SaveBlock2.playerGender")]; set => Sector(0)[Profile.At("SaveBlock2.playerGender")] = value; }
    public override uint ID32 { get => ReadUInt32LittleEndian(Sector(0)[Profile.At("SaveBlock2.playerTrainerId")..]); set => WriteUInt32LittleEndian(Sector(0)[Profile.At("SaveBlock2.playerTrainerId")..], value); }
    public override ushort TID16 { get => (ushort)ID32; set => ID32 = (ID32 & 0xFFFF0000) | value; }
    public override ushort SID16 { get => (ushort)(ID32 >> 16); set => ID32 = (ID32 & 65535) | ((uint)value << 16); }
    internal uint EncryptionKey => ReadUInt32LittleEndian(Sector(0)[Profile.At("SaveBlock2.encryptionKey")..]);
    public override uint Money { get => ReadUInt32LittleEndian(_block1.AsSpan(Profile.At("SaveBlock1.money"))) ^ EncryptionKey; set => WriteUInt32LittleEndian(_block1.AsSpan(Profile.At("SaveBlock1.money")), value ^ EncryptionKey); }
    internal Span<byte> Block1 => _block1;
    public override PlayerBag Inventory => new NativeBag(this);
    public override string GetString(ReadOnlySpan<byte> data) => StringConverter3.GetString(data, false); public override int LoadString(ReadOnlySpan<byte> data, Span<char> text) => StringConverter3.LoadString(data, text, false); public override int SetString(Span<byte> data, ReadOnlySpan<char> text, int maxLength, StringConverterOption option) => StringConverter3.SetString(data, text, maxLength, false, option);
    public string GetBoxName(int box) => GetString(_storage.AsSpan(Profile.At("PokemonStorage.boxNames") + box * 9, 9));
    public void SetBoxName(int box, ReadOnlySpan<char> name) => SetString(_storage.AsSpan(Profile.At("PokemonStorage.boxNames") + box * 9, 9), name, 8, StringConverterOption.ClearFF);
}
