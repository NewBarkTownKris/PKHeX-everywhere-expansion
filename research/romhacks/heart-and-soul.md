# Pokémon Heart & Soul 2.0.6

**Status:** experimental source-profile save editor; real-save/gameplay validation pending. The local mGBA adapter covers only its exact locally built ROM. **Scope:** [profile](../../profiles/heart-and-soul-2.0.6.json), game define `POKEMON_HNS`.

## Format

Records are 80-byte box / 100-byte party; 14 × 30 slots. SaveBlock1 is 15,760 bytes, SaveBlock2 3,892, storage 34,144; party count and party offsets are 568 and 572. The profile records save version 5 and magic `0xE8F828BC`; the normal sector signature is `0x08012025`. Use its exact per-sector checksum lengths. Check version and layout rather than assuming compatibility from the HnS name. Relevant implementations: [NativeSave](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeSave.cs), [source profile](../../src/PKHeX.Everywhere.RomHacks/Expansion/SourceProfile.cs). The repository also retains an older [HeartAndSoulFormat](../../src/PKHeX.Everywhere.RomHacks/Expansion/HeartAndSoul/HeartAndSoulFormat.cs); the current web host registers the versioned source profile.

## Mapping and evidence

Portable Johto/Kanto outputs use HG, exact unique HG/SS locations for representable PK4, and region labels for newer formats. Balls and source shiny odds/modifier are profile-specific; do not apply the Quetzal ball fallback. The older HnS adapter's [ball table](../../src/PKHeX.Everywhere.RomHacks/Expansion/HeartAndSoul/HeartAndSoulPokemon.cs) notes GS Ball has no PKHeX counterpart. The source-profile conversion path is described in [shared conversion](../formats/conversion.md). Do not silently claim that GS Ball is preserved in a retail format.

Compiler-derived layouts and synthetic save/last-box edits pass; the locally built mGBA fixture includes deliberately injected synthetic records. The isolated HnS test build required fixes to duplicate map/heal definitions; the source project was not changed for that build. Verify actual save versions, source-only data, party/PC and boot/re-save against a real user save before claiming broad compatibility.

## Randomizer save investigation

The supplied randomizer-labeled save passes the 2.0.6 source-profile sector and Pokémon checks. A no-op output was byte-identical, and a private nickname edit reopened with all other party and box records unchanged. The supplied sample contains six party and 60 boxed Pokémon. The exact randomizer ROM and its configuration were not identified, so randomized species/move/ability semantics and an edited in-game reload remain unverified. Do not assume the stock profile accurately describes altered mechanics; retain the original and treat this as structural evidence only.

The user confirmed the exact randomizer ROM/configuration is unavailable. An isolated nickname-edited copy of the supplied randomizer save was also loaded into the local **standard** 2.0.6 source build in mGBA; the party screen displayed the edit and recognizable real Pokémon, consistent with the stock source-profile interpretation. This corroborates the save layout and a narrow edit only; it cannot validate any game-data changes made by the missing randomizer build. Use the existing 2.0.6 profile as an explicitly conditional interpretation. Preserve originals and treat altered species, moves, abilities or forms as unverified.
