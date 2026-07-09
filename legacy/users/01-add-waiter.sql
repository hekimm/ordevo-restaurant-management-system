-- ============================================
-- Garson Kullanıcısı Ekle
-- ============================================

-- KULLANIM:
-- 1. Aşağıdaki değişkenleri düzenleyin
-- 2. Script'i Supabase SQL Editor'de çalıştırın

DO $$
DECLARE
  v_email TEXT := 'garson@example.com'; -- Garson email adresi
  v_password TEXT := '123456'; -- Garson şifresi (en az 6 karakter)
  v_full_name TEXT := 'Garson Adı'; -- Garson tam adı
  v_org_id UUID := '73cdab97-03c7-466e-91c9-f7c8c18c1f2f'; -- Organization ID
  v_user_id UUID;
BEGIN
  -- 1. Auth kullanıcısı oluştur
  INSERT INTO auth.users (
    instance_id,
    id,
    aud,
    role,
    email,
    encrypted_password,
    email_confirmed_at,
    created_at,
    updated_at,
    raw_app_meta_data,
    raw_user_meta_data,
    is_super_admin,
    confirmation_token,
    email_change_token_new,
    recovery_token
  )
  VALUES (
    '00000000-0000-0000-0000-000000000000',
    gen_random_uuid(),
    'authenticated',
    'authenticated',
    v_email,
    crypt(v_password, gen_salt('bf')),
    NOW(),
    NOW(),
    NOW(),
    '{"provider":"email","providers":["email"]}',
    '{}',
    false,
    '',
    '',
    ''
  )
  RETURNING id INTO v_user_id;

  -- 2. Profile oluştur
  INSERT INTO profiles (
    id,
    organization_id,
    email,
    full_name,
    role
  )
  VALUES (
    v_user_id,
    v_org_id,
    v_email,
    v_full_name,
    'waiter'
  );

  -- 3. Identity kaydı oluştur
  INSERT INTO auth.identities (
    id,
    user_id,
    identity_data,
    provider,
    last_sign_in_at,
    created_at,
    updated_at
  )
  VALUES (
    gen_random_uuid(),
    v_user_id,
    format('{"sub":"%s","email":"%s"}', v_user_id::text, v_email)::jsonb,
    'email',
    NOW(),
    NOW(),
    NOW()
  );

  RAISE NOTICE 'Garson kullanıcısı başarıyla oluşturuldu!';
  RAISE NOTICE 'Email: %', v_email;
  RAISE NOTICE 'Şifre: %', v_password;
  RAISE NOTICE 'User ID: %', v_user_id;
END $$;
