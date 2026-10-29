import { useRef, useState, type ChangeEvent } from 'react'
import { App, Button, Modal, Select, Typography } from 'antd'
import { useQuery } from '@pkhex-everywhere/react'
import { registerNativeProfile } from '../../nativeProfiles'
import { SaveFileDrop } from './SaveFileDrop'
import { useLoadSave } from './useLoadSave'

export function LoadRomHackModal({ onClose }: { onClose: () => void }) {
  const formats = useQuery('game.formats')
  const [custom, setCustom] = useState<{id:string;name:string}[]>([])
  const { notification } = App.useApp()
  const profileInput = useRef<HTMLInputElement>(null)
  const { openFile } = useLoadSave()
  const [formatId, setFormatId] = useState<string>()
  const fileInput = useRef<HTMLInputElement>(null)

  const openSave = async (file: File) => {
    if (formatId && (await openFile(file, formatId))) onClose()
  }

  const loadFile = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (file) await openSave(file)
  }

  return (
    <Modal
      open
      title="Load ROM Hack"
      okText="Open save file..."
      onOk={() => fileInput.current?.click()}
      onCancel={onClose}
      okButtonProps={{ disabled: !formatId }}
      modalRender={(content) => <SaveFileDrop onOpen={openSave} disabled={!formatId}>{content}</SaveFileDrop>}
    >
      <Select
        showSearch={{ optionFilterProp: 'label' }}
        placeholder="Search ROM hacks"
        aria-label="ROM hack"
        options={[...formats, ...custom].map((format) => ({ value: format.id, label: format.name }))}
        value={formatId}
        onChange={setFormatId}
        style={{ width: '100%' }}
      />
      <Typography.Paragraph style={{ marginTop: 16 }}>
        Expansion saves need a profile for the matching game and version. A profile supplies the save layout and the game's location names.
      </Typography.Paragraph>
      <Button onClick={() => profileInput.current?.click()}>Import source profile</Button>
      <input ref={profileInput} type="file" accept=".json" hidden onChange={async event => {
        const file = event.target.files?.[0]
        event.target.value = ''
        if (!file) return
        try {
          if (file.size > 8_000_000) throw new Error('Profile exceeds 8 MB')
          const json = JSON.parse(await file.text()) as { name:string }
          const id = await registerNativeProfile(file)
          setCustom(current => [...current, { id, name:json.name }])
          setFormatId(id)
        } catch (error) { notification.error({ title:'Could not load profile', description:String(error) }) }
      }} />
      <input ref={fileInput} type="file" hidden onChange={loadFile} />
    </Modal>
  )
}
