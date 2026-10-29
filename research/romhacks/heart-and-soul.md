# Pokémon Heart & Soul 2.0.6

**Status:** experimental source-profile save editor; supplied save and limited in-game edit verified. The local mGBA adapter covers only its exact locally built ROM. **Scope:** [profile](../../profiles/heart-and-soul-2.0.6.json), game define `POKEMON_HNS`.

## Format

Records are 80-byte box / 100-byte party; 14 × 30 slots. SaveBlock1 is 15,760 bytes, SaveBlock2 3,892, storage 34,144; party count and party offsets are 568 and 572. The profile records save version 5 and magic `0xE8F828BC`; the normal sector signature is `0x08012025`. Use its exact per-sector checksum lengths. Check version and layout rather than assuming compatibility from the HnS name. Relevant implementations: [NativeSave](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeSave.cs), [source profile](../../src/PKHeX.Everywhere.RomHacks/Expansion/SourceProfile.cs). The repository also retains an older [HeartAndSoulFormat](../../src/PKHeX.Everywhere.RomHacks/Expansion/HeartAndSoul/HeartAndSoulFormat.cs); the current web host registers the versioned source profile.

## Mapping and evidence

Portable Johto/Kanto outputs use HG, exact unique HG/SS locations for representable PK4, and region labels for newer formats. Balls and source shiny odds/modifier are profile-specific; do not apply the Quetzal ball fallback. The older HnS adapter's [ball table](../../src/PKHeX.Everywhere.RomHacks/Expansion/HeartAndSoul/HeartAndSoulPokemon.cs) notes GS Ball has no PKHeX counterpart. The source-profile conversion path is described in [shared conversion](../formats/conversion.md). Do not silently claim that GS Ball is preserved in a retail format.

Compiler-derived layouts and synthetic save/last-box edits pass; the locally built mGBA fixture includes deliberately injected synthetic records. The isolated HnS test build required fixes to duplicate map/heal definitions; the source project was not changed for that build. The supplied save and an isolated nickname edit have also been checked; broader edit operations and other versions need further testing.

## Randomizer save investigation

The supplied randomizer-labeled save passes the 2.0.6 source-profile sector and Pokémon checks. A no-op output was byte-identical, and a private nickname edit reopened with all other party and box records unchanged. The sample contains six party and 60 boxed Pokémon. An edited copy loaded in the local standard 2.0.6 source build in mGBA and displayed the nickname edit and recognizable real Pokémon. This is evidence for shared layout and that edit, not a test of every setting or operation.

## Source audit: built-in randomizer (2.0.6)

The earlier claim that a separate randomizer ROM is required was too broad. This source enables the randomizer in the normal build (`include/config/randomizer.h`); `src/randomizer.c` reads its switches from `SaveBlock3.challengeSettings`, and derives its default seed from the full player trainer ID. Encounter and learnset randomization select normal source species and move IDs: the resulting identity and learned moves are stored in the Pokémon record and use the same source profile. The save format is shared with the normal game. Different game versions, custom builds and externally patched randomizers can still change those assumptions.

SaveBlock3 is split across the 116-byte portions at offsets 3968–4083 of the 14 save sectors. The native editor retains these bytes on no-op and party/box edits; synthetic tests seed and verify every such portion in both save slots. The trainer ID is the randomizer seed in this configuration, so editing it can change randomized game behavior. The supplied save has also passed a byte-identical no-op and an isolated edit reopened in the local normal 2.0.6 game.

`GetAbilityBySpecies` and `RandomizeAbility` calculate an ability at runtime from the **player** trainer ID, source species, source ability slot and challenge setting. The record stores the slot, not the randomized ability. The editor now reproduces the source SFC32 stream, ten warm-up rounds, rejection sampling, slot fallback and duplicate collapse; source-built species ability slots and the ordered whitelist are included as data. Six source names with different PKHeX spellings are mapped explicitly. `tools/generate_hns_randomizer_data.py` regenerates these tables from a source-built ROM and ELF; the `randomizer.c` and whitelist source hashes matched the supplied 2.0.6 source. Save readers use the active sector’s settings and trainer ID; the exact supported mGBA build uses live RAM. A loose native `.expmon` does not contain the necessary save context.

An independent C reference compiled from the game functions verified 18 species/slot outputs, six special ability aliases and seed changes; automated tests also cover no-op/edit preservation, slot selection and modern portable export, plus live-state context. PK4 cannot reliably encode an arbitrary randomized ability, so randomized-ability exports use PK6 or PK9; metadata records the result. These may be illegal under retail rules. Randomized type and other battle rules cannot be represented by ordinary PK files. The separate non-random legendary-ability option remains outside this calculation. Different revisions, imported profiles or externally patched randomizers are not covered by this exact 2.0.6 implementation.
