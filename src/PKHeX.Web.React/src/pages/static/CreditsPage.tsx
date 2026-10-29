import { Typography } from 'antd'

export default function CreditsPage() {
  return (
    <Typography>
      <h1>Credits</h1>

      <h2>PKHeX Everywhere</h2>
      <p>
        PKHeX Everywhere is the original application, interface, engine, and source of the inherited ROM-hack adapters.
        It was created by <a href="https://github.com/arleypadua/PKHeX.Everywhere">Arley Padua and contributors</a>.
        This project is a modified fork that adds versioned expansion profiles and export tools.
      </p>
      <h2>Game projects and conversion research</h2>
      <p>
        Expansion profiles are derived from matching <a href="https://github.com/rh-hideout/pokeemerald-expansion">pokeemerald-expansion</a>,
        Heart &amp; Soul and Soulgold source versions. <a href="https://github.com/sofianeelhor/PKForge">PKForge</a> was a
        design reference for adapters and Pokémon conversion.
      </p>
      <h2>PokeDaisy</h2>
      <p>
        Quetzal save and ROM table research builds on <a href="https://github.com/lidor30/pokedaisy">PokeDaisy</a>,
        a GPL-3.0 project. Its Quetzal tools and documented RAM and table research also informed the Lazarus, Recharged Yellow and Polished Crystal readers.
      </p>
      <h2>Polished Crystal</h2>
      <p>Polished Crystal recovery was researched using the public <a href="https://github.com/Rangi42/polishedcrystal">Polished Crystal</a> 3.2.3 source and release symbols, by its project contributors.</p>
      <h2>PKHeX.Core</h2>
      <p>
        This app is built on the foundation of{' '}
        <a href="https://github.com/kwsch/PKHeX" target="_blank">PKHeX.Core</a>. Special thanks to{' '}
        <a href="https://github.com/kwsch" target="_blank">Kurt</a> for maintaining it.
      </p>

      <h2>PokeAPI/sprites</h2>
      <p>
        All image resources are taken from{' '}
        <a href="https://github.com/PokeAPI/sprites" target="_blank">PokeAPI/sprites</a>.
      </p>

      <h2>PKHeX-Plugins</h2>
      <p>
        Special thanks to <a href="https://github.com/santacrab2" target="_blank">santacrab2</a> for maintaining the{' '}
        <a href="https://github.com/santacrab2/PKHeX-Plugins" target="_blank">PKHeX-Plugins</a> fork, which includes the
        Auto-Legality mode.
      </p>

      <h2>OpenHome</h2>
      <p>
        The Pokémon Unbound and Radical Red species, item and move data comes from{' '}
        <a href="https://github.com/andrewbenington/OpenHome" target="_blank">OpenHome</a> by{' '}
        <a href="https://github.com/andrewbenington" target="_blank">Andrew Benington</a>. Thanks for making it available.
      </p>

      <h2>PKMN RBYGSC font</h2>
      <p>
        The pixel font is{' '}
        <a href="https://fontstruct.com/fontstructions/show/875033" target="_blank">PKMN RBYGSC</a> by David Fens.
      </p>
      <h2>License</h2>
      <p>
        The combined project is distributed under GPL-3.0-or-later. The repository includes the full license,
        retained upstream notices, and the original PKHeX Everywhere project description.
      </p>
    </Typography>
  )
}
