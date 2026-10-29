using System.Buffers.Binary;
using System.Text.Json;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Historic;

// Experimental read-only foundation for early FireRed-based hacks. Game-specific
// species, moves, items, and record-layout changes must be verified before export.
public static class HistoricGen3
{
    public sealed record Record(string Position, byte[] NativeRecord, bool ChecksumValid, ushort NativeSpecies,
        string Nickname, string Trainer, byte NativeMetLocation);
    public sealed record Inspection(uint SaveIndex, int SourceSlot, string Trainer, IReadOnlyList<Record> Records);
    public sealed record Location(int NativeId, string SourceName, int? HgssId, string? HgssName);
    public sealed record SpeciesCandidate(int NativeId, string SourceName, int? National, string Confidence);

    public static Inspection InspectFireRedSave(byte[] input) => InspectSave(input, 0);
    public static Inspection InspectEmeraldSave(byte[] input) => InspectSave(input, 1);

    public static Inspection InspectRubySave(byte[] input) => InspectSave(input, 2);

    private static Inspection InspectSave(byte[] input, int layout)
    {
        // mGBA may append its 16-byte RTC footer to the 128 KiB flash image.
        // The footer is metadata, not part of the save slots or checksums.
        if (input.Length == 0x20010)
            input = input[..0x20000];
        if (input.Length is not (0x10000 or 0x20000))
            throw new InvalidOperationException("Expected a 64 KiB or 128 KiB GBA battery save (optionally with a 16-byte RTC footer).");
        if (input.All(x => x == 0xFF) || input.All(x => x == 0))
            throw new InvalidOperationException("The battery file is blank and contains no saved game.");

        var slots = new List<(int Slot, uint Index)>();
        for (int slot = 0; slot < (input.Length == 0x20000 ? 2 : 1); slot++)
            if (CheckSlot(input, slot, out uint index)) slots.Add((slot, index));
        if (slots.Count == 0) throw new InvalidOperationException("No complete, checksum-valid 14-sector save slot was found.");
        var active = slots[0];
        if (slots.Count == 2 && unchecked((int)(slots[1].Index - slots[0].Index)) > 0) active = slots[1];

        // PKHeX selects by sector footer rather than checksum. Supply only the
        // verified slot in a copy so an invalid newer slot cannot be selected.
        byte[] normalized = new byte[0x20000]; Array.Fill(normalized, (byte)0xFF);
        input.AsSpan(active.Slot * 0xE000, 0xE000).CopyTo(normalized);
        SAV3 save = layout switch { 1 => new SAV3E(normalized), 2 => new SAV3RS(normalized), _ => new SAV3FRLG(normalized) };
        if (save.PartyCount > 6) throw new InvalidOperationException("Invalid party count; this hack may use a different layout.");
        var records = new List<Record>();
        void Add(string position, ReadOnlySpan<byte> native)
        {
            // Do not discard invalid occupied entries; surface them for review.
            if ((native[0x13] & 2) == 0) return;
            var mon = new PK3(native.ToArray());
            records.Add(new(position, native.ToArray(), mon.ChecksumValid, mon.SpeciesInternal,
                mon.Nickname, mon.OriginalTrainerName, checked((byte)mon.MetLocation)));
        }
        for (int i = 0; i < save.PartyCount; i++)
            Add($"Party {i + 1:D2}", save.LargeBlock.PartyBuffer.Slice(i * 100, 100));
        for (int box = 0; box < 14; box++)
            for (int slot = 0; slot < 30; slot++)
                Add($"Box {box + 1:D2} Slot {slot + 1:D2}", save.Storage.Slice(save.GetBoxSlotOffset(box, slot), 80));
        return new(active.Index, active.Slot, save.OT, records);
    }

    private static bool CheckSlot(byte[] data, int slot, out uint index)
    {
        index = 0; int seen = 0;
        for (int i = 0; i < 14; i++)
        {
            var sector = data.AsSpan(slot * 0xE000 + i * 0x1000, 0x1000);
            int id = BinaryPrimitives.ReadUInt16LittleEndian(sector[0xFF4..]);
            uint counter = BinaryPrimitives.ReadUInt32LittleEndian(sector[0xFFC..]);
            if (id >= 14 || (seen & (1 << id)) != 0 || BinaryPrimitives.ReadUInt32LittleEndian(sector[0xFF8..]) != 0x08012025 ||
                Checksums.CheckSum32(sector[..0xF80]) != BinaryPrimitives.ReadUInt16LittleEndian(sector[0xFF6..]) ||
                (i != 0 && counter != index)) return false;
            seen |= 1 << id; index = counter;
        }
        return seen == 0x3FFF;
    }

    public static Location? LiquidCrystalLocation(int nativeId)
    {
        using var stream = typeof(HistoricGen3).Assembly.GetManifestResourceStream("historic.liquid-crystal-places.json")
            ?? throw new InvalidOperationException("Liquid Crystal location table unavailable.");
        using var json = JsonDocument.Parse(stream);
        foreach (var row in json.RootElement.GetProperty("locations").EnumerateArray())
            if (row.GetProperty("id").GetInt32() == nativeId)
                return new(nativeId, row.GetProperty("name").GetString()!,
                    row.GetProperty("exactHgssId").ValueKind == JsonValueKind.Number ? row.GetProperty("exactHgssId").GetInt32() : null,
                    row.GetProperty("exactHgssName").GetString());
        return null;
    }

    // Candidate mappings are based on names in the user's exact ROM revision.
    // They do not prove form, ability, stats, or species compatibility.
    public static SpeciesCandidate? SpeciesFromRom(string profile, int nativeId)
    {
        string resource = profile switch
        {
            "liquid-crystal" => "historic.liquid-crystal-tables.json",
            "dark-rising" => "historic.dark-rising-tables.json",
            _ => throw new ArgumentException("Unknown historical ROM profile.", nameof(profile)),
        };
        using var stream = typeof(HistoricGen3).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Historical species table unavailable.");
        using var json = JsonDocument.Parse(stream);
        foreach (var row in json.RootElement.GetProperty("species").EnumerateArray())
            if (row.GetProperty("id").GetInt32() == nativeId)
                return new(nativeId, row.GetProperty("name").GetString()!,
                    row.GetProperty("national").ValueKind == JsonValueKind.Number ? row.GetProperty("national").GetInt32() : null,
                    row.GetProperty("confidence").GetString()!);
        return null;
    }
}
