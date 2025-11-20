// Sabit organizasyon ayarları
// Bu uygulama sadece m.sirinyilmaz6@gmail.com organizasyonu için çalışır

export const FIXED_ORGANIZATION = {
  // m.sirinyilmaz6@gmail.com organizasyonu
  ORGANIZATION_ID: '73cdab97-03c7-466e-91c9-f7c8c18c1f2f',
  OWNER_EMAIL: 'm.sirinyilmaz6@gmail.com'
} as const;

// Organizasyon ID'sini döndüren yardımcı fonksiyon
export function getOrganizationId(): string {
  return FIXED_ORGANIZATION.ORGANIZATION_ID;
}

// Kullanıcının bu organizasyona ait olup olmadığını kontrol et
export function validateUserOrganization(userOrgId: string): boolean {
  return userOrgId === FIXED_ORGANIZATION.ORGANIZATION_ID;
}
