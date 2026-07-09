// ML Export Hook

import { useState } from 'react'
import { useAuthStore } from '@/store/authStore'
import type { ExportOptions, ExportResult } from '@/types/ml-export'

export function useMLExport() {
  const { profile } = useAuthStore()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const exportMasterCSV = async (
    date: string,
    bucketMinutes: number = 60
  ): Promise<ExportResult> => {
    if (!window.electron) {
      return {
        success: false,
        error: 'Bu özellik sadece Electron uygulamasında çalışır',
      }
    }

    if (!profile?.organization_id) {
      return {
        success: false,
        error: 'Organization ID bulunamadı',
      }
    }

    try {
      setLoading(true)
      setError(null)

      const options: ExportOptions = {
        date,
        bucketMinutes,
        organizationId: profile.organization_id,
      }

      const result = await window.electron.ipcRenderer.invoke(
        'export-master-csv',
        options
      )

      if (!result.success) {
        setError(result.error || 'Export başarısız')
      }

      return result
    } catch (err: any) {
      const errorMsg = err.message || 'Bilinmeyen hata'
      setError(errorMsg)
      return {
        success: false,
        error: errorMsg,
      }
    } finally {
      setLoading(false)
    }
  }

  const exportProductsCSV = async (
    date: string,
    bucketMinutes: number = 60
  ): Promise<ExportResult> => {
    if (!window.electron) {
      return {
        success: false,
        error: 'Bu özellik sadece Electron uygulamasında çalışır',
      }
    }

    if (!profile?.organization_id) {
      return {
        success: false,
        error: 'Organization ID bulunamadı',
      }
    }

    try {
      setLoading(true)
      setError(null)

      const options: ExportOptions = {
        date,
        bucketMinutes,
        organizationId: profile.organization_id,
      }

      const result = await window.electron.ipcRenderer.invoke(
        'export-products-csv',
        options
      )

      if (!result.success) {
        setError(result.error || 'Export başarısız')
      }

      return result
    } catch (err: any) {
      const errorMsg = err.message || 'Bilinmeyen hata'
      setError(errorMsg)
      return {
        success: false,
        error: errorMsg,
      }
    } finally {
      setLoading(false)
    }
  }

  return {
    loading,
    error,
    exportMasterCSV,
    exportProductsCSV,
  }
}
