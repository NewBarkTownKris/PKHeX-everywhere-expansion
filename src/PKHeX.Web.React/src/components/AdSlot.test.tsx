import { describe, expect, it } from 'vitest'
import { AdSlot } from './AdSlot'

describe('self-hosted fork advertisements', () => {
  it('renders no third-party advertising widget', () => {
    expect(AdSlot({ slot: 'unused' })).toBeNull()
  })
})
