# Compatibility research

These implementation notes are for developers working on save readers, Pokémon conversion, or interoperability with PKHeX, PKForge, and other tools. They describe this project's observed formats and code, not a specification for all versions of a game. See [supported formats](../SUPPORTED_FORMATS.md) for the user-facing compatibility inventory and release status.

## ROM hacks and expansion bases

- [pokeemerald-expansion 1.17.1](romhacks/pokeemerald-expansion-1.17.1.md)
- [Rocket Hoenn](romhacks/rocket-hoenn.md)
- [Heart & Soul 2.0.6](romhacks/heart-and-soul.md)
- [SoulGold 1.2.1](romhacks/soulgold.md)
- [Pokémon Quetzal English Alpha 9.1](romhacks/quetzal.md)
- [Pokémon Unbound](romhacks/unbound.md)
- [Pokémon Radical Red](romhacks/radical-red.md)
- [Emerald Imperium](romhacks/emerald-imperium.md)
- [Pokémon Emerald Legacy](romhacks/emerald-legacy.md)
- [Pokémon Ruby Destiny: Reign of Legends](romhacks/ruby-destiny-reign.md) — seeded real Cyndaquil; real PC unverified
- [Pokémon Ruby Destiny: Rescue Rangers Beta 3.2](romhacks/ruby-destiny-rescue-beta-3-2.md) — seeded Ruby-layout Eevee; real PC unverified
- [Pokémon Naranja Beta 2](romhacks/naranja-beta-2.md) — seeded Ruby-layout Eevee; real PC unverified
- [Pokémon Naranja, supplied Ruby revision](romhacks/pokemon-naranja.md) — seeded Ruby-layout Eevee; real PC unverified
- [Pokémon Crono Beta 1](romhacks/crono-beta-1.md) — seeded Eevee; title-screen anomaly noted
- [Pokémon Topaz](romhacks/topaz.md) — seeded Eevee; only three observed renamed moves mapped
- [Pokémon Omega Beta 1](romhacks/omega-beta-1.md) — seeded Ruby-layout Eevee; real PC unverified
- [Pokémon Genesis Fixed Beta 3](romhacks/genesis-beta-3.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Pokémon Jasper Demo](romhacks/jasper-demo.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Pokémon Liquid Crystal Beta 1](romhacks/liquid-crystal-beta-1.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Pokémon Orange Generation](romhacks/orange-generation.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Pokémon Shining Opal Beta 2.13](romhacks/shining-opal-beta-2-13.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Pokémon Twilight](romhacks/twilight.md) — seeded Eevee battery/mGBA check; real PC unverified
- [Official LeafGreen USA baseline](romhacks/official-leafgreen-usa.md) — seeded mGBA check; supplied old save remains unrecovered
- [Pokémon Marble Beta 2.1](romhacks/marble-beta-2-1.md) — seeded real Cyndaquil; custom starters excluded
- [Pokémon Crystal Shards Beta 1](romhacks/crystal-shards.md) — local battery and mGBA export; real party and synthetic PC checks
- [Pokémon Gary Edition Beta 1](romhacks/gary-edition.md) — local save and mGBA export; PC synthetic only
- [Pokémon Lightning Yellow Demo](romhacks/lightning-yellow.md) — local save and mGBA party export; PC synthetic only
- [Pokémon Shiny Gold Beta 5 X](romhacks/shiny-gold.md) — local battery and mGBA party export; PC synthetic only
- [Pokémon Thunder Emblem Beta 1](romhacks/thunder-emblem.md) — local read-only battery and mGBA extraction; PC synthetic only
- Limited VBA party recovery and conditional PK6: [Rijon Adventures revisions](romhacks/rijon-adventures.md), [Pokémon My Ass](romhacks/pokemon-my-ass.md), [Liquid Ocean Beta 1](romhacks/liquid-ocean.md) (also local battery and exact-build mGBA export)
- Intentionally excluded custom-species roster: [Pokémon Quartz](romhacks/pokemon-quartz.md)
- [Pokémon Cobalt Blue US](romhacks/cobalt-blue.md) — local selected-save PK2; held items removed in portable output
- [Shin Pokémon Red 1.24.5](romhacks/shin-pokemon-red.md) — mGBA live-party PK1 export locally; PC state export and editing unverified
- [Liquid Crystal](romhacks/liquid-crystal.md) and [Dark Rising](romhacks/dark-rising.md) — experimental selected VBA 9 party/PC and complete battery export, with explicit approximations
- [Pokémon Ambar Beta 1](romhacks/pokemon-ambar.md) and [Ambar pabeta1](romhacks/ambar-pabeta1.md) — seeded real Eevee recovery, custom species excluded

