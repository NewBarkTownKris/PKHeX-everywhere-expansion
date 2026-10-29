# Source profile JSON

**Status:** project-specific, versioned metadata for the native expansion reader. It is not a standard save file or proof that a game matches a particular profile. [SourceProfile.cs](../../src/PKHeX.Everywhere.RomHacks/Expansion/SourceProfile.cs) parses and validates profiles; [generate_profiles.py](../../tools/generate_profiles.py) derives values and symbol tables from matching source and build configuration. The generated profiles are under [profiles](../../profiles/).

A useful profile identifies the source revision and settings, save-block and storage sizes, section lengths/signature, party/storage offsets, record size and bitfield layout, constants and relevant symbol names. Source compiler output matters for C bitfields: do not infer field ordering from declaration text alone. Tables for species, moves, abilities, items, balls, met locations and regions are needed for portable conversion; shiny odds and modifiers are also profile-dependent. Custom ROM changes or compiler settings can invalidate an otherwise plausible profile.

Test with synthetic fixtures, native getter comparisons and representative real saves for each revision, then register format detection deliberately. Loading profile JSON alone does not make arbitrary hacks compatible. Preserve attribution and do not embed saves or ROM assets.
