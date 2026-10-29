import { useRef, useState, type ChangeEvent } from 'react'
import { Alert, Button, List, Modal, Select, Space, Typography } from 'antd'
import { engineHost } from '../../app'
import { downloadFile } from '../../host'

type Mon = { position: string; nickname: string; nativeSpecies: number; storedLevel: number; nativeFile: string; nativeBytes: string; file: string | null; bytes: string | null; portableLocation?: string | null; error: string | null }
type Result = { profile: string; vbaVersion?: number; sourceType?: string; warning: string; skippedUnmapped: number; pokemon: Mon[] }
type Exports = { InspectVba: (state: string, profile: string) => string; InspectHistoricBattery: (state: string, profile: string) => string; ArchiveVba: (json: string) => string }
async function getExports(): Promise<Exports> {
  const assembly = await engineHost.getAssemblyExports('PKHeX.Everywhere.Engine.Host.dll') as unknown as { PKHeX: { Everywhere: { Engine: { Host: { StateExports: Exports } } } } }
  return assembly.PKHeX.Everywhere.Engine.Host.StateExports
}
const decode = (data: string) => Uint8Array.from(atob(data), c => c.charCodeAt(0))
function encode(data: Uint8Array) {
  let s = ''
  for (let offset = 0; offset < data.length; offset += 0x8000) s += String.fromCharCode(...data.subarray(offset, offset + 0x8000))
  return btoa(s)
}
export function VbaPartyModal({ onClose, kind = 'state' }: { onClose: () => void; kind?: 'state' | 'save' }) {
  const input = useRef<HTMLInputElement>(null)
  const [profile, setProfile] = useState<string>()
  const [result, setResult] = useState<Result>()
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)
  async function open(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]; event.target.value = ''
    if (!file || !profile) return
    setResult(undefined); setError(undefined); setBusy(true)
    try {
      if (file.size > 8_000_000) throw new Error('The state file is too large.')
      const host = await getExports()
      const data = encode(new Uint8Array(await file.arrayBuffer()))
      setResult(JSON.parse(kind === 'save' ? host.InspectHistoricBattery(data, profile) : host.InspectVba(data, profile)) as Result)
    } catch (e) { setError(e instanceof Error ? e.message : String(e)) } finally { setBusy(false) }
  }
  async function all() {
    if (!result) return
    try { downloadFile(decode((await getExports()).ArchiveVba(JSON.stringify(result))), kind === 'save' ? 'Classic hack Pokémon.zip' : 'VBA Pokémon.zip') }
    catch (e) { setError(e instanceof Error ? e.message : String(e)) }
  }
  return <Modal open title={kind === 'save' ? 'Export Pokémon from a classic hack save' : 'Recover Pokémon from a VBA state'} onCancel={onClose} footer={<Button onClick={onClose}>Close</Button>} width={800}>
    <Space direction="vertical" style={{ width: '100%' }}>
      <Typography.Paragraph>Select the exact game and revision before opening {kind === 'save' ? 'an in-game save. Party and PC are read; PC validation varies by game.' : 'a VBA 8/9 .sgm or .sg1 state. Liquid Crystal and Dark Rising include the PC; other supported states include the live party.'} Only records whose species have a supported official mapping are included; unmatched custom or uncertain species are skipped. Where mappings are available, download a Pokémon file and its original record. Depending on the game and Pokémon, portable files are PK2, PK6 or PK9. The source game and unknown encounter location are explained in the metadata.</Typography.Paragraph>
      <Select style={{ width: '100%' }} placeholder="Choose the source game and revision" value={profile} onChange={value => { setProfile(value); setResult(undefined); setError(undefined) }} options={[
        ...(kind === 'state' ? [{ value: 'rijon-2009', label: 'Rijon Adventures 2009 Beta (VBA 9)' },
        { value: 'rijon', label: 'Rijon Adventures, supplied 16 MiB revision (VBA 8)' }] : []),
        ...(kind === 'save' ? [{ value: 'crono-beta-1', label: 'Pokémon Crono Beta 1, supplied revision' }, { value: 'leaf-green-usa', label: 'Pokémon LeafGreen USA, supplied revision' }, { value: 'liquid-ocean-alpha-1-2', label: 'Pokémon Liquid Ocean Alpha 1.2 (distinct revision)' }, { value: 'old-white-1-2', label: 'Pokémon Old White 1.2, supplied revision' }, { value: 'rijon', label: 'Rijon Adventures, older supplied revision (experimental)' }, { value: 'polished-crystal-3-2-3', label: 'Polished Crystal 3.2.3 (real base species)' }, { value: 'petalburg-1-10', label: 'Pokémon Petalburg Dynasty 1.10.0 (real species only)' }, { value: 'recharged-yellow', label: 'Pokémon Recharged Yellow 1.9.7' }, { value: 'lazarus-2', label: 'Pokémon Lazarus 2.0 (real species only)' }, { value: 'pokemon-heroes', label: 'Pokémon Heroes, supplied revision' }, { value: 'snakewood-fixed', label: 'Pokémon Snakewood Fixed (real species only)' }, { value: 'resolute-1-17', label: 'Pokémon Resolute 1.17 Fixed (real species only)' }, { value: 'pokemon-isthmis', label: 'Pokémon Isthmis, supplied revision' }, { value: 'shattered-dreams', label: 'Pokémon Shattered Dreams, supplied revision' }, { value: 'marble-beta-2-1', label: 'Pokémon Marble Beta 2.1 (real species only)' }, { value: 'ruby-destiny-reign', label: 'Pokémon Ruby Destiny: Reign of Legends, supplied revision' }, { value: 'topaz', label: 'Pokémon Topaz, supplied revision' }, { value: 'ambar-beta-1', label: 'Pokémon Ambar Beta 1, supplied revision' }, { value: 'ambar-pabeta1', label: 'Pokémon Ambar pabeta1, separate revision' }, { value: 'naranja-beta-2', label: 'Pokémon Naranja Beta 2 (distinct revision)' }, { value: 'pokemon-naranja', label: 'Pokémon Naranja, supplied Ruby-based revision' }, { value: 'ruby-destiny-rescue-beta-3-2', label: 'Pokémon Ruby Destiny: Rescue Rangers Beta 3.2' }, { value: 'omega-beta-1', label: 'Pokémon Omega Beta 1, supplied revision' }, { value: 'rijon-2009', label: 'Rijon Adventures 2009 Beta, supplied revision' }, { value: 'genesis-beta-3', label: 'Pokémon Genesis Fixed Beta 3, supplied revision' }, { value: 'jasper-demo', label: 'Pokémon Jasper Demo, supplied revision' }, { value: 'liquid-crystal-beta-1', label: 'Pokémon Liquid Crystal Beta 1 (older, distinct revision)' }, { value: 'orange-generation', label: 'Pokémon Orange Generation, supplied revision' }, { value: 'shining-opal-beta-2-13', label: 'Pokémon Shining Opal Beta 2.13' }, { value: 'twilight', label: 'Pokémon Twilight, supplied revision' }, { value: 'crystal-shards-beta-1', label: 'Pokémon Crystal Shards Beta 1, supplied revision' }, { value: 'thunder-emblem', label: 'Pokémon Thunder Emblem Beta 1, supplied revision' }, { value: 'shiny-gold', label: 'Pokémon Shiny Gold Beta 5 X, supplied revision' }, { value: 'lightning-yellow', label: 'Pokémon Lightning Yellow Demo, supplied revision' }, { value: 'gary-edition', label: 'Pokémon Gary Edition Beta 1, supplied revision' }, { value: 'citrine', label: 'Pokémon Citrine, supplied revision (Game Boy Color)' }, { value: 'cobalt-blue', label: 'Pokémon Cobalt Blue US, supplied revision (Game Boy Color)' }] : []),
        { value: 'liquid-crystal', label: 'Pokémon Liquid Crystal, supplied 2012 revision (VBA 9 or complete save)' },
        { value: 'dark-rising', label: 'Pokémon Dark Rising, supplied 2013 revision (VBA 9 or complete save)' },
        { value: 'my-ass', label: `Pokémon My Ass, supplied Portuguese revision${kind === 'state' ? ' (VBA 9)' : ''}` },
        { value: 'liquid-ocean-beta-1', label: `Liquid Ocean Beta 1, supplied Emerald-based revision${kind === 'state' ? ' (VBA 8)' : ''}` },
      ]} />
      <Alert type="warning" title="Game identification depends on your selection" description="Several hacks share the same ROM header. Choosing the wrong revision can produce misleading data even when checksums pass." />
      <Button disabled={!profile} loading={busy} onClick={() => input.current?.click()}>{kind === 'save' ? 'Choose save file' : 'Choose VBA state'}</Button>
      <input ref={input} type="file" hidden onChange={open} />
      {error && <Alert type="error" title="Could not read this state" description={error} />}
      {result && <>
        <Typography.Text>{result.profile} · {kind === 'state' ? `VBA ${result.vbaVersion ?? 9}` : 'in-game save'} · {result.pokemon.length} Pokémon</Typography.Text>
        <Alert type="info" title="Portable files include approximations" description={`${result.warning} Skipped unsupported records: ${result.skippedUnmapped}.`} />
        <Button onClick={() => void all()} disabled={result.pokemon.length === 0}>Download Pokémon and metadata (ZIP)</Button>
        <List size="small" dataSource={result.pokemon} renderItem={mon => <List.Item actions={[
          ...(mon.file && mon.bytes ? [<Button key="pokemon" onClick={() => downloadFile(decode(mon.bytes!), mon.file!)}>Download Pokémon</Button>] : []),
          <Button key="native" onClick={() => downloadFile(decode(mon.nativeBytes), mon.nativeFile)}>Download raw record</Button>,
        ]}><List.Item.Meta title={`${mon.position} — ${mon.nickname}`} description={mon.error ?? `Native species ID ${mon.nativeSpecies}${mon.storedLevel == null ? "" : ` · level ${mon.storedLevel}`} · met location ${mon.portableLocation || "unknown"}`} /></List.Item>} />
      </>}
    </Space>
  </Modal>
}
