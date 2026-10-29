# Emerald3DS supplied save

The supplied `emerald3ds.sav` is a 131,072-byte standard Generation 3 Emerald save. PKHeX Core recognizes it as `SAV3E`, verifies its checksums, finds one party Pokémon, and produces a byte-identical no-op write. A private copy loaded into the supplied unmodified Emerald ROM in mGBA. It is handled through upstream official Emerald save support; no new ROM-hack adapter or unsupported conversion is necessary. Individual Pokémon have not been compared against the running game, and an edited in-game reload was not performed.

The separately supplied `1986 - Pokemon - Emerald Version (UE).sav` is entirely `FF` and contains no recoverable save in this form. The original files were not modified. Emulator save-state support for the supplied official ROM was not established by this check.
