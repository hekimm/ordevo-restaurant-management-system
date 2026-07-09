import { create } from 'zustand'
import { supabase, Profile, Organization } from '@/lib/supabase'
import { User } from '@supabase/supabase-js'

interface AuthState {
  user: User | null
  profile: Profile | null
  organization: Organization | null
  loading: boolean
  error: string | null
  
  // Actions
  signIn: (email: string, password: string) => Promise<void>
  signUp: (email: string, password: string, fullName: string, restaurantName: string) => Promise<void>
  signOut: () => Promise<void>
  loadUserData: (retryCount?: number) => Promise<void>
  setError: (error: string | null) => void
  createSampleData: (organizationId: string) => Promise<void>
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  profile: null,
  organization: null,
  loading: true,
  error: null,

  setError: (error) => set({ error }),

  signIn: async (email: string, password: string) => {
    try {
      set({ loading: true, error: null })
      
      const { data, error } = await supabase.auth.signInWithPassword({
        email,
        password
      })

      if (error) throw new Error(error.message)
      
      if (data.user) {
        await get().loadUserData()
      }
    } catch (error: any) {
      set({ error: error.message || 'Giriş yapılırken bir hata oluştu' })
      throw error
    } finally {
      set({ loading: false })
    }
  },

  signUp: async (email: string, password: string, fullName: string, restaurantName: string) => {
    try {
      set({ loading: true, error: null })

      // 1. Kullanıcı oluştur
      const { data: authData, error: authError } = await supabase.auth.signUp({
        email,
        password
      })

      if (authError) throw new Error(authError.message)
      if (!authData.user) throw new Error('Kullanıcı oluşturulamadı')

      // 2. Organizasyon oluştur
      const slug = restaurantName
        .toLowerCase()
        .replace(/ğ/g, 'g')
        .replace(/ü/g, 'u')
        .replace(/ş/g, 's')
        .replace(/ı/g, 'i')
        .replace(/ö/g, 'o')
        .replace(/ç/g, 'c')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')

      const { data: orgData, error: orgError } = await supabase
        .from('organizations')
        .insert({
          name: restaurantName,
          slug: slug + '-' + Date.now()
        })
        .select()

      if (orgError) throw new Error(orgError.message)
      if (!orgData || orgData.length === 0) throw new Error('Organizasyon oluşturulamadı')

      const organization = orgData[0]

      // 3. Profil oluştur
      const { error: profileError } = await supabase
        .from('profiles')
        .insert({
          id: authData.user.id,
          organization_id: organization.id,
          full_name: fullName,
          role: 'owner'
        })

      if (profileError) throw new Error(profileError.message)

      // 4. Örnek veriler oluştur
      await get().createSampleData(organization.id)

      // 5. Kısa bir bekleme (RLS politikalarının güncellenmesi için)
      await new Promise(resolve => setTimeout(resolve, 1000))

      // 6. Kullanıcı verilerini yükle
      await get().loadUserData()
    } catch (error: any) {
      set({ error: error.message || 'Kayıt olurken bir hata oluştu' })
      throw error
    } finally {
      set({ loading: false })
    }
  },

  signOut: async () => {
    try {
      set({ loading: true, error: null })
      await supabase.auth.signOut()
      set({ user: null, profile: null, organization: null })
    } catch (error: any) {
      set({ error: error.message || 'Çıkış yapılırken bir hata oluştu' })
    } finally {
      set({ loading: false })
    }
  },

  loadUserData: async (retryCount = 0) => {
    try {
      set({ loading: true, error: null })

      const { data: { user } } = await supabase.auth.getUser()
      
      if (!user) {
        set({ user: null, profile: null, organization: null, loading: false })
        return
      }

      // Profil bilgisini çek
      const { data: profileData, error: profileError } = await supabase
        .from('profiles')
        .select('*')
        .eq('id', user.id)

      if (profileError) throw profileError
      
      // Eğer profil bulunamadıysa ve retry hakkımız varsa, tekrar dene
      if ((!profileData || profileData.length === 0) && retryCount < 3) {
        console.log(`Profil bulunamadı, tekrar deneniyor... (${retryCount + 1}/3)`)
        await new Promise(resolve => setTimeout(resolve, 1000))
        return get().loadUserData(retryCount + 1)
      }
      
      if (!profileData || profileData.length === 0) {
        throw new Error('Profil bulunamadı. Lütfen sayfayı yenileyin.')
      }

      const profile = profileData[0]

      // Organizasyon bilgisini çek
      const { data: orgData, error: orgError } = await supabase
        .from('organizations')
        .select('*')
        .eq('id', profile.organization_id)

      if (orgError) throw orgError
      if (!orgData || orgData.length === 0) {
        throw new Error('Organizasyon bulunamadı')
      }

      const organization = orgData[0]

      set({ user, profile, organization, loading: false })
    } catch (error: any) {
      console.error('Kullanıcı verisi yüklenirken hata:', error)
      set({ error: error.message, loading: false })
    }
  },

  // Örnek veriler oluştur (ilk kayıtta)
  createSampleData: async (organizationId: string) => {
    try {
      // Örnek kategoriler
      const { data: categories } = await supabase
        .from('menu_categories')
        .insert([
          { organization_id: organizationId, name: 'Kebaplar', sort_order: 1 },
          { organization_id: organizationId, name: 'İçecekler', sort_order: 2 },
          { organization_id: organizationId, name: 'Tatlılar', sort_order: 3 }
        ])
        .select()

      if (categories && categories.length > 0) {
        // Örnek ürünler
        await supabase.from('menu_items').insert([
          {
            organization_id: organizationId,
            category_id: categories[0].id,
            name: 'Adana Kebap',
            price: 180,
            is_active: true
          },
          {
            organization_id: organizationId,
            category_id: categories[0].id,
            name: 'Urfa Kebap',
            price: 180,
            is_active: true
          },
          {
            organization_id: organizationId,
            category_id: categories[1].id,
            name: 'Ayran',
            price: 15,
            is_active: true
          },
          {
            organization_id: organizationId,
            category_id: categories[1].id,
            name: 'Kola',
            price: 25,
            is_active: true
          }
        ])
      }

      // Örnek masalar
      await supabase.from('restaurant_tables').insert([
        { organization_id: organizationId, name: 'Masa 1', capacity: 4, sort_order: 1 },
        { organization_id: organizationId, name: 'Masa 2', capacity: 4, sort_order: 2 },
        { organization_id: organizationId, name: 'Masa 3', capacity: 6, sort_order: 3 },
        { organization_id: organizationId, name: 'Masa 4', capacity: 2, sort_order: 4 }
      ])
    } catch (error) {
      console.error('Örnek veriler oluşturulurken hata:', error)
    }
  }
}))

// Auth state değişikliklerini dinle
supabase.auth.onAuthStateChange((event, session) => {
  if (event === 'SIGNED_IN' && session) {
    useAuthStore.getState().loadUserData()
  } else if (event === 'SIGNED_OUT') {
    useAuthStore.setState({ user: null, profile: null, organization: null })
  }
})
