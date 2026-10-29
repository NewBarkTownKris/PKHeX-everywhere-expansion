# Legacy .sa1 files

**Earlier status:** the first supplied samples did not identify the container. The supplied `.sa1` samples were 64 KiB entirely filled with `0xFF`, so they contained no recoverable Pokémon and do not establish whether populated `.sa1` files are flash saves, snapshots, backups, or something else. Renaming an empty file does not recover a save.

Other revisions require a populated file with known game and emulator version, an original unmodified backup, and preferably a corresponding in-game battery save or state for comparison. First identify its bytes and checksums; only then choose or implement the appropriate parser. Ball, shiny and location rules belong to the actual game profile, not the `.sa1` extension. See [battery saves](gba-battery-saves.md) and [VBA states](vba-sgm.md) for the separate currently researched formats.

The supplied Dark Rising `.sa1` is a 64 KiB raw flash snapshot with one complete 14-sector save scattered among 16 physical sectors. The local classic reader validates IDs, a single counter, and sector checksums before recovering it. The supplied Liquid Crystal `.sa1` lacks its sector 0 and is supported for read-only party/PC recovery, explicitly flagged as incomplete; its 60 native records match a separately supplied VBA state. No reconstructed complete save is exported. The extension itself is not a format identifier.
