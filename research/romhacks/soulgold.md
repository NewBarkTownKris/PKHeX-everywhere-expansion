# Pokémon SoulGold 1.2.1

**Status:** experimental versioned BX21 source profile; mGBA state reading is local-only for a particular test build. Real-save and gameplay edit validation pending. Profile: [soulgold-1.2.1.json](../../profiles/soulgold-1.2.1.json).

## Storage layout

The compact boxed/party records are **68/96 bytes**, with 21 boxes × 30, unlike 80/100-byte expansion layouts. Storage size is 43,456 bytes; current box is at offset 16 and records begin at 404. The first 35,712 storage bytes come from ordinary sectors 5–13; then 3,968 bytes come from physical sector `30 + slot`, 1,944 from the SaveBlock1 storage-extension tail, and 1,832 from physical sector `28 + slot` after its 12-byte auxiliary header. These offsets are for the cited profile only. Main slot signature is `0x21325842` (`BX2!` bytes in little-endian); the storage/auxiliary header magic is `0x31325842` (`BX21` bytes). Distinguish them.

[NativeSave](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeSave.cs) validates the main sectors, storage header, complements, storage checksum, auxiliary header/payload checksum, extra-sector IDs/signatures/counters and their checksums. On export it recomputes these. Compact `NativePokemon` records are not ordinary encrypted PK3 records; its validity checks reserved bits rather than a standard per-mon checksum. The state reader therefore cannot claim the same per-Pokémon integrity guarantee for this codec.

## Conversion and verification

`SHINY_ODDS` is **512**, with a source shiny modifier; preserve effective shininess when converting to standard PID-derived formats and log PID changes. Balls and other IDs use the profile mapping. Johto/Kanto source locations become SS origin with exact HG/SS PK4 locations where possible, or regional labels in PK6/PK9. See [shared conversion](../formats/conversion.md). Synthetic checks cover box 21 slot 30, extra sectors, edit/reload and a local mGBA state with synthetic live records. No real-save proof yet; older SoulGold storage revisions must not be assumed BX21.

## Supplied save verification

A supplied 1.2.1 save passed all sector and compact storage checks; a no-op write was byte-identical. An isolated nickname edit was written and reopened with all other party and all 21 boxes unchanged, then loaded in the locally built matching SoulGold ROM in mGBA; the party screen displayed the edit. The source file was not modified. Its six occupied boxed records did not exercise boxes after box 14; split-storage behavior there still has synthetic coverage only. This validates a narrow edit and does not establish arbitrary edit or legality correctness.
