import { app, BrowserWindow, ipcMain, dialog } from 'electron'
import path from 'path'
import fs from 'fs'
import os from 'os'
import { exec } from 'child_process'
import { createClient } from '@supabase/supabase-js'
import { weatherService } from './weather-service'
import { exportMasterCSV } from './export/exportMasterCsv'
import { exportProductsCSV } from './export/exportProductsCsv'
import { generateFilename } from './export/csvUtils'

// Supabase client for exports (lazy initialization)
// Electron main process'te VITE_ prefix'li değişkenler otomatik yüklenmez
// Bu yüzden hardcode ediyoruz (güvenli çünkü anon key zaten public)
const SUPABASE_URL = 'https://kiucehfasorbtftnferi.supabase.co'
const SUPABASE_ANON_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtpdWNlaGZhc29yYnRmdG5mZXJpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjI4NzQ3MzgsImV4cCI6MjA3ODQ1MDczOH0.xetRavdybCP-gNwPNQJLeGOt5rVcHsM3Y2VKVTcPKSM'

let supabaseExport: ReturnType<typeof createClient> | null = null

function getSupabaseClient() {
  if (!supabaseExport) {
    supabaseExport = createClient(SUPABASE_URL, SUPABASE_ANON_KEY)
    console.log('[Electron] Supabase client initialized for exports')
  }
  return supabaseExport
}

let mainWindow: BrowserWindow | null = null

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1200,
    minHeight: 700,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      webSecurity: false,                 // ⚡ local file erişimi serbest
      allowRunningInsecureContent: true,  // ⚡ asset/js yüklenmesini açar
      sandbox: false,                     // ⚡ sandbox kapalı (prod için güvenli)
      devTools: true
    },
    title: 'Ordevo - Restoran Yönetim Sistemi',
    autoHideMenuBar: false,
    show: false
  })

  mainWindow.once('ready-to-show', () => {
    mainWindow?.show()

    if (!process.env.VITE_DEV_SERVER_URL) {
      mainWindow?.webContents.openDevTools()
    }
  })

  mainWindow.webContents.on('console-message', (event, level, message) => {
    console.log(`[Renderer] ${message}`)
  })

  // ===========================
  // DEV MODE
  // ===========================
  if (process.env.VITE_DEV_SERVER_URL) {
    console.log('DEV MODE URL:', process.env.VITE_DEV_SERVER_URL)
    mainWindow.loadURL(process.env.VITE_DEV_SERVER_URL)
    mainWindow.webContents.openDevTools()
  }

  // ===========================
  // PROD MODE (EXE)
  // ===========================
  else {
    const distPath = path.join(process.resourcesPath, 'app', 'dist')
    const indexPath = path.join(distPath, 'index.html')

    console.log('=== PROD PATH DEBUG ===')
    console.log('process.resourcesPath:', process.resourcesPath)
    console.log('distPath:', distPath)
    console.log('indexPath:', indexPath)
    console.log('========================')

    mainWindow.loadFile(indexPath).catch(err => {
      console.error('❌ index.html yüklenemedi:', err)
    })
  }

  mainWindow.on('closed', () => {
    mainWindow = null
  })
}

