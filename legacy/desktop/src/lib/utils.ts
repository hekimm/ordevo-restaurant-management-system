// Tarih formatlama (Türkçe)
export const formatDate = (date: string | Date): string => {
  const d = typeof date === 'string' ? new Date(date) : date
  return d.toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export const formatDateTime = (date: string | Date): string => {
  const d = typeof date === 'string' ? new Date(date) : date
  return d.toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Para formatlama (TRY)
export const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount)
}

// Tarih aralığı yardımcıları
export const getDateRange = (period: 'today' | 'week' | 'month' | 'year'): { start: Date; end: Date } => {
  const start = new Date()
  const end = new Date()

  switch (period) {
    case 'today':
      start.setHours(0, 0, 0, 0)
      end.setHours(23, 59, 59, 999)
      break
    case 'week':
      const day = start.getDay()
      const diff = start.getDate() - day + (day === 0 ? -6 : 1) // Pazartesi başlangıç
      start.setDate(diff)
      start.setHours(0, 0, 0, 0)
      end.setDate(start.getDate() + 6)
      end.setHours(23, 59, 59, 999)
      break
    case 'month':
      start.setDate(1)
      start.setHours(0, 0, 0, 0)
      end.setMonth(start.getMonth() + 1)
      end.setDate(0)
      end.setHours(23, 59, 59, 999)
      break
    case 'year':
      start.setMonth(0, 1)
      start.setHours(0, 0, 0, 0)
      end.setMonth(11, 31)
      end.setHours(23, 59, 59, 999)
      break
  }

  return { start, end }
}

// Ödeme metodu Türkçe çevirisi
export const getPaymentMethodLabel = (method: string): string => {
  const labels: Record<string, string> = {
    cash: 'Nakit',
    credit_card: 'Kredi Kartı',
    online: 'Online',
    other: 'Diğer'
  }
  return labels[method] || method
}

// Sipariş durumu Türkçe çevirisi
export const getOrderStatusLabel = (status: string): string => {
  const labels: Record<string, string> = {
    open: 'Açık',
    closed: 'Kapalı',
    cancelled: 'İptal'
  }
  return labels[status] || status
}

// Sipariş kalemi durumu Türkçe çevirisi
export const getOrderItemStatusLabel = (status: string): string => {
  const labels: Record<string, string> = {
    pending: 'Bekliyor',
    in_kitchen: 'Mutfakta',
    served: 'Servis Edildi',
    cancelled: 'İptal'
  }
  return labels[status] || status
}

// Rol Türkçe çevirisi
export const getRoleLabel = (role: string): string => {
  const labels: Record<string, string> = {
    owner: 'Sahip',
    manager: 'Yönetici',
    cashier: 'Kasiyer',
    waiter: 'Garson'
  }
  return labels[role] || role
}

// Süre hesaplama (ne kadar zaman geçti)
export const getElapsedTime = (startDate: string | Date): string => {
  const start = typeof startDate === 'string' ? new Date(startDate) : startDate
  const diffMs = new Date().getTime() - start.getTime()
  
  const diffMinutes = Math.floor(diffMs / (1000 * 60))
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)
  
  if (diffDays > 0) {
    return `${diffDays} gün ${diffHours % 24} saat`
  } else if (diffHours > 0) {
    return `${diffHours} saat ${diffMinutes % 60} dk`
  } else {
    return `${diffMinutes} dakika`
  }
}

// Süre hesaplama (kısa format)
export const getElapsedTimeShort = (startDate: string | Date): string => {
  const start = typeof startDate === 'string' ? new Date(startDate) : startDate
  const diffMs = new Date().getTime() - start.getTime()
  
  const diffMinutes = Math.floor(diffMs / (1000 * 60))
  const diffHours = Math.floor(diffMinutes / 60)
  
  if (diffHours > 0) {
    return `${diffHours}s ${diffMinutes % 60}dk`
  } else {
    return `${diffMinutes}dk`
  }
}
