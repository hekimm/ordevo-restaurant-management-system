-- ============================================
-- Kullanıcı Şifresini Değiştir
-- ============================================

-- KULLANIM:
-- 1. Email ve yeni şifreyi yazın
-- 2. Script'i çalıştırın

DO $$
DECLARE
  v_email TEXT := 'kullanici@example.com'; -- Kullanıcı email
  v_new_password TEXT := 'yeni123456'; -- Yeni şifre (en az 6 karakter)
  v_user_id UUID;
BEGIN
  -- User ID'yi bul
  SELECT id INTO v_user_id FROM profiles WHERE email = v_email;

  IF v_user_id IS NULL THEN
    RAISE EXCEPTION 'Kullanıcı bulunamadı: %', v_email;
  END IF;

  -- Şifreyi güncelle
  UPDATE auth.users
  SET 
    encrypted_password = crypt(v_new_password, gen_salt('bf')),
    updated_at = NOW()
  WHERE id = v_user_id;

  RAISE NOTICE 'Şifre başarıyla değiştirildi!';
  RAISE NOTICE 'Email: %', v_email;
  RAISE NOTICE 'Yeni Şifre: %', v_new_password;
END $$;
