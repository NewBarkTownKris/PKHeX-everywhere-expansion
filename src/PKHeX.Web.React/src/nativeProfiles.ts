import { engineHost } from './app'

type ProfileExports = {
  Register(json: string): string
  HasNativeSave(): boolean
  ExportCollection(): string
}

async function exports(): Promise<ProfileExports> {
  const assembly = await engineHost.getAssemblyExports('PKHeX.Everywhere.Engine.Host.dll') as unknown as {
    PKHeX: { Everywhere: { Engine: { Host: { ExpansionProfileExports: ProfileExports } } } }
  }
  return assembly.PKHeX.Everywhere.Engine.Host.ExpansionProfileExports
}

export async function registerNativeProfile(file: File): Promise<string> {
  return (await exports()).Register(await file.text())
}
export async function hasNativeSave(): Promise<boolean> { return (await exports()).HasNativeSave() }
export async function exportNativeCollection(): Promise<Uint8Array<ArrayBuffer>> {
  const encoded = (await exports()).ExportCollection()
  return Uint8Array.from(atob(encoded), character => character.charCodeAt(0))
}
