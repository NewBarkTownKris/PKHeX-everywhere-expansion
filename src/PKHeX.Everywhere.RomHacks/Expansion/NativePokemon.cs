using PKHeX.Core;
using PKHeX.Facade.Abstractions;
using PKHeX.Everywhere.RomHacks.Cfru;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Everywhere.RomHacks.Expansion;

public sealed class NativePokemon : PKM, ISpeciesIndex, IHeldItemIndex, IPortablePokemon, IShinyState
{
    public SourceProfile Profile { get; }
    public void SetShinyState(bool shiny) => Set("outer.shinyModifier", ((ShinyXor < Profile.At("SHINY_ODDS")) ^ shiny) ? 1u : 0u);
    public PKM ToPortable() => NativePortable.Export(this);
    public NativePokemon() : this(SourceProfile.Find("rocket-hoenn")) { NativeRecord = false; }
    public NativePokemon(SourceProfile profile) : this(profile, new byte[profile.At("sizeof.Pokemon")]) { }
    public NativePokemon(SourceProfile profile, Memory<byte> data) : base(Widen(profile, data)) => Profile = profile;
    private static byte[] Widen(SourceProfile p, Memory<byte> data) { var result = new byte[p.At("sizeof.Pokemon")]; data.Span.CopyTo(result); return result; }
    public bool StatsDirty { get; private set; }
    internal bool NativeRecord { get; private set; } = true;
    public override NativePokemon Clone() => new(Profile, Data.ToArray()) { StatsDirty = StatsDirty, NativeRecord = NativeRecord };
    public uint Read(string name)
    {
        if (!Profile.BitFields.TryGetValue(name, out var bits)) return 0;
        uint value = 0; for (int i = 0; i < bits.Length; i++) if ((Data[bits[i] / 8] & (1 << (bits[i] % 8))) != 0) value |= 1u << i;
        return value;
    }
    public void Set(string name, uint value)
    {
        if (!Profile.BitFields.TryGetValue(name, out var bits)) return;
        if (value != Read(name) && (name == "species" || name == "experience" || name == "outer.hiddenNatureModifier" || name.EndsWith("IV") || name.EndsWith("EV"))) StatsDirty = true;
        if (bits.Length < 32 && value >= (1u << bits.Length)) throw new ArgumentOutOfRangeException(name);
        for (int i = 0; i < bits.Length; i++) { int at = bits[i] / 8; byte mask = (byte)(1 << (bits[i] % 8)); Data[at] = (byte)((Data[at] & ~mask) | (((value >> i) & 1) != 0 ? mask : 0)); }
    }
    private byte[] NameBytes() { var result = new byte[MaxStringLengthNickname]; NicknameTrash.CopyTo(result); if (!Profile.Compact) { result[10] = (byte)Read("nickname11"); result[11] = (byte)Read("nickname12"); if (result[10] == 0 && result[11] == 0) result[10] = result[11] = 0xFF; } return result; }
    public override string Nickname { get => StringConverter3.GetString(NameBytes(), Language); set { var b = NameBytes(); StringConverter3.SetString(b, value, MaxStringLengthNickname, Language, StringConverterOption.None); b.AsSpan(0, NicknameTrash.Length).CopyTo(NicknameTrash); if (!Profile.Compact) { Set("nickname11", b[10]); Set("nickname12", b[11]); } } }
    public override Span<byte> NicknameTrash => Data.Slice(Profile.At("BoxPokemon.nickname"), Profile.Compact ? 12 : 10);
    public override Span<byte> OriginalTrainerTrash => Data.Slice(Profile.At("BoxPokemon.otName"), 7);
    public override string OriginalTrainerName { get => StringConverter3.GetString(OriginalTrainerTrash, Language); set => StringConverter3.SetString(OriginalTrainerTrash, value, 7, Language, StringConverterOption.None); }
    public override int TrashCharCountTrainer => 7;
    public override int TrashCharCountNickname => NicknameTrash.Length;
    public override int MaxStringLengthTrainer => 7;
    public override int MaxStringLengthNickname => 12;
    public override EntityContext Context => EntityContext.Gen9;
    public override PersonalInfo PersonalInfo => Profile.NativePersonal.GetValueOrDefault(SpeciesIndex) ?? Profile.Species.Personal.GetFormEntry(Species, Form);
    public override int SIZE_PARTY => Profile.At("sizeof.Pokemon");
    public override int SIZE_STORED => Profile.At("sizeof.BoxPokemon");
    public override bool ChecksumValid => Profile.Compact ? Read("outer.reservedOuter") == 0 && Read("reserved7") == 0 && Read("reserved8") == 0 : ReadUInt16LittleEndian(Data[28..]) == Checksums.Add16(Data[32..SIZE_STORED]);
    public override bool Valid { get => ChecksumValid && Read("outer.isBadEgg") == 0; set => Set("outer.isBadEgg", value ? 0u : 1u); }
    public override void RefreshChecksum() { if (!Profile.Compact) WriteUInt16LittleEndian(Data[28..], Checksums.Add16(Data[32..SIZE_STORED])); }
    protected override void EncryptStored(Span<byte> stored) { if (!Profile.Compact) { WriteUInt16LittleEndian(stored[28..], Checksums.Add16(stored[32..])); PokeCrypto.Encrypt3(stored); } }
    protected override void EncryptParty(Span<byte> party) { }
    public override uint PID { get => ReadUInt32LittleEndian(Data); set { var nature = Nature; WriteUInt32LittleEndian(Data, value); Nature = nature; } }
    public override uint ID32 { get => ReadUInt32LittleEndian(Data[4..]); set => WriteUInt32LittleEndian(Data[4..], value); }
    public override ushort TID16 { get => (ushort)ID32; set => ID32 = (ID32 & 0xFFFF0000) | value; }
    public override ushort SID16 { get => (ushort)(ID32 >> 16); set => ID32 = (ID32 & 65535) | ((uint)value << 16); }
    public override uint EncryptionConstant { get => PID; set { } }
    public override int Language { get => (int)Read("outer.language"); set => Set("outer.language", (uint)value); }
    public override Nature Nature { get => (Nature)((PID % 25) ^ Read("outer.hiddenNatureModifier")); set => Set("outer.hiddenNatureModifier", (PID % 25) ^ (uint)value); }
    public ushort SpeciesIndex { get => (ushort)Read("species"); set { Set("species", value); Set("outer.hasSpecies", value == 0 ? 0u : 1u); } }
    public override ushort Species { get => Profile.Species.ToSpeciesId(SpeciesIndex); set { if (value != Species && Profile.Species.ToIndex(value) is { } i) SpeciesIndex = i; } }
    public override byte Form { get => Profile.Species.ToNational(SpeciesIndex).Form; set { if (value != Form && Profile.Species.ToIndex(new CfruSpecies(Species, value)) is { } i) SpeciesIndex = i; } }
    public ushort HeldItemIndex { get => (ushort)Read("heldItem"); set => Set("heldItem", value); }
    public override int HeldItem { get => Profile.Items.ToModern(HeldItemIndex); set { if (value != HeldItem && Profile.Items.ToIndex((ushort)value) is { } i) HeldItemIndex = i; } }
    public override uint EXP { get => Read("experience"); set => Set("experience", value); }
    public override int AbilityNumber { get => 1 << (int)Math.Min(Read("abilityNum"), 2); set => Set("abilityNum", value switch { 2 => 1u, 4 => 2u, _ => 0u }); }
    public override int Ability { get => PersonalInfo.GetAbilityAtIndex((int)Math.Min(Read("abilityNum"), 2)); set { int slot = PersonalInfo.GetIndexOfAbility(value); if (slot >= 0) Set("abilityNum", (uint)slot); } }
    public override byte Ball { get => Read("pokeball") < Profile.Balls.Length ? (byte)Profile.Balls[Read("pokeball")] : (byte)0; set { if (value == Ball) return; var i = Array.IndexOf(Profile.Balls, (Ball)value); if (i >= 0) Set("pokeball", (uint)i); } }
    public override byte OriginalTrainerGender { get => (byte)Read("otGender"); set => Set("otGender", value); }
    public override byte OriginalTrainerFriendship { get => (byte)Read("friendship"); set => Set("friendship", value); }
    public override byte CurrentFriendship { get => OriginalTrainerFriendship; set => OriginalTrainerFriendship = value; }
    public override ushort MetLocation { get => (ushort)(Read("metLocation") | (Read("metLocationHiBit0") << 8) | (Read("metLocationHiBits1_5") << 9) | (Read("metLocationHiBit6") << 14)); set { Set("metLocation", (byte)value); Set("metLocationHiBit0", (uint)(value >> 8) & 1); Set("metLocationHiBits1_5", (uint)(value >> 9) & 31); Set("metLocationHiBit6", (uint)(value >> 14) & 1); } }
    public override byte MetLevel { get => (byte)Read("metLevel"); set => Set("metLevel", value); }
    public override GameVersion Version { get => (GameVersion)Read("metGame"); set => Set("metGame", (uint)value); }
    public override bool FatefulEncounter { get => Read(Profile.Compact ? "outer.modernFatefulEncounter" : "modernFatefulEncounter") != 0; set => Set(Profile.Compact ? "outer.modernFatefulEncounter" : "modernFatefulEncounter", value ? 1u : 0u); }
    public override bool IsEgg { get => Read("isEgg") != 0; set { Set("isEgg", value ? 1u : 0u); Set("outer.isEgg", value ? 1u : 0u); } }
    public override bool IsNicknamed { get => !string.Equals(Nickname, ((Species)Species).ToString(), StringComparison.OrdinalIgnoreCase); set { } }
    public override byte Gender { get => EntityGender.GetFromPIDAndRatio(PID, PersonalInfo.Gender); set { } }
    public override byte CurrentHandler { get => 0; set { } }
    public override ushort EggLocation { get => 0; set { } }
    public override uint PSV => ((PID >> 16) ^ (PID & 65535)) >> 3;
    public override uint TSV => (uint)(TID16 ^ SID16) >> 3;
    public override bool IsShiny => (ShinyXor < Profile.At("SHINY_ODDS")) ^ (Read("outer.shinyModifier") != 0);
    public override int Characteristic => -1;
    public override int PokerusDays { get => (int)Read("pokerus") & 15; set => Set("pokerus", (Read("pokerus") & 240) | (uint)value); }
    public override int PokerusStrain { get => (int)Read("pokerus") >> 4; set => Set("pokerus", (Read("pokerus") & 15) | ((uint)value << 4)); }
    private ushort Move(int i) => Profile.Moves.ToNational((ushort)Read("move" + i));
    private void Move(int i, ushort v) { if (Profile.Moves.ToIndex(v) is { } m) Set("move" + i, m); }
    public override ushort Move1 { get => Move(1); set => Move(1, value); }
    public override ushort Move2 { get => Move(2); set => Move(2, value); }
    public override ushort Move3 { get => Move(3); set => Move(3, value); }
    public override ushort Move4 { get => Move(4); set => Move(4, value); }
    public override int Move1_PP { get => (int)Read("pp1"); set => Set("pp1", (uint)value); }
    public override int Move2_PP { get => (int)Read("pp2"); set => Set("pp2", (uint)value); }
    public override int Move3_PP { get => (int)Read("pp3"); set => Set("pp3", (uint)value); }
    public override int Move4_PP { get => (int)Read("pp4"); set => Set("pp4", (uint)value); }
    private int Up(int i) => (int)(Read("ppBonuses") >> (i * 2)) & 3; private void Up(int i, int v) => Set("ppBonuses", (Read("ppBonuses") & ~(3u << (i * 2))) | ((uint)v << (i * 2)));
    public override int Move1_PPUps { get => Up(0); set => Up(0, value); }
    public override int Move2_PPUps { get => Up(1); set => Up(1, value); }
    public override int Move3_PPUps { get => Up(2); set => Up(2, value); }
    public override int Move4_PPUps { get => Up(3); set => Up(3, value); }
    public override int IV_HP { get => (int)Read("hpIV"); set => Set("hpIV", (uint)value); }
    public override int IV_ATK { get => (int)Read("attackIV"); set => Set("attackIV", (uint)value); }
    public override int IV_DEF { get => (int)Read("defenseIV"); set => Set("defenseIV", (uint)value); }
    public override int IV_SPE { get => (int)Read("speedIV"); set => Set("speedIV", (uint)value); }
    public override int IV_SPA { get => (int)Read("spAttackIV"); set => Set("spAttackIV", (uint)value); }
    public override int IV_SPD { get => (int)Read("spDefenseIV"); set => Set("spDefenseIV", (uint)value); }
    public override int EV_HP { get => (int)Read("hpEV"); set => Set("hpEV", (uint)value); }
    public override int EV_ATK { get => (int)Read("attackEV"); set => Set("attackEV", (uint)value); }
    public override int EV_DEF { get => (int)Read("defenseEV"); set => Set("defenseEV", (uint)value); }
    public override int EV_SPE { get => (int)Read("speedEV"); set => Set("speedEV", (uint)value); }
    public override int EV_SPA { get => (int)Read("spAttackEV"); set => Set("spAttackEV", (uint)value); }
    public override int EV_SPD { get => (int)Read("spDefenseEV"); set => Set("spDefenseEV", (uint)value); }
    private int Stat(string name) => ReadUInt16LittleEndian(Data[Profile.At("Pokemon." + name)..]); private void Stat(string name, int value) => WriteUInt16LittleEndian(Data[Profile.At("Pokemon." + name)..], (ushort)value);
    public override int Status_Condition { get => ReadInt32LittleEndian(Data[Profile.At("Pokemon.status")..]); set => WriteInt32LittleEndian(Data[Profile.At("Pokemon.status")..], value); }
    public override byte Stat_Level { get => Data[Profile.At("Pokemon.level")]; set => Data[Profile.At("Pokemon.level")] = value; }
    public override int Stat_HPCurrent { get => Stat("hp"); set { Stat("hp", value); KeepHP(); } }
    public override int Stat_HPMax { get => Stat("maxHP"); set { Stat("maxHP", value); KeepHP(); } }
    private void KeepHP() => Set("outer.hpLost", (uint)Math.Clamp(Stat_HPMax - Stat_HPCurrent, 0, 16383));
    public override int Stat_ATK { get => Stat("attack"); set => Stat("attack", value); }
    public override int Stat_DEF { get => Stat("defense"); set => Stat("defense", value); }
    public override int Stat_SPE { get => Stat("speed"); set => Stat("speed", value); }
    public override int Stat_SPA { get => Stat("spAttack"); set => Stat("spAttack", value); }
    public override int Stat_SPD { get => Stat("spDefense"); set => Stat("spDefense", value); }
    public override ushort MaxMoveID => (ushort)((int)PKHeX.Core.Move.MAX_COUNT - 1); public override ushort MaxSpeciesID => (ushort)((int)PKHeX.Core.Species.MAX_COUNT - 1); public override int MaxAbilityID => (int)PKHeX.Core.Ability.MAX_COUNT - 1; public override int MaxItemID => 65535; public override int MaxBallID => (int)PKHeX.Core.Ball.Strange; public override GameVersion MaxGameID => GameVersion.CXD; public override int MaxIV => 31; public override int MaxEV => 252;
    public override string GetString(ReadOnlySpan<byte> data) => StringConverter3.GetString(data, Language); public override int LoadString(ReadOnlySpan<byte> data, Span<char> text) => StringConverter3.LoadString(data, text, Language); public override int SetString(Span<byte> data, ReadOnlySpan<char> text, int maxLength, StringConverterOption option) => StringConverter3.SetString(data, text, maxLength, Language, option); public override int GetStringTerminatorIndex(ReadOnlySpan<byte> data) => TrashBytes8.GetTerminatorIndex(data); public override int GetStringLength(ReadOnlySpan<byte> data) => TrashBytes8.GetStringLength(data); public override int GetBytesPerChar() => 1;
}
