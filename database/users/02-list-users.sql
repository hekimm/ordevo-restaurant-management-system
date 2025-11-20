-- ============================================
-- Tüm Kullanıcıları Listele
-- ============================================

SELECT 
  p.id as user_id,
  p.email,
  p.full_name,
  p.role,
  o.name as organization_name,
  p.organization_id,
  p.created_at
FROM profiles p
LEFT JOIN organizations o ON o.id = p.organization_id
ORDER BY p.created_at DESC;
