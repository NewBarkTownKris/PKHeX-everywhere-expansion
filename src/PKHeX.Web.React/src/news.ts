import type { Settings } from './settings'

export interface NewsEntry {
  date: string
  headline?: string
  items: string[]
}

export const romHacksNewsDate = '2026-10-20'

export const news: NewsEntry[] = [
  {
    date: '2026-10-29',
    headline: 'Local preview: classic-hack and save-state Pokémon recovery.',
    items: [
      'The older Rijon Adventures revision adds experimental selected battery and exact-ROM mGBA state recovery, checked using a reconstructed save and seeded Pokémon; a game-created battery save remains unverified.',
      'Polished Crystal 3.2.3 adds experimental real-base-species save and exact normal-ROM mGBA party recovery; PID and IV conversion are approximations.',
      'Pokémon Petalburg Dynasty 1.10.0 adds real-species-only save and mGBA recovery; its supplied custom species is skipped.',
      'Pokémon Recharged Yellow 1.9.7 adds experimental save and mGBA party/PC PK6 recovery; the supplied save was checked against the game.',
      'Pokémon Lazarus 2.0 adds experimental read-only save and mGBA party/PC recovery for recognized real species; two supplied saves were checked against live states.',
      'Pokémon Citrine supports experimental save and mGBA live-party PK2 export using a seeded test Pokémon; PC tests are synthetic.',
      'Pokémon Heroes, Resolute 1.17 Fixed, Pokémon Isthmis, Snakewood Fixed (real species only), Old White 1.2, Liquid Ocean Alpha 1.2, Pokémon Shattered Dreams, Pokémon Crono Beta 1, LeafGreen USA (supplied build), Pokémon Marble Beta 2.1, Ruby Destiny: Reign of Legends, Pokémon Topaz, Pokémon Ambar (both supplied revisions), Pokémon Naranja (both supplied Ruby revisions), Ruby Destiny: Rescue Rangers Beta 3.2, Omega Beta 1, Rijon Adventures 2009 Beta, Genesis Fixed Beta 3, Jasper Demo, older Liquid Crystal Beta 1, Orange Generation, Shining Opal Beta 2.13 and Twilight have experimental selected battery and exact-build mGBA recovery, checked with one cross-game seeded Pokémon each; PC checks are synthetic.',
      'The supplied Liquid Crystal and Dark Rising revisions support experimental VBA 9 party/PC, complete battery-save and exact-build mGBA state Pokémon export to approximate PK6; original records and metadata are retained.',
      'Recover party records and conditional PK6 exports from selected Rijon Adventures, Pokémon My Ass and Liquid Ocean Beta 1 VBA states. Custom and unrecognized species are skipped.',
      'Selected My Ass, Thunder Emblem Beta 1, Shiny Gold Beta 5 X, Lightning Yellow Demo, Gary Edition Beta 1, Liquid Ocean Beta 1 and Crystal Shards Beta 1 battery saves and mGBA states support experimental PK6 recovery; party fixtures are game-created and PC checks are synthetic. Select the exact game.',
      'Shin Pokémon Red 1.24.5 mGBA states support live-party PK1 export; PC state extraction is not yet supported.',
      'Cobalt Blue US in-game saves and exact-build mGBA live-party states support experimental PK2 export. Portable files omit held items; source records and metadata retain them.',
      'PK6 conversions use name-based mappings and standard ability and shiny assumptions. Most met locations remain unknown; Shiny Gold New Bark Town and Lightning Yellow/Gary Edition Pallet Town are approximated as the Johto and Kanto regions. Native records and metadata are retained.',
    ],
  },
  {
    date: '2026-10-28',
    headline: 'PKHeX Expansion Web preview: versioned expansion save profiles.',
    items: [
      'Added a read-only Pokémon Quetzal Alpha 9.1 screen for individual PK9 downloads and original-record metadata.',
      'Pokémon exports map Johto/Kanto origins to HG/SS and Hoenn origins to OR/AS. Exact locations are kept where supported, with regional labels for newer formats.',
      'Collection archives include modern regional alternatives for exact-location PK4 exports.',
      'Added separate profiles for stock pokeemerald-expansion 1.17.1, Rocket Hoenn, Heart & Soul 2.0.6 and Soulgold 1.2.1.',
      'Rocket Hoenn has completed-save and game verification. The other profiles have source-derived layouts and synthetic checks; real-save testing is still needed.',
      'Use the profile that matches your game build. Other expansion versions or save-changing configurations need their own profile.',
      'Export a collection archive with portable Pokémon files, native records and original metadata.',
    ],
  },
  {
    date: romHacksNewsDate,
    headline:
      'PKHeX.Web now opens Heart & Soul and Emerald Legacy saves, along with Unbound, Radical Red and Emerald Imperium.',
    items: [
      'Added support for Pokémon Heart & Soul and Emerald Legacy saves.',
      'The Box page can open a second save and move its Pokémon into the loaded one, including between two saves of the same ROM hack.',
      'You can open a save by dropping it on the Load page.',
      'Unbound and Radical Red bags have a Key Items pocket.',
      "Emerald Imperium Pokémon can switch to another of their species' abilities.",
      "Radical Red's Hakamo-o and its Basculin, Calyrex and Terapagos forms show correctly.",
      "A Gen 3 save past its game's limits asks whether it's from a ROM hack instead of failing to load.",
      'ROM hack saves from more emulators load.',
      'Colosseum, XD and Box saves on a GameCube memory card open.',
      'The Home page hides the trainer gender for games that have none.',
      'Exporting a save writes back only the Pokémon you changed.',
      'Pokémon icons that stopped loading show again.',
      'Stadium saves no longer leave gaps in boxes after export.',
    ],
  },
  {
    date: '2026-10-04',
    headline: 'PKHeX.Web now opens Unbound, Radical Red and Emerald Imperium saves.',
    items: ['Added support for Pokémon Unbound, Radical Red and Emerald Imperium saves.'],
  },
  {
    date: '2026-10-01',
    items: [
      'Added an Alpha toggle to the Pokémon editor.',
      'Added a Show all moves checkbox to the Moves tab.',
      'The Stats tab shows Hidden Power type and power.',
      "Imported Pokémon files convert to the loaded save's format.",
      'Action menus open on click instead of hover.',
      'Encounter Search preselects the game version on your first visit.',
      'Trainer name is editable on the Home page.',
      'Pre-Gen7 Pokémon show their 16-bit OT TID.',
      'Gen1/2 Pokémon of the same species no longer open the wrong one.',
      'Plug-in updates no longer crash on a 404.',
      'Search works in dropdowns again.',
      'Empty Legends: Arceus saves without item pockets load.',
      'The analytics page shows an alert when results fail to load.',
      'Money edits are disabled on Legends: Z-A saves without a money block.',
      'TM item icons load, and unnamed Z-A items are skipped.',
      'The Moves tab no longer crashes on Battle Revolution saves or unknown moves.',
      'The Pokémon editor no longer crashes on species missing from the loaded game.',
      'Demo loads a bundled Emerald save.',
      'Encounter Search sends you to the load page when no save is loaded.',
      "Let's Go party edits no longer crash on saves with empty slots.",
      'Malformed save files show an error instead of crashing.',
      'Fixed a unique id error in box rows.',
    ],
  },
  {
    date: '2026-03-20',
    items: [
      'Upgraded PKHeX.Core and ALM plugins to latest version',
      'Upgraded from .NET 9.x to 10.x',
      'Small bug fixes',
    ],
  },
  {
    date: '2025-05-01',
    items: [
      "Fixed exporting of Let's Go Eevee save files.",
      'Update to privacy policy and cookie consent.',
      'Upgraded PKHeX.Core and ALM plugins to latest version',
    ],
  },
  {
    date: '2025-03-25',
    items: [
      'Introduced plugin error pages showing a list of errors whenever executing a plugin',
      'Upgraded PKHeX.Core and ALM plugins to latest version',
    ],
  },
  {
    date: '2025-02-14',
    items: [
      'Minor bug fixes',
      'News banner',
    ],
  },
  {
    date: '2024-11-30',
    items: [
      'Migrated to .NET 9',
      'Upgraded PKHeX.Core and ALM plugins to latest',
      'Added support for Battle Points.',
    ],
  },
  {
    date: '2024-11-29',
    items: [
      'Introduced the PKHeX.Web Cloud (alpha) with up to 6 Pokémon stored in the cloud.',
    ],
  },
]

export const latestNewsDate = news[0].date

export function newsHeadline(entry: NewsEntry) {
  return entry.headline ?? 'PKHeX.Web just got updated'
}

export function checkUnseenNews(settings: Settings, today: Date) {
  const since = settings.readLastDateNewsSeen()
  const lastSeen = since ?? formatDate(daysBefore(today, 30))
  if (since === null) settings.writeLastDateNewsSeen(lastSeen)
  return { since, unseen: lastSeen < latestNewsDate }
}

export function markNewsSeen(settings: Settings) {
  settings.writeLastDateNewsSeen(latestNewsDate)
}

function daysBefore(date: Date, days: number) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() - days)
}

export function formatDate(date: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
