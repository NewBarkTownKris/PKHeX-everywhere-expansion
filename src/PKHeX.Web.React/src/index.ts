import './startSentry'
import 'antd/dist/reset.css'
import './index.css'
import { start } from './main'

start({ autoLoad: false })

if (import.meta.env.DEV && new URLSearchParams(location.search).has('privateCheck')) {
  const { engine } = await import('./app')
  const response = await fetch('/__private_fixture.sav')
  if (response.ok) await engine.game.load(await response.blob(), 'Local test save.sav', 'rocket-hoenn')
}
