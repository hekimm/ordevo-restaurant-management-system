import { create } from 'zustand'
import { supabase } from '@/lib/supabase'

interface DailySalesArchive {
  id: string
  organization_id: string
  business_date: string
  total_orders: number
  total_revenue: number
  total_cash: number
  total_credit_card: number
  total_online: number
  total_other: number
  archived_at: string
}

interface DailyArchiveState {
  currentBusinessDate: string
  lastArchiveDate: string | null
  isArchiving: boolean
  error: string | null
  
  // Actions
  checkAndArchive: () => Promise<void>
  archiveDay: (businessDate: string) => Promise<void>
  getArchiveHistory: (startDate: string, endDate: string) => Promise<DailySalesArchive[]>
  getCurrentBusinessDate: () => string
}

export const useDailyArchiveStore = create<DailyArchiveState>((set, get) => ({
  currentBusinessDate: new Date().toISOString().split('T')[0],
  lastArchiveDate: null,
  isArchiving: false,
  error: null,

  getCurrentBusinessDate: () => {
    return new Date().toISOString().split('T')[0]
  },

  checkAndArchive: async () => {
    try {
      const currentDate = get().getCurrentBusinessDate()
      const lastArchive = localStorage.getItem('lastArchiveDate')
      
      // Eğer son arşivleme bugün değilse, dünü arşivle
      if (lastArchive !== currentDate) {
        const yesterday = new Date()
        yesterday.setDate(yesterday.getDate() - 1)
        const yesterdayStr = yesterday.toISOString().split('T')[0]
        
        await get().archiveDay(yesterdayStr)
        localStorage.setItem('lastArchiveDate', currentDate)
        set({ lastArchiveDate: currentDate })
      }
    } catch (error: any) {
      console.error('Otomatik arşivleme hatası:', error)
      set({ error: error.message })
    }
  },

  archiveDay: async (businessDate: string) => {
    try {
      set({ isArchiving: true, error: null })

      const { data: { user } } = await supabase.auth.getUser()
      if (!user) throw new Error('Kullanıcı oturumu bulunamadı')

      const { data: profile } = await supabase
        .from('profiles')
        .select('organization_id')
        .eq('id', user.id)
        .single()

      if (!profile) throw new Error('Profil bulunamadı')

      // PostgreSQL fonksiyonunu çağır
      const { data, error } = await supabase.rpc('archive_daily_data', {
        p_organization_id: profile.organization_id,
        p_business_date: businessDate,
        p_user_id: user.id
      })

      if (error) throw error

      if (data && !data.success) {
        throw new Error(data.error || 'Arşivleme başarısız')
      }

      console.log('Günlük arşivleme tamamlandı:', data)
    } catch (error: any) {
      console.error('Arşivleme hatası:', error)
      set({ error: error.message })
      throw error
    } finally {
      set({ isArchiving: false })
    }
  },

  getArchiveHistory: async (startDate: string, endDate: string) => {
    try {
      const { data: { user } } = await supabase.auth.getUser()
      if (!user) throw new Error('Kullanıcı oturumu bulunamadı')

      const { data: profile } = await supabase
        .from('profiles')
        .select('organization_id')
        .eq('id', user.id)
        .single()

      if (!profile) throw new Error('Profil bulunamadı')

      const { data, error } = await supabase
        .from('daily_sales_archive')
        .select('*')
        .eq('organization_id', profile.organization_id)
        .gte('business_date', startDate)
        .lte('business_date', endDate)
        .order('business_date', { ascending: false })

      if (error) throw error

      return data || []
    } catch (error: any) {
      console.error('Arşiv geçmişi yüklenirken hata:', error)
      set({ error: error.message })
      return []
    }
  }
}))
