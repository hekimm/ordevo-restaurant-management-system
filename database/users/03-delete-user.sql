-- ============================================
-- Kullanıcı Sil
-- ============================================

-- KULLANIM:
-- 1. Silinecek kullanıcının email adresini yazın
-- 2. Script'i çalıştırın

DO $$
DECLARE
  v_email TEXT := 'silinecek@example.com'; -- Silinecek kullanıcı email
  v_user_id UUID;
BEGIN
  -- User ID'yi bul
  SELECT id INTO v_user_id FROM profiles WHERE email = v_email;

  IF v_user_id IS NULL THEN
    RAISE EXCEPTION 'Kullanıcı bulunamadı: %', v_email;
  END IF;

  -- Profile sil (cascade ile auth.users da silinir)
  DELETE FROM profiles WHERE id = v_user_id;

  -- Auth identities sil
  DELETE FROM auth.identities WHERE user_id = v_user_id;

  -- Auth users sil
  DELETE FROM auth.users WHERE id = v_user_id;

  RAISE NOTICE 'Kullanıcı başarıyla silindi: %', v_email;
END $$;
