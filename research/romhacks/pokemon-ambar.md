# Pokémon Ambar Beta 1 — experimental real-species recovery

**Earlier status:** investigation only before the local real-species reader described below. This note covers only the supplied `- Pokemon - Ambar Beta 1.gba` revision; the similarly named ROM under `Pokemon Ambar/` has a different hash and must be checked independently.

A fresh mGBA playthrough selected the custom starter **LONEZ**, saved in-game, and continued after restarting. PKHeX recognizes a checksum-valid Ruby-format save and reports native species ID 280 as Torchic. The game identifies it as LONEZ instead. This is direct evidence that a checksum-valid standard reader can misidentify a custom Pokémon. The locally generated candidate name/ID allowlist correctly omits ID 280; no LONEZ record may be exported as Torchic. Two other selectable starters are also visibly custom.

A local name-table scan finds 359 exact official species-name and native-ID matches, but those are candidates only; it does not verify forms, abilities or changed behavior. Party/PC extraction, ball, shiny, move/item and location handling and portable exports need further validation. Keep original records and clearly identify approximations, and skip custom or ambiguous species.


## Local read-only recovery update (2026-10-29)

The exact ROM SHA-256 is `6f496134eb2f159ff25c6b6fc482d364043230819542d78792f18c7436b41386`, mGBA CRC `4EA40939`. A Gary Eevee was inserted into a copy of an Ambar game-created Ruby-layout save, displayed, re-saved and reopened in this exact ROM; the battery/mGBA party bytes, OT, moves and PK6 matched. This is a cross-game seeded specimen, not a native-caught Eevee. Synthetic tests cover occupied PC positions and confirm that native ID 280 (the custom LONEZ starter) is skipped while a qualifying real boxed record is kept. Other species, real PC contents, modified mechanics and forms are not independently validated.

Source move names at `0x1F8320` are Spanish; 294 matched official modern English or Spanish names by ID, and native ID 39 `LÁTIGO` was added after the in-game move summary described the Tail Whip effect. Its item names at `0x3C5564` yielded 175 exact-name candidates. Abbreviations and unmatched names remain native-only, not guessed. Portable PK6 assumes standard ability/PID shininess, accepts only the standard Poké Ball, and leaves location unknown; source bytes, trainer, PID and met values are preserved in metadata. `Historic/VbaParty.cs` handles selected Ruby battery and exact-build mGBA state export, with `vba-ambar-beta-1*.json` mappings. Browser chooser/download remains unverified. [Synthetic tests](../../src/PKHeX.Everywhere.ProfileChecks/VbaChecks.cs) exercise filtering. No private data or game files are included.

An additional check against the actual fresh game-created LONEZ battery save returned zero exported records and one skipped species, confirming that the fake starter is not mislabeled as Torchic. The separate synthetic test also confirms that a qualifying real boxed Pokémon is still retained when the fake party member is skipped.
