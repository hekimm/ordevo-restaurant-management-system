-- ============================================
-- ORDEVO - Sample Data (Optional)
-- Sadece test için kullanın
-- ============================================

-- NOT: Bu script'i çalıştırmadan önce:
-- 1. Bir kullanıcı kayıt olmalı (Register sayfasından)
-- 2. O kullanıcının organization_id'sini almalısınız
-- 3. Aşağıdaki 'YOUR_ORG_ID_HERE' yerine kendi organization_id'nizi yazın

DO $$
DECLARE
  v_org_id UUID := 'YOUR_ORG_ID_HERE'; -- BURAYA KENDİ ORG ID'NİZİ YAZIN
  v_category_kebap UUID;
  v_category_icecek UUID;
  v_category_tatli UUID;
  v_category_salata UUID;
BEGIN
  -- Kategoriler
  INSERT INTO menu_categories (organization_id, name, sort_order)
  VALUES 
    (v_org_id, 'Kebaplar', 1),
    (v_org_id, 'İçecekler', 2),
    (v_org_id, 'Tatlılar', 3),
    (v_org_id, 'Salatalar', 4)
  RETURNING id INTO v_category_kebap, v_category_icecek, v_category_tatli, v_category_salata;

  -- Kategori ID'lerini al
  SELECT id INTO v_category_kebap FROM menu_categories WHERE organization_id = v_org_id AND name = 'Kebaplar';
  SELECT id INTO v_category_icecek FROM menu_categories WHERE organization_id = v_org_id AND name = 'İçecekler';
  SELECT id INTO v_category_tatli FROM menu_categories WHERE organization_id = v_org_id AND name = 'Tatlılar';
  SELECT id INTO v_category_salata FROM menu_categories WHERE organization_id = v_org_id AND name = 'Salatalar';

  -- Kebaplar
  INSERT INTO menu_items (organization_id, category_id, name, price, is_active)
  VALUES
    (v_org_id, v_category_kebap, 'Adana Kebap', 180.00, true),
    (v_org_id, v_category_kebap, 'Urfa Kebap', 180.00, true),
    (v_org_id, v_category_kebap, 'Beyti Kebap', 200.00, true),
    (v_org_id, v_category_kebap, 'İskender Kebap', 190.00, true),
    (v_org_id, v_category_kebap, 'Karışık Izgara', 220.00, true);

  -- İçecekler
  INSERT INTO menu_items (organization_id, category_id, name, price, is_active)
  VALUES
    (v_org_id, v_category_icecek, 'Ayran', 15.00, true),
    (v_org_id, v_category_icecek, 'Kola', 25.00, true),
    (v_org_id, v_category_icecek, 'Fanta', 25.00, true),
    (v_org_id, v_category_icecek, 'Su', 10.00, true),
    (v_org_id, v_category_icecek, 'Çay', 12.00, true);

  -- Tatlılar
  INSERT INTO menu_items (organization_id, category_id, name, price, is_active)
  VALUES
    (v_org_id, v_category_tatli, 'Künefe', 120.00, true),
    (v_org_id, v_category_tatli, 'Baklava', 100.00, true),
    (v_org_id, v_category_tatli, 'Sütlaç', 60.00, true);

  -- Salatalar
  INSERT INTO menu_items (organization_id, category_id, name, price, is_active)
  VALUES
    (v_org_id, v_category_salata, 'Çoban Salata', 45.00, true),
    (v_org_id, v_category_salata, 'Mevsim Salata', 40.00, true);

  -- Masalar
  INSERT INTO restaurant_tables (organization_id, name, capacity, sort_order, is_active)
  VALUES
    (v_org_id, 'Masa 1', 4, 1, true),
    (v_org_id, 'Masa 2', 4, 2, true),
    (v_org_id, 'Masa 3', 6, 3, true),
    (v_org_id, 'Masa 4', 2, 4, true),
    (v_org_id, 'Masa 5', 4, 5, true),
    (v_org_id, 'Masa 6', 8, 6, true),
    (v_org_id, 'Masa 7', 4, 7, true),
    (v_org_id, 'Masa 8', 4, 8, true);

  -- Organization Settings
  INSERT INTO organization_settings (organization_id, auto_print_enabled, default_printer)
  VALUES (v_org_id, false, NULL)
  ON CONFLICT (organization_id) DO NOTHING;

  RAISE NOTICE 'Sample data created successfully!';
END $$;
