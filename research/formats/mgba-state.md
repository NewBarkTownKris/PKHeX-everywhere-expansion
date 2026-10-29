# mGBA GBA save states and PNG states

**Status:** local implementation, pending publication; support is limited to explicitly identified GBA ROM builds. [MgbaState.cs](../../src/PKHeX.Everywhere.RomHacks/Expansion/MgbaState.cs), [StateExport.cs](../../src/PKHeX.Everywhere.RomHacks/Expansion/StateExport.cs), and [StateChecks.cs](../../src/PKHeX.Everywhere.ProfileChecks/StateChecks.cs) are authoritative. This reads the live party and storage from emulated RAM, not an assumed embedded battery save; no writing back is offered.

## Container and memory

The validated raw snapshot has little-endian version magic `0x01000007`, a ROM CRC at offset `0x08`, and a fixed GBA state portion of `0x61000` bytes; an extension can follow. IWRAM occupies state offset `0x19000` for `0x8000` bytes; EWRAM occupies offset `0x21000` for `0x40000`. PNG states carry a zlib-compressed `gbAs` chunk; validate PNG/chunk integrity and decompression before interpreting its bytes. Runtime addresses must be checked against their valid RAM region and size before dereferencing.

## Build-specific addresses

Addresses are GBA addresses, not offsets in the state file. The table is intentionally limited to known ROM CRCs; a CRC is a lookup aid, not cryptographic proof of identity. Before adding a build verify the complete ROM identity and derive addresses from its matching symbols or disassembly.

| Profile and CRC32 | Party | Party count | Storage pointer | Trainer pointer |
|---|---:|---:|---:|---:|
| Rocket Hoenn `AEBB572A`, `7307A1CC`, `16AF6C3A`, `26DC8E55`, `A22CE6D7` | `02031BC8` | `02031BB8` | `030051C8` | `030051C0` |
| Expansion 1.17.1 `A5B1DD32` | `02031B64` | `02031B54` | `030051C8` | `030051C0` |
| Heart & Soul 2.0.6 `42944F19` | `0203476C` | `020342AC` | `030041C4` | `030041BC` |
| SoulGold 1.2.1 `EB4F2403` | `02038E60` | `02038C19` | `030040C8` | `030040C0` |

The matching source profile supplies record sizes, field definitions, checks and ID tables. Standard-layout boxed records require the profile's Gen 3 decrypt path; SoulGold compact records have a separate codec. Opponent data is not a player party. Portable exports follow the matching profile's [conversion rules](conversion.md); unsupported nonzero IDs fail state conversion rather than silently using a different species, item, move or ball.

## Historic My Ass build

A separate, non-expansion reader handles the supplied Pokémon My Ass build CRC `1DED7E51` , Thunder Emblem Beta 1 build CRC `6A2B8EE2`, Shiny Gold Beta 5 X build CRC `61EF78C2`, Lightning Yellow Demo CRC `82F643BF`, and Gary Edition Beta 1 CRC `912918D8` with its own FireRed-layout party and PC addresses. Its game-generated mGBA party record was verified against the corresponding battery save; occupied-PC coverage is synthetic only. It does not use the expansion record codec. See [My Ass](../romhacks/pokemon-my-ass.md) [Thunder Emblem](../romhacks/thunder-emblem.md), [Shiny Gold](../romhacks/shiny-gold.md), [Lightning Yellow](../romhacks/lightning-yellow.md), and [Gary Edition](../romhacks/gary-edition.md).

## Verification and limitations