## File and container formats

- [GBA battery saves (.sav / .srm)](formats/gba-battery-saves.md)
- [mGBA GBA save states (.state / .ss0 / PNG)](formats/mgba-state.md) — local, pending publication
- [VisualBoyAdvance .sgm / .sg1](formats/vba-sgm.md) — limited raw party recovery, local next build
- [Legacy .sa1](formats/sa1.md) — unknown container; not a dedicated web format
- [PK1 and PK2](formats/pk1-pk2.md), [PK3](formats/pk3.md), [PK4](formats/pk4.md), [PK6](formats/pk6.md), [PK9](formats/pk9.md)
- [Native .expmon](formats/expmon.md), [source profile JSON](formats/source-profile-json.md), [collection ZIP](formats/collection-zip.md)
- [Shared conversion and preservation rules](formats/conversion.md)

## Reading the notes

Each note identifies what is actually implemented, the source files that carry the behavior, what was tested, and what remains uncertain. The numeric values and version/build restrictions are evidence for the cited revisions only. A checksum-valid record is not proof that the source hack uses standard species, move, ball or location IDs. Do not infer retail encounter legality from a mapped file.

No private save, emulator state, player metadata, extracted Pokémon, ROM image or proprietary game assets belong in this directory. Use synthetic examples and public code links; obtain permission and apply the project's distribution rules before adding any new reference material. Preserve original attribution and licenses.

## Maintenance rule

When support for a new ROM hack, revision, container, Pokémon format, or conversion is added, create or update its own research note in the same change. Record: source/version and fingerprint; offsets, sizes and checksums; native-to-standard species, move, item, form, ball, shiny and location handling when applicable; losses and unmapped cases; code and tests; evidence and negative controls; known limits; and attribution/license for any borrowed work. Mark speculative findings as such, and update the index, [compatibility inventory](../SUPPORTED_FORMATS.md), site list and release notes alongside it. Use [the template](template.md).

- [Pokémon Shattered Dreams](romhacks/shattered-dreams.md) — seeded Cyndaquil; party verified, PC synthetic only

- [Liquid Ocean Alpha 1.2](romhacks/liquid-ocean-alpha-1-2.md) — distinct FireRed revision; seeded Eevee, PC synthetic only

- [Old White 1.2](romhacks/old-white-1-2.md) — seeded Eevee; PC synthetic only

- [Snakewood Fixed](romhacks/snakewood-fixed.md) — seeded Eevee; nonmatching/custom species skipped; PC synthetic only

- [Pokémon Isthmis](romhacks/pokemon-isthmis.md) — game-created save seeded with Eevee; PC synthetic only

- [Pokémon Resolute 1.17 Fixed](romhacks/resolute-1-17.md) — seeded Eevee in game-created save; unmatched species skipped; PC synthetic only

- [Pokémon Heroes](romhacks/pokemon-heroes.md) — Spanish source labels; seeded Eevee, PC synthetic only

- [Pokémon Citrine](romhacks/citrine.md) — game-created save with seeded Pikachu, mGBA live party; PC synthetic only

- [Emerald3DS supplied save](romhacks/emerald3ds.md) — standard Emerald battery save; second supplied file blank

- [Petalburg Dynasty 1.10.0 investigation](romhacks/petalburg-dynasty.md) — structurally similar expansion save; custom species excluded, mappings unverified

- [Pokémon Lazarus 2.0 investigation](romhacks/lazarus.md) — both saves load; expansion-specific layout not yet verified

- [Polished Crystal 3.2.3 investigation](romhacks/polished-crystal.md) — changed species, DV and save structures; export unverified

- [Recharged Yellow investigation](romhacks/recharged-yellow.md) — supplied save loads; revised record mappings still unverified
