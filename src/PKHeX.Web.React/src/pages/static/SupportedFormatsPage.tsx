import { Typography } from 'antd'

export default function SupportedFormatsPage() {
  return <Typography>
    <h1>Supported games and formats</h1>
    <p>
      This fork builds on <a href="https://github.com/arleypadua/PKHeX.Everywhere">PKHeX Everywhere</a> and{' '}
      <a href="https://github.com/kwsch/PKHeX">PKHeX</a>. Official-game support comes from the included PKHeX engine.
      ROM-hack support is experimental and depends on the game version, configuration and file layout.
    </p>
    <h2>ROM hacks and expansion bases</h2>
    <ul>
      <li><strong>Pokémon Unbound:</strong> inherited experimental save editor.</li>
      <li><strong>Pokémon Radical Red:</strong> inherited experimental save editor; format selection may be needed.</li>
      <li><strong>Emerald Imperium:</strong> inherited experimental save editor; its distinct 2.0 layout is refused.</li>
      <li><strong>Pokémon Emerald Legacy:</strong> inherited experimental save editor; format selection may be needed.</li>
      <li><strong>Pokémon Heart &amp; Soul 2.0.6:</strong> source-profile save editor; synthetic checks, real-save validation pending.</li>
      <li><strong>Pokémon SoulGold 1.2.1:</strong> source-profile save editor with 21-box BX21 storage; real-save validation pending.</li>
      <li><strong>pokeemerald-expansion 1.17.1:</strong> source-profile save editor for the untouched release defaults; synthetic checks.</li>
      <li><strong>Rocket Hoenn:</strong> source-profile save editor, checked against a completed save and game data.</li>
      <li><strong>Pokémon Quetzal English Alpha 9.1:</strong> read-only PK9 export for 31-byte PC records or an empty PC; other compact PC formats are refused.</li>
      <li><strong>Liquid Crystal (supplied 2012 revision) and Dark Rising (supplied 2013 revision):</strong> experimental read-only VBA 9 party and PC export, complete battery-save export and exact-build mGBA state export. Fresh mGBA party and synthetic PC checks passed. PK6 files use name-based mappings and documented approximations. The supplied Liquid Crystal .sa1 lacks its trainer sector, but its party and PC can be extracted read-only.</li>
      <li><strong>Rijon Adventures (older supplied revision):</strong> VBA party, selected battery and exact-build mGBA recovery; the battery fixture was reconstructed from a state, and a game-created save remains unverified.</li>
      <li><strong>Polished Crystal 3.2.3:</strong> experimental selected save and exact supplied normal-ROM mGBA live-party PK6/PK9 recovery. Only recognized real base species are exported; PID and IV conversions are approximations.</li>
      <li><strong>Pokémon Petalburg Dynasty 1.10.0:</strong> experimental selected battery and exact-build mGBA PK6/PK9 recovery for recognized real base species; custom species are skipped.</li>
      <li><strong>Pokémon Recharged Yellow 1.9.7:</strong> experimental selected save and exact-build mGBA party/PC PK6 recovery; six supplied records were checked against the live state.</li>
      <li><strong>Pokémon Lazarus 2.0:</strong> experimental selected save and exact-build mGBA party/PC recovery to PK6 or PK9. Two supplied saves were checked against live states; unsupported species and forms are skipped, and portable files have documented approximations.</li>
      <li><strong>Pokémon Citrine:</strong> experimental selected battery and exact-build mGBA live-party PK2 export, tested with one seeded Pikachu in a game-created save; PC is synthetic-only and held items are omitted.</li>
      <li><strong>Pokémon Heroes, Resolute 1.17 Fixed, Pokémon Isthmis, Snakewood Fixed (real species only), Old White 1.2, Liquid Ocean Alpha 1.2, Pokémon Shattered Dreams, Pokémon Crono Beta 1, LeafGreen USA (supplied build), Pokémon Marble Beta 2.1, Ruby Destiny: Reign of Legends, Pokémon Topaz, Pokémon Ambar (both supplied revisions), Pokémon Naranja (both supplied Ruby revisions), Ruby Destiny: Rescue Rangers Beta 3.2, Omega Beta 1, Rijon Adventures 2009 Beta, Genesis Fixed Beta 3, Jasper Demo, the older Liquid Crystal Beta 1, Orange Generation, Shining Opal Beta 2.13 and Twilight:</strong> experimental selected battery and exact-build mGBA PK6 export. Each was checked with one Pokémon seeded from another game and re-saved; PC tests are synthetic. Native-caught specimens, other revisions and VBA states remain unverified.</li>
      <li><strong>Rijon Adventures, Pokémon My Ass and Liquid Ocean Beta 1:</strong> limited party recovery and conditional PK6 export from selected VBA states. Pokémon My Ass, Thunder Emblem Beta 1, Shiny Gold Beta 5 X, Lightning Yellow Demo, Gary Edition Beta 1, Liquid Ocean Beta 1 and Crystal Shards Beta 1 also have experimental battery-save and exact-build mGBA export; PC coverage is synthetic only.</li>
    </ul>
    <p>
      Quetzal boxed Pokémon use the save trainer and a generated PID as labeled approximations. Suggested met locations and
      some other fields are approximated. Radiant Pokémon become normal shinies, and unmapped balls become Great Balls.
      The export metadata retains the original records and approximation flags.
    </p>
    <h2>Games excluded because of custom Pokémon</h2>
    <p>
      The supplied Pokémon Quartz revision is excluded because its author describes a custom Pokémon roster and its
      supplied state contains custom species. This project supports real Pokémon only; using the same numeric ID as a
      standard species would misidentify them. Other games that mix real and custom Pokémon can be considered for
      real-species export when their mappings are verified. Games still under investigation are not necessarily excluded
      for this reason.
    </p>
    <h2>Files</h2>
    <ul>
      <li><strong>Battery saves:</strong> compatible in-game saves, commonly .sav or .srm. A matching game layout is required.</li>
      <li><strong>Pokémon files:</strong> compatible individual files including .pk3, .pk4, .pk6 and .pk9; availability depends on the open game and conversion.</li>
      <li><strong>mGBA GBA states:</strong> read-only party and PC exports from version 7 raw .state and screenshot-style .ss0/PNG states, for specific verified ROM builds of Rocket Hoenn, expansion 1.17.1, Heart &amp; Soul 2.0.6, SoulGold 1.2.1 and the exact classic-hack revisions listed above, including Petalburg Dynasty 1.10.0, Lazarus 2.0 and Recharged Yellow 1.9.7. Evidence varies by revision; other builds are refused until checked.</li>
      <li><strong>Source-profile JSON:</strong> compatible expansion profiles can be imported for the browser session.</li>
      <li><strong>Collection ZIP:</strong> export containing portable Pokémon, native records and metadata where available.</li>
      <li><strong>Pokémon Cobalt Blue US:</strong> selected in-game save to PK2 export for the supplied revision. Held items are removed from portable files and retained in native records; PC coverage is synthetic only.</li>
      <li><strong>mGBA Game Boy states:</strong> read-only live-party export for the exact supplied Shin Pokémon Red (PK1), Cobalt Blue and Citrine (PK2), and Polished Crystal normal 3.2.3 (PK6/PK9) builds. PC state extraction and other Game Boy builds are not supported.</li>
      <li><strong>VisualBoyAdvance .sgm / .sg1:</strong> limited party recovery and conditional PK6 export for the supplied Rijon Adventures revisions, Pokémon My Ass and Liquid Ocean Beta 1. Choose the exact revision; shared headers cannot identify a hack. Unmatched species are skipped; PK6 uses name-based mappings, standard ability and shiny assumptions, and an unknown met location. The listed Liquid Crystal and Dark Rising VBA states also include PC extraction; general VBA support remains unavailable.</li>
      <li><strong>.sa1:</strong> file contents determine support; the supplied partial Liquid Crystal battery dump can be recovered read-only.</li>
      <li><strong>ROM images (.gba):</strong> not opened as save files.</li>
    </ul>
    <p>
      Support for other revisions and games remains under investigation. The{' '}
      <a href="https://github.com/NewBarkTownKris/PKHeX-everywhere-expansion/blob/expansion-web/SUPPORTED_FORMATS.md">repository compatibility list</a>{' '}
      provides additional version and validation details.
    </p>
  </Typography>
}
