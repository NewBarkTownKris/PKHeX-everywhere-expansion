# Rocket Hoenn

**Status:** source-profile save editing and portable export; matching completed save and native getters tested. mGBA-state support for listed exact build CRCs is local-only. **Scope:** this project's expansion 1.17.1-based ROM and [profile](../../profiles/rocket-hoenn.json), not every Emerald hack.

## Layout and portability

Box/party records are 80/100 bytes, 14 boxes. SaveBlock1 is 15,652 bytes; SaveBlock2 3,884; storage 34,144. Native party count/party offsets are 564/568 in SaveBlock1. The profile includes source names, bitfields and, for this game, numeric native personal data. Sector selection, encryption and checksums use [NativeSave](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeSave.cs) and [NativePokemon](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativePokemon.cs).

Johto/Kanto places map to SS and unique HG/SS names where PK4 can represent the data; otherwise PK6/PK9 use a regional label. Hoenn maps to OR and corresponding OR/AS locations or a regional fallback. This deliberately changes the **portable** origin, keeping the source record and native location/origin in metadata. Ball enumeration and the forced-shiny modifier follow the [shared conversion policy](../formats/conversion.md). PK4 constraints may require a new PID; no blanket Great Ball fallback is used here.

## Evidence and limits

A completed save's six party and three box entries were compared with game-native getters, including names, trainer IDs, IVs/EVs, moves and shininess; no-op save round trips were byte-identical, and an edited copy was booted. mGBA live state identities were checked against the save and current Rocket checkpoints include both raw and PNG states; see [mGBA state research](../formats/mgba-state.md). Do not include the user's save, states or personal metadata in public fixtures. See [verification](../../VERIFICATION.md) and [profile checks](../../src/PKHeX.Everywhere.ProfileChecks/Program.cs).
