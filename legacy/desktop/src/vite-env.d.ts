/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_SUPABASE_URL: string
  readonly VITE_SUPABASE_ANON_KEY: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

interface Window {
  electron?: {
    ipcRenderer: {
      invoke: (channel: string, ...args: any[]) => Promise<any>
    }
  }
  api?: {
    exportMasterCsv: (options: any) => Promise<any>
    exportProductsCsv: (options: any) => Promise<any>
  }
}
