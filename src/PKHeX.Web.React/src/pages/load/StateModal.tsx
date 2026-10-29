import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Alert, Button, List, Modal, Space, Typography } from 'antd'
import { engineHost } from '../../app'
import { downloadFile } from '../../host'

type StateMon = {
  position: string; nickname: string; nativeFile: string; nativeBytes: string
  file: string | null; bytes: string | null; portableLocation: string | null; error: string | null
}
type StateResult = { profile: string; trainer: string; romCrc: string; pokemon: StateMon[]; warning?: string; skippedUnmapped?: number }
type Exports = { Inspect: (state: string) => string; Archive: (json: string) => string }

async function getExports(): Promise<Exports> {
  const assembly = await engineHost.getAssemblyExports('PKHeX.Everywhere.Engine.Host.dll') as unknown as {
    PKHeX: { Everywhere: { Engine: { Host: { StateExports: Exports } } } }
  }
  return assembly.PKHeX.Everywhere.Engine.Host.StateExports
}

function encode(data: Uint8Array) {
  let text = ''
  for (let offset = 0; offset < data.length; offset += 0x8000)
    text += String.fromCharCode(...data.subarray(offset, offset + 0x8000))
  return btoa(text)
}
const decode = (data: string) => Uint8Array.from(atob(data), c => c.charCodeAt(0))

export function StateModal({ onClose }: { onClose: () => void }) {
  const input = useRef<HTMLInputElement>(null)
  const [result, setResult] = useState<StateResult>()
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)
  async function convert(file: Blob) {
    setBusy(true); setError(undefined); setResult(undefined)
    try {
      if (file.size > 8_000_000) throw new Error('The state file is too large.')
      const data = new Uint8Array(await file.arrayBuffer())
      setResult(JSON.parse((await getExports()).Inspect(encode(data))) as StateResult)
    } catch (e) { setError(e instanceof Error ? e.message : String(e)) }
    finally { setBusy(false) }
  }
  async function open(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]; event.target.value = ''
    if (file) await convert(file)
  }
  useEffect(() => {
    if (import.meta.env.DEV && new URLSearchParams(location.search).has('stateCheck')) {
      void fetch('/__private_fixture.sav').then(response => response.blob()).then(convert)
    }
  }, [])
  async function all() {
    if (!result) return
    try { downloadFile(decode((await getExports()).Archive(JSON.stringify(result))), 'Pokémon from mGBA state.zip') }
    catch (e) { setError(e instanceof Error ? e.message : String(e)) }
  }
  return <Modal open title="Export Pokémon from an mGBA state" onCancel={onClose} footer={<Button onClick={onClose}>Close</Button>} width={800}>
    <Space direction="vertical" style={{ width: '100%' }}>
      <Typography.Paragraph>
        This reads the live Pokémon from recognized mGBA states. Most supported GBA builds include party and PC; Shin Pokémon Red and Cobalt Blue provide party only. Each supported game must match a known ROM build.
        The state is read locally and cannot be edited here. Portable exports may use mapped origins and locations.
      </Typography.Paragraph>
      <Button loading={busy} onClick={() => input.current?.click()}>Choose mGBA state</Button>
      <input ref={input} type="file" hidden onChange={open} />
      {error && <Alert type="error" title="Could not read this state" description={error} />}
      {result && <>
        <Typography.Text>{result.trainer} · {result.profile} · ROM {result.romCrc} · {result.pokemon.length} Pokémon</Typography.Text>
        {result.warning && <Alert type="warning" title="Export assumptions" description={`${result.warning} Skipped unmatched species: ${result.skippedUnmapped ?? 0}.`} />}
        <Button onClick={() => void all()} disabled={result.pokemon.length === 0}>Download all Pokémon (ZIP)</Button>
        <List size="small" style={{ maxHeight: 420, overflowY: 'auto' }} dataSource={result.pokemon} renderItem={mon =>
          <List.Item actions={[
            ...(mon.file && mon.bytes ? [<Button key="portable" onClick={() => downloadFile(decode(mon.bytes!), mon.file!)}>Download Pokémon</Button>] : []),
            <Button key="native" onClick={() => downloadFile(decode(mon.nativeBytes), mon.nativeFile)}>Native record</Button>,
          ]}>
            <List.Item.Meta title={`${mon.position} — ${mon.nickname}`} description={mon.error ?? (mon.portableLocation ? `Mapped location: ${mon.portableLocation}` : 'Location unavailable')} />
          </List.Item>} />
      </>}
    </Space>
  </Modal>
}
