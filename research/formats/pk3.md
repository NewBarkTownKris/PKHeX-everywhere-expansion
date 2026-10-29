# PK3 Pokémon files

**Status:** standard Gen 3 record interoperability and limited offline recovery; not a universal representation for all hack-specific data. Standard stored records are 80 bytes, with a 100-byte party representation; encryption, shuffled blocks and checksum must be handled in the correct context. PKHeX's `PK3` codec is used by the project and offline probes. An extension or record length alone does not prove that hack-specific IDs mean the same thing as retail Ruby/Emerald.

For ordinary compatible records, species, ball, met location, OT and shiny determination follow the source game's compatible Gen 3 rules. A hack with expanded IDs or custom layouts needs a profile and deliberate conversion; Quetzal compact records are not standard PK3. See [battery saves](gba-battery-saves.md), [VBA research](vba-sgm.md) and [conversion](conversion.md). A valid checksum does not establish an authentic encounter, correct origin, or correct name mapping.
