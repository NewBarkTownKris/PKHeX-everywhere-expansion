import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Alert, Button, List, Modal, Space, Typography } from 'antd'
import { engineHost } from '../../app'
import { downloadFile } from '../../host'
import { inspectQuetzal } from './quetzalSave'

type Converted = {
  position: string; nickname: string; file: string | null; bytes: string | null; error: string | null
  location: string | null; ballApproximate: boolean; trainerApproximate: boolean; trainerGenderApproximate: boolean
  pidApproximate: boolean; ppApproximate: boolean; formatHintRequired: boolean; radiantConvertedToShiny: boolean
}

export function QuetzalModal({ onClose }: { onClose: () => void }) {
  const fileInput = useRef<HTMLInputElement>(null)
  const [records, setRecords] = useState<Converted[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string>()

  async function convert(file: Blob) {
    setBusy(true); setError(undefined); setRecords(null)
    try {
      const inspection = inspectQuetzal(new Uint8Array(await file.arrayBuffer()))
      const exports = await engineHost.getAssemblyExports('PKHeX.Everywhere.Engine.Host.dll') as unknown as {
        PKHeX: { Everywhere: { Engine: { Host: { QuetzalExports: { Export: (json: string) => string } } } } }
      }
      setRecords(JSON.parse(exports.PKHeX.Everywhere.Engine.Host.QuetzalExports.Export(JSON.stringify(inspection))) as Converted[])
    } catch (e) { setError(e instanceof Error ? e.message : String(e)) }
    finally { setBusy(false) }
  }

  async function open(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (file) await convert(file)
  }

  useEffect(() => {
    if (import.meta.env.DEV && new URLSearchParams(location.search).has('quetzalCheck')) {
      void fetch('/__private_fixture.sav').then(response => response.blob()).then(convert)
    }
  }, [])

  return <Modal open title="Pokémon Quetzal Alpha 9.1 — PK9 export" onCancel={onClose} footer={<Button onClick={onClose}>Close</Button>} width={780}>
    <Space direction="vertical" style={{ width: '100%' }}>
      <Typography.Paragraph>
        Open a Quetzal Alpha 9.1 save to choose individual PK9 files. Box records do not store the original trainer or PID;
        exports use the save trainer and a generated PID. Locations are Scarlet/Violet encounter approximations when known.
        Experience is set for the Pokémon's level; box PP is estimated. Quetzal-only and unmapped balls become Great Balls.
        Radiant Pokémon are exported as regular shinies; their custom appearance cannot be retained in PK9.
        Pokémon with unsupported data are listed with an explanation.
      </Typography.Paragraph>
      <Button loading={busy} onClick={() => fileInput.current?.click()}>Choose Quetzal save</Button>
      <input ref={fileInput} type="file" accept=".sav,.srm" hidden onChange={open} />
      {error && <Alert type="error" title="Could not read this save" description={error} />}
      {records && <>
        <Typography.Text>{records.filter(r => r.file).length} of {records.length} Pokémon can be exported.</Typography.Text>
        <Button onClick={() => downloadFile(new TextEncoder().encode(JSON.stringify(records.map(({ bytes: _bytes, ...metadata }) => metadata), null, 2)), 'Quetzal export metadata.json')}>Download metadata</Button>
        <List size="small" style={{ maxHeight: 420, overflowY: 'auto' }} dataSource={records} renderItem={record => <List.Item actions={record.file && record.bytes ? [<Button key="download" onClick={() => downloadFile(Uint8Array.from(atob(record.bytes!), c => c.charCodeAt(0)), record.file!)}>Download PK9</Button>] : []}>
          <List.Item.Meta title={`${record.position} — ${record.nickname || 'Unsupported Pokémon'}`} description={record.error ?? [record.location ? `Suggested location: ${record.location}` : 'No matching Scarlet/Violet wild location', record.trainerApproximate ? 'Trainer and PID approximated' : '', record.ballApproximate ? 'Great Ball replaces original custom ball' : '', record.radiantConvertedToShiny ? 'Radiant converted to shiny' : '', record.formatHintRequired ? 'Choose PK9 format when importing' : ''].filter(Boolean).join(' · ')} />
        </List.Item>} />
      </>}
    </Space>
  </Modal>
}
