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

A significant remaining limitation is randomized abilities. `GetAbilityBySpecies` and `RandomizeAbility` calculate the in-game ability at runtime from the player seed, species, ability slot and challenge setting; the individual Pokémon stores the ability slot, not the randomized ability. Current `NativePokemon.Ability` and portable export use the normal source personal table, not this calculation. Thus the ability displayed/exported may differ from the active game ability when ability randomization (or the legendary ability option) is enabled. Randomized type and other battle rules also cannot be represented by ordinary PK files. Preserve the native save and do not claim complete faithful randomizer export.
