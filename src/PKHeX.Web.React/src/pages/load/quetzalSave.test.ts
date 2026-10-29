import { describe, expect, it } from 'vitest'
import { inspectQuetzal } from './quetzalSave'

function fixture(hasBoxes = true) {
  const data = new Uint8Array(0x20000)
  const dv = new DataView(data.buffer)
  for (let slot = 0; slot < 2; slot++) {
    for (let id = 0; id < 16; id++) {
      const at = (slot * 16 + id) * 0x1000
      dv.setUint16(at + 0xff4, id, true)
      dv.setUint32(at + 0xff8, 0x08012025, true)
      dv.setUint32(at + 0xffc, 10 - slot, true)
      if (id === 1) {
        data[at + 0x6a4] = 1
        dv.setUint16(at + 0x6a8 + 0x28, slot ? 25 : 4, true)
        data[at + 0x6a8 + 0x58] = 5
      }
      if (id === 5 && hasBoxes) {
        dv.setUint16(at + 0x461, 4, true)
        dv.setUint16(at + 0x461 + 31, 25, true)
      }
      let sum = 0
      for (let i = 0; i < 0xff4; i += 4) sum = (sum + dv.getUint32(at + i, true)) >>> 0
      dv.setUint16(at + 0xff6, ((sum >>> 16) + (sum & 0xffff)) & 0xffff, true)
    }
  }
  return data
}

describe('Quetzal save inspection', () => {
  it('reads the newest valid slot and identifies compact box records', () => {
    const data = fixture()
    const result = inspectQuetzal(data)
    expect(result.party[0].nativeSpecies).toBe(4)
    expect(result.pcRecordBytes).toBe(31)
    expect(result.pc.map(p => p.nativeSpecies)).toEqual([4, 25])
    data[0x1000] ^= 1
    expect(inspectQuetzal(data).party[0].nativeSpecies).toBe(25)
    data[17 * 0x1000] ^= 1
    expect(() => inspectQuetzal(data)).toThrow('Neither 16-sector save slot')
  })

  it('accepts a save with an empty PC and rejects the wrong file size', () => {
    expect(inspectQuetzal(fixture(false)).pc).toEqual([])
    expect(() => inspectQuetzal(new Uint8Array(10))).toThrow('128 KiB')
  })
})
