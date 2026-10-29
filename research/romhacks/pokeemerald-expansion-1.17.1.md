# pokeemerald-expansion 1.17.1

**Status:** source-profile save editing, experimental; mGBA-state adapter is local-only and tied to one test build. **Scope:** untouched release defaults, `EMERALD` / `MODERN=1`, release commit `bb1093a7c72d3961d9a37d8e02ad2abc2c866682`. A different configuration or release can change the save and Pokémon layout.

## Layout and mapping

The [profile](../../profiles/pokeemerald-expansion-1-17-1.json) records compiler-derived sizes, offsets, bitfields, symbol IDs, source hashes and checksum lengths. Box/party records are 80/100 bytes; storage has 14 × 30 slots. SaveBlock1 is 15,568 bytes; SaveBlock2 is 3,884; Pokémon storage is 34,144. The normal GBA signature is `0x08012025`; two slots have 14 rotated 4 KiB sectors each. See [battery saves](../formats/gba-battery-saves.md) and [source profile JSON](../formats/source-profile-json.md).

The profile supplies native species/move/item/ball constants and a shiny threshold of 8. Portable ball, shiny and location handling is the [shared conversion policy](../formats/conversion.md), including native shiny modifier and Hoenn OR/AS locations. No specific custom-ball fallback is asserted. Standard PKHeX personal data is currently a fallback where native stats were not captured.

## Evidence and references

The ARM-compiled release headers produced the profile; synthetic no-op, last-box edit and checksums were tested. A local ROM built from the release provided a real mGBA container, but its party/last-box contents were deliberately injected synthetic fixtures, not a real playthrough. Real-save and in-game edit verification remain needed. See [profile checks](../../src/PKHeX.Everywhere.ProfileChecks/Program.cs), [state checks](../../src/PKHeX.Everywhere.ProfileChecks/StateChecks.cs), [profile generator](../../tools/generate_profiles.py), and [upstream source](https://github.com/rh-hideout/pokeemerald-expansion).
