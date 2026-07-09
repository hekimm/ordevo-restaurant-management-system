-- ============================================
-- ORDEVO - Row Level Security (RLS) Policies
-- ============================================

-- Enable RLS on all tables
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE restaurant_tables ENABLE ROW LEVEL SECURITY;
ALTER TABLE menu_categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE menu_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE order_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE organization_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE category_printer_mappings ENABLE ROW LEVEL SECURITY;
ALTER TABLE weather_data ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_sales_archive ENABLE ROW LEVEL SECURITY;

-- Drop existing policies if any
DROP POLICY IF EXISTS "Users can view their organization" ON organizations;
DROP POLICY IF EXISTS "Users can update their organization" ON organizations;
DROP POLICY IF EXISTS "Users can view profiles in their organization" ON profiles;
DROP POLICY IF EXISTS "Users can view tables in their organization" ON restaurant_tables;
DROP POLICY IF EXISTS "Users can manage tables in their organization" ON restaurant_tables;
DROP POLICY IF EXISTS "Users can view categories in their organization" ON menu_categories;
DROP POLICY IF EXISTS "Users can manage categories in their organization" ON menu_categories;
DROP POLICY IF EXISTS "Users can view menu items in their organization" ON menu_items;
DROP POLICY IF EXISTS "Users can manage menu items in their organization" ON menu_items;
DROP POLICY IF EXISTS "Users can view orders in their organization" ON orders;
DROP POLICY IF EXISTS "Users can manage orders in their organization" ON orders;
DROP POLICY IF EXISTS "Users can view order items in their organization" ON order_items;
DROP POLICY IF EXISTS "Users can manage order items in their organization" ON order_items;
DROP POLICY IF EXISTS "Users can view settings in their organization" ON organization_settings;
DROP POLICY IF EXISTS "Users can manage settings in their organization" ON organization_settings;
DROP POLICY IF EXISTS "Users can view printer mappings in their organization" ON category_printer_mappings;
DROP POLICY IF EXISTS "Users can manage printer mappings in their organization" ON category_printer_mappings;
DROP POLICY IF EXISTS "Users can view weather in their organization" ON weather_data;
DROP POLICY IF EXISTS "Users can manage weather in their organization" ON weather_data;
DROP POLICY IF EXISTS "Users can view archive in their organization" ON daily_sales_archive;
DROP POLICY IF EXISTS "Users can manage archive in their organization" ON daily_sales_archive;

-- Organizations Policies
CREATE POLICY "Users can view their organization" ON organizations
  FOR SELECT USING (
    id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can update their organization" ON organizations
  FOR UPDATE USING (
    id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Profiles Policies
CREATE POLICY "Users can view profiles in their organization" ON profiles
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

-- Restaurant Tables Policies
CREATE POLICY "Users can view tables in their organization" ON restaurant_tables
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage tables in their organization" ON restaurant_tables
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Menu Categories Policies
CREATE POLICY "Users can view categories in their organization" ON menu_categories
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage categories in their organization" ON menu_categories
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Menu Items Policies
CREATE POLICY "Users can view menu items in their organization" ON menu_items
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage menu items in their organization" ON menu_items
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Orders Policies
CREATE POLICY "Users can view orders in their organization" ON orders
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage orders in their organization" ON orders
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

-- Order Items Policies
CREATE POLICY "Users can view order items in their organization" ON order_items
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage order items in their organization" ON order_items
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

-- Organization Settings Policies
CREATE POLICY "Users can view settings in their organization" ON organization_settings
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage settings in their organization" ON organization_settings
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Category Printer Mappings Policies
CREATE POLICY "Users can view printer mappings in their organization" ON category_printer_mappings
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage printer mappings in their organization" ON category_printer_mappings
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );

-- Weather Data Policies
CREATE POLICY "Users can view weather in their organization" ON weather_data
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage weather in their organization" ON weather_data
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

-- Daily Sales Archive Policies
CREATE POLICY "Users can view archive in their organization" ON daily_sales_archive
  FOR SELECT USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid())
  );

CREATE POLICY "Users can manage archive in their organization" ON daily_sales_archive
  FOR ALL USING (
    organization_id IN (SELECT organization_id FROM profiles WHERE id = auth.uid() AND role IN ('owner', 'manager'))
  );