app.whenReady().then(() => {
  createWindow()

  // Weather service'i başlat - Seferihisar, İzmir (Mersin Alanı)
  // Koordinatlar: 38.1967, 26.8383
  weatherService.start(38.1967, 26.8383, 'Seferihisar, İzmir')

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('before-quit', () => {
  // Weather service'i durdur
  weatherService.stop()
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

// ==========================================
// PRINTER FUNCTIONS
// ==========================================

// Get available printers
ipcMain.handle('get-printers', async () => {
  try {
    if (!mainWindow) return []
    const printers = await mainWindow.webContents.getPrintersAsync()
    return printers.map(p => p.name)
  } catch (error) {
    console.error('Get printers error:', error)
    return []
  }
})

// Print receipt (otomatik yazdırma - dialog göstermez)
// HTML → PDF → Silent Print yöntemi - Canon Pixma dahil %100 çalışır
ipcMain.handle('print-receipt', async (event, html: string, printerName?: string) => {
  let tempHtmlPath: string | null = null
  let tempPdfPath: string | null = null
  let printWindow: BrowserWindow | null = null

  try {
    console.log('[Print] Starting PDF silent print method...')
    
    const timestamp = Date.now()
    tempHtmlPath = path.join(os.tmpdir(), `ordevo_receipt_${timestamp}.html`)
    tempPdfPath = path.join(os.tmpdir(), `ordevo_receipt_${timestamp}.pdf`)

    // 1) HTML'i geçici dosyaya kaydet
    fs.writeFileSync(tempHtmlPath, html, 'utf-8')
    console.log('[Print] HTML file created:', tempHtmlPath)

    // 2) Gizli pencere oluştur
    printWindow = new BrowserWindow({
      width: 300,
      height: 600,
      show: false,
      webPreferences: {
        nodeIntegration: false,
        contextIsolation: true
      }
    })

    // 3) HTML dosyasını yükle
    await printWindow.loadFile(tempHtmlPath)
    console.log('[Print] HTML loaded successfully')

    // 4) PDF olarak dışa aktar
    const pdfBuffer = await printWindow.webContents.printToPDF({
      printBackground: true,
      pageSize: 'A4',
      landscape: false,
      margins: {
        top: 0,
        bottom: 0,
        left: 0,
        right: 0
      }
    })

    fs.writeFileSync(tempPdfPath, pdfBuffer)
    console.log('[Print] PDF created:', tempPdfPath)

    // Pencereyi kapat
    printWindow.close()
    printWindow = null

    // 5) PDF'i Windows ile sessiz yazdır
    // PowerShell komutu ile PDF'i varsayılan yazıcıya gönder
    const printCmd = `powershell -command "& { Start-Process -FilePath '${tempPdfPath}' -Verb Print -WindowStyle Hidden }"`
    
    console.log('[Print] Sending to printer...')
    
    return new Promise((resolve) => {
      exec(printCmd, (error, stdout, stderr) => {
        // Temizlik (3 saniye sonra - yazdırma işlemi için zaman tanı)
        setTimeout(() => {
          try {
            if (tempHtmlPath && fs.existsSync(tempHtmlPath)) {
              fs.unlinkSync(tempHtmlPath)
            }
            if (tempPdfPath && fs.existsSync(tempPdfPath)) {
              fs.unlinkSync(tempPdfPath)
            }
            console.log('[Print] Temp files deleted')
          } catch (cleanupError) {
            console.error('[Print] Cleanup error:', cleanupError)
          }
        }, 3000)

        if (error) {
          console.error('[Print] PowerShell error:', error)
          console.error('[Print] stderr:', stderr)
          resolve({ success: false, error: error.message })
        } else {
          console.log('[Print] PDF sent to printer successfully!')
          resolve({ success: true })
        }
      })
    })

  } catch (error) {
    console.error('[Print] Exception:', error)
    
    // Temizlik
    if (printWindow) {
      printWindow.close()
    }
    if (tempHtmlPath && fs.existsSync(tempHtmlPath)) {
      fs.unlinkSync(tempHtmlPath)
    }
    if (tempPdfPath && fs.existsSync(tempPdfPath)) {
      fs.unlinkSync(tempPdfPath)
    }
    
    return { success: false, error: String(error) }
  }
})

// Silent print (otomatik yazdırma için)
ipcMain.handle('print-silent', async (event, html: string, printerName?: string) => {
  if (!mainWindow) return { success: false, error: 'Main window not found' }

  try {
    const printWindow = new BrowserWindow({
      show: false,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false
      }
    })

    await printWindow.loadURL(
      `data:text/html;charset=utf-8,${encodeURIComponent(html)}`
    )

    const printOptions: any = {
      silent: true, // Sessiz yazdırma
      printBackground: true,
      margins: { marginType: 'none' }
    }

    if (printerName) {
      printOptions.deviceName = printerName
    }

    printWindow.webContents.print(
      printOptions,
      (success, errorType) => {
        if (!success) console.error('Silent print error:', errorType)
        printWindow.close()
      }
    )

    return { success: true }
  } catch (error) {
    console.error('Silent print error:', error)
    return { success: false, error: String(error) }
  }
})

// ==========================================
// WEATHER FUNCTIONS
// ==========================================

// Get latest weather data
ipcMain.handle('get-weather', async () => {
  try {
    const weather = await weatherService.getLatestWeather()
    return { success: true, data: weather }
  } catch (error) {
    console.error('Get weather error:', error)
    return { success: false, error: String(error) }
  }
})

// Fetch and save weather immediately
ipcMain.handle('fetch-weather', async () => {
  try {
    const weather = await weatherService.fetchAndSaveWeather()
    return { success: true, data: weather }
  } catch (error) {
    console.error('Fetch weather error:', error)
    return { success: false, error: String(error) }
  }
})

// Set city for weather updates
ipcMain.handle('set-weather-city', async (event, city: string) => {
  try {
    weatherService.setCity(city)
    return { success: true }
  } catch (error) {
    console.error('Set weather city error:', error)
    return { success: false, error: String(error) }
  }
})

// ==========================================
// ML EXPORT FUNCTIONS
// ==========================================

// Export master CSV
ipcMain.handle('export-master-csv', async (event, options: any) => {
  try {
    console.log('[IPC] export-master-csv called with:', options)

    const supabase = getSupabaseClient()
    if (!supabase) {
      return { 
        success: false, 
        error: 'Supabase client not initialized. Please check environment variables.' 
      }
    }

    // Show save dialog
    const result = await dialog.showSaveDialog(mainWindow!, {
      title: 'Master CSV Kaydet',
      defaultPath: generateFilename('ordevo_master', options.date),
      filters: [
        { name: 'CSV Files', extensions: ['csv'] },
        { name: 'All Files', extensions: ['*'] },
      ],
    })

    if (result.canceled || !result.filePath) {
      return { success: false, error: 'Cancelled by user' }
    }

    // Export
    const exportResult = await exportMasterCSV(
      supabase,
      options,
      result.filePath
    )

    return {
      ...exportResult,
      filePath: result.filePath,
    }
  } catch (error: any) {
    console.error('[IPC] export-master-csv error:', error)
    return { success: false, error: String(error) }
  }
})

// Export products CSV
ipcMain.handle('export-products-csv', async (event, options: any) => {
  try {
    console.log('[IPC] export-products-csv called with:', options)

    const supabase = getSupabaseClient()
    if (!supabase) {
      return { 
        success: false, 
        error: 'Supabase client not initialized. Please check environment variables.' 
      }
    }

    // Show save dialog
    const result = await dialog.showSaveDialog(mainWindow!, {
      title: 'Ürün CSV Kaydet',
      defaultPath: generateFilename('ordevo_products', options.date),
      filters: [
        { name: 'CSV Files', extensions: ['csv'] },
        { name: 'All Files', extensions: ['*'] },
      ],
    })

    if (result.canceled || !result.filePath) {
      return { success: false, error: 'Cancelled by user' }
    }

    // Export
    const exportResult = await exportProductsCSV(
      supabase,
      options,
      result.filePath
    )

    return {
      ...exportResult,
      filePath: result.filePath,
    }
  } catch (error: any) {
    console.error('[IPC] export-products-csv error:', error)
    return { success: false, error: String(error) }
  }
})
