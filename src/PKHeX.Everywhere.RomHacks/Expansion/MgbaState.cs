using System.Buffers.Binary;
using System.IO.Compression;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Expansion;

// These addresses are meaningful only for the exact ROM build identified by
// the CRC stored by mGBA. Add a descriptor only after testing against that build.
public static class MgbaState
{
    public sealed record Build(uint RomCrc, string Profile, uint Party, uint Count, uint StoragePtr, uint TrainerPtr);
    public sealed record Member(string Position, byte[] Original, NativePokemon Pokemon);
    public sealed record Result(Build Game, string Trainer, IReadOnlyList<Member> Members);

    public static readonly List<Build> Builds =
    [
        new(0xAEBB572A, "rocket-hoenn", 0x02031BC8, 0x02031BB8, 0x030051C8, 0x030051C0),
        new(0x7307A1CC, "rocket-hoenn", 0x02031BC8, 0x02031BB8, 0x030051C8, 0x030051C0),
        new(0x16AF6C3A, "rocket-hoenn", 0x02031BC8, 0x02031BB8, 0x030051C8, 0x030051C0),
        new(0x26DC8E55, "rocket-hoenn", 0x02031BC8, 0x02031BB8, 0x030051C8, 0x030051C0),
        new(0xA22CE6D7, "rocket-hoenn", 0x02031BC8, 0x02031BB8, 0x030051C8, 0x030051C0),
        new(0xA5B1DD32, "pokeemerald-expansion-1-17-1", 0x02031B64, 0x02031B54, 0x030051C8, 0x030051C0),
        new(0x42944F19, "heart-and-soul-2.0.6", 0x0203476C, 0x020342AC, 0x030041C4, 0x030041BC),
        new(0xEB4F2403, "soulgold-1.2.1", 0x02038E60, 0x02038C19, 0x030040C8, 0x030040C0),
    ];

    private const int StateSize = 0x61000;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static Result Read(byte[] input)
    {
        byte[] state = ExtractCore(input);
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(state);
        if (magic != 0x01000007) throw new InvalidDataException("This mGBA state version is not supported.");
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(state.AsSpan(8));
        var build = Builds.SingleOrDefault(x => x.RomCrc == crc)
            ?? throw new InvalidDataException($"This ROM build is not recognized (CRC {crc:X8}). A build-specific memory profile is required.");
        var profile = SourceProfile.Find(build.Profile);
        ReadOnlySpan<byte> Ram(uint address, int length)
        {
            if (length < 0) throw new InvalidDataException("Invalid memory length.");
            long offset = address switch
            {
                >= 0x02000000 and < 0x02040000 => 0x21000L + address - 0x02000000,
                >= 0x03000000 and < 0x03008000 => 0x19000L + address - 0x03000000,
                _ => throw new InvalidDataException("State memory pointer is outside GBA working RAM."),
            };
            long end = (address < 0x03000000 ? 0x61000L : 0x21000L);
            if (offset + length > end) throw new InvalidDataException("State memory pointer crosses the end of RAM.");
            return state.AsSpan((int)offset, length);
        }
        uint Pointer(uint address) => BinaryPrimitives.ReadUInt32LittleEndian(Ram(address, 4));
        var trainer = StringConverter3.GetString(Ram(Pointer(build.TrainerPtr) + (uint)profile.At("SaveBlock2.playerName"), 7), false);
        if (string.IsNullOrWhiteSpace(trainer)) throw new InvalidDataException("The state has no initialized trainer. Open an in-game state.");
        int count = Ram(build.Count, 1)[0];
        if (count > 6) throw new InvalidDataException("The state has an invalid player party count.");
        uint storage = Pointer(build.StoragePtr);
        var boxData = Ram(storage, profile.At("sizeof.PokemonStorage"));
        if (boxData[profile.At("PokemonStorage.currentBox")] >= profile.At("TOTAL_BOXES_COUNT"))
            throw new InvalidDataException("The state has an invalid PC storage header.");
        var members = new List<Member>();
        void Add(string position, ReadOnlySpan<byte> raw, bool party)
        {
            int hasSpeciesBit = profile.BitFields["outer.hasSpecies"][0];
            if ((raw[hasSpeciesBit / 8] & (1 << (hasSpeciesBit % 8))) == 0)
            {
                if (party) throw new InvalidDataException($"{position} is empty despite the party count.");
                return;
            }
            byte[] original = raw.ToArray(), decrypted = raw.ToArray();
            if (!profile.Compact) PokeCrypto.Decrypt3(decrypted.AsSpan(0, profile.At("sizeof.BoxPokemon")));
            var mon = new NativePokemon(profile, decrypted);
            if (!mon.Valid || mon.SpeciesIndex == 0) throw new InvalidDataException($"{position} failed its native Pokémon checks.");
            members.Add(new(position, original, mon));
        }
        for (int i = 0; i < count; i++)
            Add($"Party {i + 1:D2}", Ram(build.Party + (uint)(i * profile.At("sizeof.Pokemon")), profile.At("sizeof.Pokemon")), true);
        int size = profile.At("sizeof.BoxPokemon");
        for (int box = 0; box < profile.At("TOTAL_BOXES_COUNT"); box++)
            for (int slot = 0; slot < profile.At("IN_BOX_COUNT"); slot++)
                Add($"Box {box + 1:D2} Slot {slot + 1:D2}", boxData.Slice(profile.At("PokemonStorage.boxes") + (box * 30 + slot) * size, size), false);
        return new(build, trainer, members);
    }

    // Shared container boundary for future game adapters; this returns mGBA's
    // raw GBA core state, not a Pokémon save and not a guessed game layout.
    public static byte[] ExtractCore(byte[] input)
    {
        if (input.Length > 8_000_000) throw new InvalidDataException("The state file is too large.");
        if (!input.AsSpan().StartsWith(PngSignature))
        {
            if (input.Length < StateSize) throw new InvalidDataException("This is not a complete mGBA GBA state.");
            return input.AsSpan(0, StateSize).ToArray();
        }
        int offset = 8; byte[]? result = null; bool ended = false;
        while (offset <= input.Length - 12)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(offset));
            if (length > input.Length - offset - 12) throw new InvalidDataException("Truncated PNG state chunk.");
            var type = input.AsSpan(offset + 4, 4);
            var payload = input.AsSpan(offset + 8, (int)length);
            if (Crc32(input.AsSpan(offset + 4, (int)length + 4)) != BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(offset + 8 + (int)length)))
                throw new InvalidDataException("PNG state chunk checksum failed.");
            if (type.SequenceEqual("gbAs"u8))
            {
                if (result != null) throw new InvalidDataException("Duplicate mGBA state chunks.");
                using var stream = new ZLibStream(new MemoryStream(payload.ToArray()), CompressionMode.Decompress);
                result = new byte[StateSize]; stream.ReadExactly(result);
                if (stream.ReadByte() != -1) throw new InvalidDataException("Unexpected mGBA state size.");
            }
            offset += 12 + (int)length;
            if (type.SequenceEqual("IEND"u8)) { ended = true; break; }
        }
        if (!ended || result is null) throw new InvalidDataException("PNG does not contain a complete mGBA state.");
        return result;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }
}