The checks include raw and PNG containers, malformed input, bounds and profile-specific synthetic live party/last-box records; Rocket Hoenn also has private real-state coverage. Synthetic fixtures do not establish correctness for all real saves. Never publish save states or extracted player data. Other mGBA versions, game builds, VBA `.sgm`, and unrelated games require their own validation and registration. Reference: [mGBA GBA serialization source](https://github.com/mgba-emu/mgba/blob/master/include/mgba/internal/gba/serialize.h).

## Game Boy PNG states: Shin Pokémon Red 1.24.5

The same PNG `gbAs` chunk name is used for Game Boy and GBA cores; decompressed size and version distinguish them. The exact Shin Red build (ROM CRC `878032D3`) is supported locally for live party only, using a 0x11800-byte GB serialized core and party offset 0x5563. The supplied `.ss1` loaded in mGBA; six live party identities matched its accompanying save. PNG integrity, unsupported builds and invalid party lists are checked. PC extraction is not implemented. A GB `gbAx` save-data chunk may carry an embedded SRAM snapshot, but it is not used to infer live PC or claimed as supported. See [Shin Red](../romhacks/shin-pokemon-red.md).

Cobalt Blue US CRC `6C8DD2E8` uses the same mGBA GB state core layout, with its live Gen 2 party at core offset `0x5E22`. One fresh Pikachu matched its in-game save; all held items are removed from portable PK2 and retained in the native record, with metadata. PC state extraction is not implemented. See [Cobalt Blue](../romhacks/cobalt-blue.md).

The supplied exact Dark Rising (`13548284`) and Liquid Crystal (`96D04AD1`) ROM builds also have experimental mGBA version 7 live party/PC export. Fresh Dratini and Cyndaquil party states matched game-created battery saves; PC coverage is synthetic. See their game-specific conversion notes.

The exact supplied Liquid Ocean Beta 1 (`B82CE606`) and Crystal Shards Beta 1 (`A3938FD1`) Emerald-derived ROMs have local mGBA version 7 party/PC exports. A fresh game-created starter state for each matched its reloaded battery save; PC coverage is synthetic. Their original locations remain unknown unless independently mapped.

The exact supplied Rijon Adventures 2009 Beta (`935ECB8B`), Genesis Fixed Beta 3 (`C9B11249`), Jasper Demo (`178CEAA6`), older Liquid Crystal Beta 1 (`AE6BE484`), Orange Generation (`9165F8A3`), Shining Opal Beta 2.13 (`FFA7854B`) and Twilight (`F7C447B1`) builds also have experimental version 7 party/PC export. Each game displayed, re-saved and reopened one Eevee seeded from a different hack; PC checks are synthetic and native-caught specimens remain unverified.

Omega Beta 1 (`532F54BC`) uses a separate Ruby-layout reader and was checked with a cross-game seeded Eevee that the game re-saved; real PC is unverified. See the game-specific note for its distinct fixed RAM addresses.

The supplied Pokémon Naranja (`84C3A0D6`) and Ruby Destiny: Rescue Rangers Beta 3.2 (`7D43AEE4`) revisions use the same checked Ruby-layout mGBA addresses and have the same cross-game seeded Eevee and synthetic-PC limits as Omega. The separately fingerprinted Naranja Beta 2 (`E4C181BF`) has its own profile and seeded-fixture check.

The supplied Ambar Beta 1 (`4EA40939`) and Ambar pabeta1 (`52FF506E`) builds also use Ruby-layout mGBA recovery, with seeded real Eevee and synthetic PC checks. The known custom starter is excluded; Spanish move-name matching and the observed Tail Whip label are described in their notes.

The exact supplied Topaz build (`1B921419`) also has Ruby-layout seeded Eevee recovery. Only three renamed moves observed in its in-game descriptions are mapped; other renamed moves remain native-only and PC checks are synthetic.

The supplied Ruby Destiny: Reign of Legends revision (`575C5ECD`) uses the same Ruby-layout reader. Its seeded Cyndaquil passed the game re-save and native-byte/PK6 comparisons; its standard Eevee ID did not match the source-name allowlist and is excluded. PC tests are synthetic.

The supplied Marble Beta 2.1 build (`2BED9BD8`) has seeded Cyndaquil party recovery; its known custom starter IDs are filtered, and PC checks are synthetic.

The supplied official LeafGreen USA ROM (`1C12FD39`) has seeded-party mGBA state recovery; real PC tests are synthetic. Its separate old user-supplied battery save failed standard sector validation and has not been recovered.

Crono Beta 1 (`CF4F1957`) uses Ruby-layout seeded Eevee recovery. A local title-screen graphics anomaly was observed, but Continue reopened a normal scene and the party matched the save; real PC remains unverified.
