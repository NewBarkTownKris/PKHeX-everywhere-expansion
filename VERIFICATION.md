# Prototype verification

- .NET WebAssembly publish: PASS.
- React/TypeScript production build: PASS.
- Web unit suite: 200 tests, 30 files, PASS.
- Completed-save native comparison: six party members and three occupied box slots, PASS.
- Direct and editor/facade no-op save exports: byte-identical, PASS.
- Nickname, IV, level and native shiny edits: PASS.
- Source-profile import and unreviewed import guard: PASS.
- Four source profiles: synthetic save round trips and last-box edits, PASS.
- Stock expansion 1.17.1: layout measured from untouched release bb1093a7c72d3961d9a37d8e02ad2abc2c866682; synthetic no-op and edit/reopen checks PASS.
- Soulgold BX21: 21 boxes and split storage/auxiliary checksums, PASS in synthetic fixtures.
- Collection ZIP: nine standard files, nine native records and metadata, PASS.
- Negative control: altered native Pokémon data fails checksum validation.
- Edited Rocket Hoenn save: cold-booted in the matching actual ROM; native nickname getter confirms the edit.
- Original completed save unchanged.
- JavaScript dependency audits: no reported vulnerabilities after compatible patches.

Real-save and in-game stock expansion/HnS/Soulgold validation remains outstanding.
Other expansion releases and save-changing configurations require their own profiles.
Persistent custom-profile storage and cross-profile imports are not complete.
GitHub Pages uses the project base path; nested-route redirect and asset loading are checked separately.

Location conversion checks: exact HG/SS Cherrygrove, New Bark, Violet, Route 29, Saffron, Routes 1/26, and Viridian Forest; matching Johto/Kanto regional labels in PK6; hidden abilities/long names/modern moves trigger newer files; forced shininess/nature/ability slot preserved in PK4; ORAS exact Hoenn locations retained with Gen6 and later moves. Completed-save exports verify M4GN1 PK4/PK6 and the other eight ORAS mappings.

Quetzal Alpha 9.1: the provided save’s five party records matched live mGBA
memory; 49 boxed records decoded as 31-byte records. All 54 resulting PK9 files
were reopened with PKHeX Core and checked for species, form, trainer IDs, IVs,
EVs, nicknames and shininess. The original save was unchanged. Independent
synthetic tests cover slot fallback, corrupt sectors, empty PC and PK9 conversion.
The prototype is read-only; other compressed box layouts are deliberately refused.
