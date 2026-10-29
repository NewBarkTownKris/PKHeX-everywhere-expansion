const sectorSize = 0x1000
const dataSize = 0xf80

type SectorSlot = { counter: number; sectors: Uint8Array[] }
type Party = { slot: number; nativeSpecies: number; level: number; pid: number; otId: number; moves: number[]; raw: string }
type BoxMon = { box: number; slot: number; nativeSpecies: number; nativeItem: number; experienceApproximate: number; nativeBall: number; shiny: boolean; moves: number[]; evs: number[]; ivs: number[]; femaleBit: number; nature: number; abilitySlot: number; raw: string; plausible: boolean }

const hex = (data: Uint8Array) => Array.from(data, n => n.toString(16).padStart(2, '0')).join('')
const view = (data: Uint8Array) => new DataView(data.buffer, data.byteOffset, data.byteLength)

function readSlot(data: Uint8Array, slot: number): SectorSlot | null {
  const sectors: Uint8Array[] = Array(16)
  let counter: number | null = null
  for (let i = 0; i < 16; i++) {
    const raw = data.subarray((slot * 16 + i) * sectorSize, (slot * 16 + i + 1) * sectorSize)
    const dv = view(raw)
    const id = dv.getUint16(0xff4, true)
    const saveIndex = dv.getUint32(0xffc, true)
    if (id >= 16 || sectors[id] || dv.getUint32(0xff8, true) !== 0x08012025 || (counter !== null && saveIndex !== counter)) return null
    let sum = 0
    for (let at = 0; at < 0xff4; at += 4) sum = (sum + dv.getUint32(at, true)) >>> 0
    if (((sum >>> 16) + (sum & 0xffff) & 0xffff) !== dv.getUint16(0xff6, true)) return null
    sectors[id] = raw
    counter = saveIndex
  }
  return { counter: counter!, sectors }
}

function join(chunks: Uint8Array[]) {
  const result = new Uint8Array(chunks.length * dataSize)
  chunks.forEach((chunk, i) => result.set(chunk.subarray(0, dataSize), i * dataSize))
  return result
}

function decodeRecord(raw: Uint8Array, index: number): BoxMon {
  let value = 0n
  for (let i = 20; i >= 0; i--) value = (value << 8n) | BigInt(raw[i])
  const bits = (at: number, count: number) => Number((value >> BigInt(at)) & ((1n << BigInt(count)) - 1n))
  const species = bits(0, 11), experience = bits(21, 17) * 10, nature = bits(161, 5), ability = bits(166, 2)
  const moves = [48, 58, 68, 78].map(at => bits(at, 10))
  return { box: Math.floor(index / 30) + 1, slot: index % 30 + 1, nativeSpecies: species,
    nativeItem: bits(11, 10), experienceApproximate: experience, nativeBall: bits(38, 6), shiny: bits(44, 1) !== 0,
    moves, evs: Array.from({ length: 6 }, (_, i) => bits(88 + i * 6, 6) * 4),
    ivs: Array.from({ length: 6 }, (_, i) => bits(124 + i * 5, 5)), femaleBit: bits(160, 1), nature, abilitySlot: ability,
    raw: hex(raw), plausible: species <= 1528 && experience <= 1059860 && nature <= 24 && ability <= 2 && moves.every(m => m <= 848) }
}

export function inspectQuetzal(bytes: Uint8Array) {
  if (bytes.length !== 0x20000) throw new Error('Quetzal Alpha 9 saves must be 128 KiB.')
  const slots = [readSlot(bytes, 0), readSlot(bytes, 1)].filter((x): x is SectorSlot => x !== null)
  if (!slots.length) throw new Error('Neither 16-sector save slot passed its signature and checksum checks.')
  const active = slots.length === 2 && ((slots[1].counter - slots[0].counter) >>> 0) > 0 && ((slots[1].counter - slots[0].counter) >>> 0) < 0x80000000 ? slots[1] : slots[0]
  const block = join(active.sectors.slice(1, 5)), partyCount = block[0x6a4]
  if (partyCount > 6) throw new Error('The save has an invalid party count.')
  const party: Party[] = Array.from({ length: partyCount }, (_, i) => {
    const raw = block.subarray(0x6a8 + i * 104, 0x6a8 + (i + 1) * 104), dv = view(raw)
    return { slot: i + 1, nativeSpecies: dv.getUint16(0x28, true), level: raw[0x58], pid: dv.getUint32(0, true), otId: dv.getUint32(4, true), moves: [0, 1, 2, 3].map(j => dv.getUint16(0x34 + j * 2, true)), raw: hex(raw) }
  })
  const storage = join(active.sectors.slice(5, 16))
  const formats = [[38, 37], [31, 45], [21, 67]].map(([stride, boxes]) => {
    const records: BoxMon[] = []
    let score = 0
    for (let i = 0; i < Math.min(boxes * 30, Math.floor((storage.length - 0x461) / stride)); i++) {
      const raw = storage.subarray(0x461 + i * stride, 0x461 + (i + 1) * stride)
      const item = decodeRecord(raw, i)
      if (!item.nativeSpecies) continue
      score += item.plausible ? 1 : -1
      records.push(item)
    }
    return { stride, boxes, score, records }
  }).sort((a, b) => b.score - a.score)
  const trainer = active.sectors[0]
  const base = { trainerNameRaw: hex(trainer.subarray(0, 7)), trainerGender: trainer[8], trainerId: view(trainer).getUint32(10, true), party }
  if (formats.every(f => f.records.length === 0)) return { ...base, pc: [] as BoxMon[], pcRecordBytes: 0 }
  if (formats[0].score <= 0 || formats[0].score === formats[1].score || formats[0].records.some(x => !x.plausible)) throw new Error('The box record format is empty, ambiguous or contains unsupported data.')
  if (formats[0].stride !== 31) throw new Error(`The ${formats[0].stride}-byte box format is recognized but not yet supported for PK9 export.`)
  return { ...base, pc: formats[0].records, pcRecordBytes: formats[0].stride }
}
