using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Historic;

// Version-specific read-only live party. Never infer the ROM from its common title.
public static class MgbaShinRed
{
    private const int Size = 0x11800, Party = 0x5563, PartyLength = 0x194;
    private const uint RomCrc = 0x878032D3, Magic = 0x00400003;
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10];

    public static string? Inspect(byte[] input)
    {
        if (input.Length > 8_000_000) throw new InvalidDataException("State file is too large.");
        byte[]? core = null;
        if (input.AsSpan().StartsWith(Png))
        {
            int offset = 8; bool ended = false;
            while (offset <= input.Length - 12)
            {
                uint len = BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(offset));
                if (len > input.Length - offset - 12) throw new InvalidDataException("Truncated PNG state chunk.");
                var type = input.AsSpan(offset + 4, 4);
                var payload = input.AsSpan(offset + 8, (int)len);
                if (Crc32(input.AsSpan(offset + 4, (int)len + 4)) != BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(offset + 8 + (int)len)))
                    throw new InvalidDataException("PNG chunk checksum failed.");
                if (type.SequenceEqual("gbAs"u8))
                {
                    using var z = new ZLibStream(new MemoryStream(payload.ToArray()), CompressionMode.Decompress);
                    // Read one byte beyond the supported GB core size; GBA chunks are handled elsewhere.
                    byte[] temp = new byte[Size + 1]; int total = 0, n;
                    while (total < temp.Length && (n = z.Read(temp, total, temp.Length - total)) != 0) total += n;
                    if (total == Size)
                    {
                        if (core is not null) throw new InvalidDataException("Duplicate Game Boy state chunks.");
                        core = temp.AsSpan(0, Size).ToArray();
                    }
                }
                offset += (int)len + 12;
                if (type.SequenceEqual("IEND"u8)) { ended = true; break; }
            }
            if (!ended) throw new InvalidDataException("PNG state is incomplete.");
        }
        else if (input.Length >= Size && BinaryPrimitives.ReadUInt32LittleEndian(input) == Magic)
            core = input.AsSpan(0, Size).ToArray();
        if (core is null) return null;
        if (BinaryPrimitives.ReadUInt32LittleEndian(core) != Magic) throw new InvalidDataException("Unsupported mGBA Game Boy state version.");
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(core.AsSpan(4));
        if (crc == 0xF98367E4) return PolishedRecovery.InspectLiveCore(core);
        if (crc == 0xA90AC0A3) return GbClassic.InspectLiveParty(core.AsSpan(0x5E22, 428), "citrine");
        if (crc == 0x6C8DD2E8) return GbClassic.InspectLiveParty(core.AsSpan(0x5E22, 428));
        if (crc != RomCrc) throw new InvalidDataException($"This Game Boy ROM build is not recognized (CRC {crc:X8}).");
        var party = core.AsSpan(Party, PartyLength); int count = party[0];
        if (count > 6 || party[1 + count] != 0xFF) throw new InvalidDataException("Invalid live party list.");
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject(); w.WriteString("sourceType", "mGBA Game Boy party");
            w.WriteString("profile", "Shin Pokémon Red 1.24.5"); w.WriteString("trainer", "Live party"); w.WriteString("romCrc", crc.ToString("X8"));
            w.WriteString("warning", "Party only; PC Pokémon are not included. PK1 preserves original Generation 1 data; shiny is inferred from DVs and no capture location or ball was stored.");
            w.WriteNumber("skippedUnmapped", 0); w.WriteStartArray("pokemon");
            for (int i = 0; i < count; i++)
            {
                var mon = new PK1(party.Slice(8 + i * 44, 44), party.Slice(272 + i * 11, 11), party.Slice(338 + i * 11, 11));
                if (!mon.Valid || mon.Species is < 1 or > 151 || mon.SpeciesInternal != party[1 + i])
                    throw new InvalidDataException($"Invalid or unsupported Pokémon in party slot {i + 1}.");
                byte[] data = new byte[mon.SIZE_PARTY]; mon.WriteDecryptedDataParty(data);
                string stem = $"Party {i + 1:D2} - {string.Concat(mon.Nickname.Where(char.IsLetterOrDigit))}";
                w.WriteStartObject(); w.WriteString("position", $"Party {i + 1:D2}"); w.WriteString("nickname", mon.Nickname);
                w.WriteString("nativeFile", stem + ".pk1"); w.WriteBase64String("nativeBytes", data);
                w.WriteString("file", stem + ".pk1"); w.WriteBase64String("bytes", data);
                w.WriteString("originalTrainer", mon.OriginalTrainerName); w.WriteNumber("trainerId", mon.TID16);
                w.WriteNumber("species", mon.Species); w.WriteBoolean("shiny", mon.IsShiny);
                w.WriteNull("portableLocation"); w.WriteNull("error"); w.WriteEndObject();
            }
            w.WriteEndArray(); w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = uint.MaxValue;
        foreach (byte b in data) { crc ^= b; for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1)); }
        return ~crc;
    }
}
